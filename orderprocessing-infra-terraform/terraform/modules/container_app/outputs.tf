output "environment_id" {
  description = "Container Apps Environment ID"
  value       = azurerm_container_app_environment.this.id
}

output "app_urls" {
  description = "URLs of deployed container apps"
  value = {
    for k, app in azurerm_container_app.microservice :
    k => "https://${app.ingress[0].fqdn}"
  }
}

output "app_names" {
  description = "Names of deployed container apps"
  value = {
    for k, app in azurerm_container_app.microservice :
    k => app.name
  }
}
