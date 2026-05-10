output "resource_group_name" {
  value       = azurerm_resource_group.main.name
  description = "Resource group name"
}

output "container_registry_name" {
  value       = azurerm_container_registry.main.name
  description = "Azure Container Registry name"
}

output "container_registry_login_server" {
  value       = azurerm_container_registry.main.login_server
  description = "Azure Container Registry login server"
}

output "container_app_environment_name" {
  value       = azurerm_container_app_environment.main.name
  description = "Container Apps Environment name"
}

output "application_insights_connection_string" {
  value       = azurerm_application_insights.main.connection_string
  description = "Application Insights connection string"
  sensitive   = true
}

output "key_vault_name" {
  value       = azurerm_key_vault.main.name
  description = "Key Vault name"
}

output "key_vault_uri" {
  value       = azurerm_key_vault.main.vault_uri
  description = "Key Vault URI"
}

output "redis_name" {
  value       = try(azurerm_redis_cache.main[0].name, null)
  description = "Redis cache name when enabled"
}
