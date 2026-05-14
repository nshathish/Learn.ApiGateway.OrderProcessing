resource "azurerm_resource_group" "rg" {
  name     = var.resource_group_name
  location = var.location
  tags     = var.tags
}

module "acr" {
  source = "./modules/acr"

  name                = "${var.project_name}acr"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = var.acr_sku
}

module "log_analytics" {
  source = "./modules/log_analytics"

  name                = "${var.project_name}-law"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
}

module "container_app_env" {
  source = "./modules/container_app_env"

  name                = "${var.project_name}-cae"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  log_analytics_id    = module.log_analytics.workspace_id
}

# Reference existing Container Apps using data sources
data "azurerm_container_app" "cart" {
  name                = "${var.project_name}-cart"
  resource_group_name = var.resource_group_name
}

data "azurerm_container_app" "payment" {
  name                = "${var.project_name}-payment"
  resource_group_name = var.resource_group_name
}

data "azurerm_container_app" "product" {
  name                = "${var.project_name}-product"
  resource_group_name = var.resource_group_name
}

data "azurerm_container_app" "user" {
  name                = "${var.project_name}-user"
  resource_group_name = var.resource_group_name
}

locals {
  cart_principal_id    = try(data.azurerm_container_app.cart.identity[0].principal_id, null)
  payment_principal_id = try(data.azurerm_container_app.payment.identity[0].principal_id, null)
  product_principal_id = try(data.azurerm_container_app.product.identity[0].principal_id, null)
  user_principal_id    = try(data.azurerm_container_app.user.identity[0].principal_id, null)
}

# Grant AcrPull role to each Container App's managed identity
resource "azurerm_role_assignment" "cart_acr_pull" {
  count                = local.cart_principal_id != null ? 1 : 0
  scope                = module.acr.id
  role_definition_name = "AcrPull"
  principal_id         = local.cart_principal_id
}

resource "azurerm_role_assignment" "payment_acr_pull" {
  count                = local.payment_principal_id != null ? 1 : 0
  scope                = module.acr.id
  role_definition_name = "AcrPull"
  principal_id         = local.payment_principal_id
}

resource "azurerm_role_assignment" "product_acr_pull" {
  count                = local.product_principal_id != null ? 1 : 0
  scope                = module.acr.id
  role_definition_name = "AcrPull"
  principal_id         = local.product_principal_id
}

resource "azurerm_role_assignment" "user_acr_pull" {
  count                = local.user_principal_id != null ? 1 : 0
  scope                = module.acr.id
  role_definition_name = "AcrPull"
  principal_id         = local.user_principal_id
}
