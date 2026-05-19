resource "azurerm_container_app_environment" "this" {
  name                       = var.environment_name
  location                   = var.location
  resource_group_name        = var.resource_group_name
  log_analytics_workspace_id = var.log_analytics_workspace_id

  # Infrastructure subnet (optional - for VNET integration)
  infrastructure_subnet_id = var.infrastructure_subnet_id

  tags = var.tags
}

resource "azurerm_container_app" "microservice" {
  for_each = var.microservices

  name                         = "${each.key}-app"
  container_app_environment_id = azurerm_container_app_environment.this.id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"

  identity {
    type         = "UserAssigned"
    identity_ids = [var.acr_pull_identity_id]
  }

  registry {
    server   = var.acr_login_server
    identity = var.acr_pull_identity_id
  }

  ingress {
    external_enabled = each.value.external
    target_port      = each.value.target_port
    transport        = "auto"

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  template {
    container {
      name   = each.key
      image  = "${var.acr_login_server}/${each.key}:${each.value.tag}"
      cpu    = each.value.cpu
      memory = each.value.memory

      dynamic "env" {
        for_each = each.value.env_vars
        content {
          name  = env.key
          value = env.value
        }
      }

      liveness_probe {
        transport               = "HTTP"
        path                    = "/health"
        port                    = each.value.target_port
        interval_seconds        = 30
        failure_count_threshold = 3
      }

      readiness_probe {
        transport               = "HTTP"
        port                    = each.value.target_port
        path                    = "/health"
        interval_seconds        = 5
        failure_count_threshold = 3
      }
    }
  }

  tags = var.tags
}
