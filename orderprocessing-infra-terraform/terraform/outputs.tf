output "acr_login_server" {
  value = module.acr.login_server
}

output "container_app_env_id" {
  value = module.container_app_env.environment_id
}

output "cart_app_fqdn" {
  value = data.azurerm_container_app.cart.ingress[0].fqdn
}

output "payment_app_fqdn" {
  value = data.azurerm_container_app.payment.ingress[0].fqdn
}

output "product_app_fqdn" {
  value = data.azurerm_container_app.product.ingress[0].fqdn
}

output "user_app_fqdn" {
  value = data.azurerm_container_app.user.ingress[0].fqdn
}
