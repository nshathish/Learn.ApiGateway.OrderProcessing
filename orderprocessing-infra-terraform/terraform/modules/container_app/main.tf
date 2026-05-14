resource "azurerm_container_app" "this" {
  name                         = var.name
  resource_group_name          = var.resource_group_name
  container_app_environment_id = var.environment_id
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  registry {
    server = var.acr_login_server
  }

  template {
    container {
      name   = "app"
      image  = var.image
      cpu    = var.cpu
      memory = var.memory

      liveness_probe {
        transport        = "HTTP"
        path             = "/health"
        port             = var.container_port
        initial_delay    = 5
        interval_seconds = 30
      }
    }
  }

  ingress {
    external_enabled = true
    target_port      = var.container_port
    transport        = "auto"
    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }
}


