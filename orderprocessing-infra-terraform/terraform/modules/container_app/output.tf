output "id" {
  value = azurerm_container_app.this.id
}

output "system_assigned_identity_principal_id" {
  value       = azurerm_container_app.this.identity[0].principal_id
  description = "Principal ID of the system assigned managed identity"
}
