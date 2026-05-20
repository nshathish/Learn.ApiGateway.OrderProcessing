module "resource_group" {
  source = "./modules/resource_group"

  name     = "${var.prefix}-${var.environment}-rg"
  location = var.location

  tags = {
    Environment = var.environment
    Project     = var.prefix
    ManagedBy   = "Terraform"
  }
}

module "container_registry" {
  source = "./modules/container_registry"

  name                    = "${var.prefix}${var.environment}acr" # Must be globally unique, no hyphens
  resource_group_name     = module.resource_group.name
  location                = module.resource_group.location
  sku                     = var.acr_sku
  enable_managed_identity = var.enable_acr_managed_identity

  tags = {
    Environment = var.environment
    Project     = var.prefix
    ManagedBy   = "Terraform"
  }
}

# NEW: User-assigned managed identity for Container Apps to pull from ACR
resource "azurerm_user_assigned_identity" "acr_pull_identity" {
  name                = "${var.prefix}-${var.environment}-acr-pull-id"
  resource_group_name = module.resource_group.name
  location            = module.resource_group.location

  tags = {
    Environment = var.environment
    Project     = var.prefix
    Purpose     = "ACR Pull for Container Apps"
  }
}

# Grant the identity permission to pull from ACR
resource "azurerm_role_assignment" "acr_pull_role" {
  principal_id         = azurerm_user_assigned_identity.acr_pull_identity.principal_id
  role_definition_name = "AcrPull"
  scope                = module.container_registry.id
}

# NEW: Log Analytics Workspace for Container Apps monitoring
resource "azurerm_log_analytics_workspace" "this" {
  name                = "${var.prefix}-${var.environment}-logs"
  location            = module.resource_group.location
  resource_group_name = module.resource_group.name
  sku                 = "PerGB2018"
  retention_in_days   = var.environment == "prod" ? 90 : 30

  tags = {
    Environment = var.environment
    Project     = var.prefix
  }
}

module "container_apps" {
  source = "./modules/container_app"

  environment_name           = "${var.prefix}-${var.environment}-env"
  resource_group_name        = module.resource_group.name
  location                   = module.resource_group.location
  log_analytics_workspace_id = azurerm_log_analytics_workspace.this.id
  acr_login_server           = module.container_registry.login_server
  acr_pull_identity_id       = azurerm_user_assigned_identity.acr_pull_identity.id

  # Define your microservices
  microservices = {
    paymentservice = {
      tag         = "latest"
      image       = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
      cpu         = "0.5"
      memory      = "1.0Gi"
      target_port = 8080
      external    = false # Internal only
      env_vars = {
        "ASPNETCORE_ENVIRONMENT"               = var.environment
        "ASPNETCORE_URLS"                      = "http://+:8080"
        "SERVICE_NAME"                         = "PaymentService"
        "ConnectionStrings__DefaultConnection" = "Data Source=/tmp/payment.db"
      }
    }

    productservice = {
      tag         = "latest"
      image       = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
      cpu         = "0.5"
      memory      = "1.0Gi"
      target_port = 8080
      external    = false # Internal only
      env_vars = {
        "ASPNETCORE_ENVIRONMENT"               = var.environment
        "ASPNETCORE_URLS"                      = "http://+:8080"
        "SERVICE_NAME"                         = "ProductService"
        "ConnectionStrings__DefaultConnection" = "Data Source=/tmp/product.db"
      }
    }

    userservice = {
      tag         = "latest"
      image       = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
      cpu         = "0.5"
      memory      = "1.0Gi"
      target_port = 8080
      external    = false # Internal only
      env_vars = {
        "ASPNETCORE_ENVIRONMENT"               = var.environment
        "ASPNETCORE_URLS"                      = "http://+:8080"
        "SERVICE_NAME"                         = "UserService"
        "ConnectionStrings__DefaultConnection" = "Data Source=/tmp/user.db"
      }
    }

  }

  depends_on = [
    azurerm_role_assignment.acr_pull_role
  ]

  tags = {
    Environment = var.environment
    Project     = var.prefix
  }
}

# CartService is created separately so it can reference ProductService's FQDN
# for internal product validation calls when adding items to a cart.
resource "azurerm_container_app" "cartservice" {
  name                         = "cartservice-app"
  container_app_environment_id = module.container_apps.environment_id
  resource_group_name          = module.resource_group.name
  revision_mode                = "Single"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.acr_pull_identity.id]
  }

  registry {
    server   = module.container_registry.login_server
    identity = azurerm_user_assigned_identity.acr_pull_identity.id
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "auto"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  template {
    container {
      name   = "cartservice"
      image  = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
      cpu    = "0.5"
      memory = "1.0Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment
      }
      env {
        name  = "ASPNETCORE_URLS"
        value = "http://+:8080"
      }
      env {
        name  = "SERVICE_NAME"
        value = "CartService"
      }
      env {
        name  = "ConnectionStrings__DefaultConnection"
        value = "Data Source=/tmp/cart.db"
      }
      env {
        name  = "ServiceUrls__ProductService"
        value = module.container_apps.app_urls["productservice"]
      }

      liveness_probe {
        transport               = "HTTP"
        path                    = "/health"
        port                    = 8080
        interval_seconds        = 30
        failure_count_threshold = 3
      }

      readiness_probe {
        transport               = "HTTP"
        path                    = "/health"
        port                    = 8080
        interval_seconds        = 5
        failure_count_threshold = 3
      }
    }
  }

  depends_on = [
    module.container_apps,
    azurerm_role_assignment.acr_pull_role
  ]

  tags = {
    Environment = var.environment
    Project     = var.prefix
    ManagedBy   = "Terraform"
  }
}

# API Gateway is created separately so it can reference backend service FQDNs
# as YARP cluster destination addresses via ASP.NET Core env var overrides.
resource "azurerm_container_app" "apigateway" {
  name                         = "apigateway-app"
  container_app_environment_id = module.container_apps.environment_id
  resource_group_name          = module.resource_group.name
  revision_mode                = "Single"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.acr_pull_identity.id]
  }

  registry {
    server   = module.container_registry.login_server
    identity = azurerm_user_assigned_identity.acr_pull_identity.id
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "auto"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  template {
    container {
      name   = "apigateway"
      image  = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
      cpu    = "0.5"
      memory = "1.0Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = var.environment
      }
      env {
        name  = "ASPNETCORE_URLS"
        value = "http://+:8080"
      }
      env {
        name  = "SERVICE_NAME"
        value = "ApiGateway"
      }
      # HttpClient base addresses for the checkout aggregation endpoint
      env {
        name  = "ServiceUrls__ProductService"
        value = module.container_apps.app_urls["productservice"]
      }
      env {
        name  = "ServiceUrls__CartService"
        value = "https://${azurerm_container_app.cartservice.ingress[0].fqdn}"
      }
      env {
        name  = "ServiceUrls__UserService"
        value = module.container_apps.app_urls["userservice"]
      }
      env {
        name  = "ServiceUrls__PaymentService"
        value = module.container_apps.app_urls["paymentservice"]
      }
      # YARP cluster destination overrides — ASP.NET Core maps __ to : in JSON paths
      env {
        name  = "ReverseProxy__Clusters__product-cluster__Destinations__product1__Address"
        value = "${module.container_apps.app_urls["productservice"]}/"
      }
      env {
        name  = "ReverseProxy__Clusters__cart-cluster__Destinations__cart1__Address"
        value = "https://${azurerm_container_app.cartservice.ingress[0].fqdn}/"
      }
      env {
        name  = "ReverseProxy__Clusters__user-cluster__Destinations__user1__Address"
        value = "${module.container_apps.app_urls["userservice"]}/"
      }
      env {
        name  = "ReverseProxy__Clusters__payment-cluster__Destinations__payment1__Address"
        value = "${module.container_apps.app_urls["paymentservice"]}/"
      }

      liveness_probe {
        transport               = "HTTP"
        path                    = "/health"
        port                    = 8080
        interval_seconds        = 30
        failure_count_threshold = 3
      }

      readiness_probe {
        transport               = "HTTP"
        path                    = "/health"
        port                    = 8080
        interval_seconds        = 5
        failure_count_threshold = 3
      }
    }
  }

  depends_on = [
    module.container_apps,
    azurerm_role_assignment.acr_pull_role
  ]

  tags = {
    Environment = var.environment
    Project     = var.prefix
    ManagedBy   = "Terraform"
  }
}

resource "azurerm_static_web_app" "ui" {
  name                = "${var.prefix}-${var.environment}-ui"
  resource_group_name = module.resource_group.name
  location            = "westeurope" # SWA not available in uksouth; available: centralus, eastus2, westus2, westeurope, eastasia
  sku_tier            = "Free"
  sku_size            = "Free"

  tags = {
    Environment = var.environment
    Project     = var.prefix
    ManagedBy   = "Terraform"
  }
}
