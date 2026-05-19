# Resource Group
output "resource_group_name" {
  value = module.resource_group.name
}

# ACR
output "acr_name" {
  value = module.container_registry.name
}

output "acr_login_server" {
  value = module.container_registry.login_server
}

# Managed Identity
output "acr_pull_identity_id" {
  value = azurerm_user_assigned_identity.acr_pull_identity.id
}

# Container Apps
output "container_apps_urls" {
  description = "URLs for accessing deployed container apps"
  value       = module.container_apps.app_urls
}

output "container_apps_names" {
  description = "Names of deployed container apps"
  value       = module.container_apps.app_names
}

# Individual service URLs (for convenience)
output "cartservice_url" {
  value = module.container_apps.app_urls["cartservice"]
}

output "paymentservice_url" {
  value = module.container_apps.app_urls["paymentservice"]
}

output "productservice_url" {
  value = module.container_apps.app_urls["productservice"]
}

output "userservice_url" {
  value = module.container_apps.app_urls["userservice"]
}

# Log Analytics
output "log_analytics_workspace_id" {
  value = azurerm_log_analytics_workspace.this.id
}

# Useful commands
output "view_logs_command" {
  description = "Command to view Container App logs"
  value       = "az containerapp logs show --name <app-name> --resource-group ${module.resource_group.name}"
}
