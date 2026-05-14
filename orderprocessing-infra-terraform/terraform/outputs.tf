output "acr_login_server" {
  value = module.acr.login_server
}

output "container_app_env_id" {
  value = module.container_app_env.environment_id
}

output "cart_app_fqdn" {
  value = module.cart_app.fqdn
}

output "payment_app_fqdn" {
  value = module.payment_app.fqdn
}

output "product_app_fqdn" {
  value = module.product_app.fqdn
}

output "user_app_fqdn" {
  value = module.user_app.fqdn
}
