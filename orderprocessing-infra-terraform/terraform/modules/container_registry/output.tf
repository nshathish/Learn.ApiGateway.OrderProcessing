output "id" {
  description = "ACR resource ID"
  value       = azurerm_container_registry.this.id
}

output "name" {
  description = "ACR name"
  value       = azurerm_container_registry.this.name
}

output "login_server" {
  description = "Login server URL (use this to login with Docker)"
  value       = azurerm_container_registry.this.login_server
}

output "identity_principal_id" {
  description = "Principal ID of the system-assigned managed identity (use this for role assignments)"
  value       = var.enable_managed_identity ? azurerm_container_registry.this.identity[0].principal_id : null
}

output "identity_tenant_id" {
  description = "Tenant ID of the system-assigned managed identity"
  value       = var.enable_managed_identity ? azurerm_container_registry.this.identity[0].tenant_id : null
}

output "has_managed_identity" {
  description = "Whether managed identity is enabled"
  value       = var.enable_managed_identity
}
