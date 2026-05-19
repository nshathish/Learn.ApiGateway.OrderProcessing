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
    cartservice = {
      tag         = "latest"
      cpu         = "0.5"
      memory      = "1.0Gi"
      target_port = 80
      external    = true # Publicly accessible
      env_vars = {
        "ASPNETCORE_ENVIRONMENT" = var.environment
        "SERVICE_NAME"           = "CartService"
      }
    }

    paymentservice = {
      tag         = "latest"
      cpu         = "0.5"
      memory      = "1.0Gi"
      target_port = 80
      external    = false # Internal only
      env_vars = {
        "ASPNETCORE_ENVIRONMENT" = var.environment
        "SERVICE_NAME"           = "PaymentService"
      }
    }

    productservice = {
      tag         = "latest"
      cpu         = "0.5"
      memory      = "1.0Gi"
      target_port = 80
      external    = false # Internal only
      env_vars = {
        "ASPNETCORE_ENVIRONMENT" = var.environment
        "SERVICE_NAME"           = "ProductService"
      }
    }

    userservice = {
      tag         = "latest"
      cpu         = "0.5"
      memory      = "1.0Gi"
      target_port = 80
      external    = false # Internal only
      env_vars = {
        "ASPNETCORE_ENVIRONMENT" = var.environment
        "SERVICE_NAME"           = "UserService"
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
