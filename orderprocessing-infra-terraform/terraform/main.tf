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

# CartService
module "cart_app" {
  source = "./modules/container_app"

  name                = "${var.project_name}-cart"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location

  environment_id = module.container_app_env.environment_id
  image          = var.cart_image
  container_port = var.container_port
  cpu            = var.container_cpu
  memory         = var.container_memory

  acr_id           = module.acr.id
  acr_login_server = module.acr.login_server
}

resource "azurerm_role_assignment" "cart_acr_pull" {
  scope              = module.acr.id
  role_definition_name = "AcrPull"
  principal_id       = module.cart_app.system_assigned_identity_principal_id
}

# PaymentService
module "payment_app" {
  source = "./modules/container_app"

  name                = "${var.project_name}-payment"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location

  environment_id = module.container_app_env.environment_id
  image          = var.payment_image
  container_port = var.container_port
  cpu            = var.container_cpu
  memory         = var.container_memory

  acr_id           = module.acr.id
  acr_login_server = module.acr.login_server
}

resource "azurerm_role_assignment" "payment_acr_pull" {
  scope              = module.acr.id
  role_definition_name = "AcrPull"
  principal_id       = module.payment_app.system_assigned_identity_principal_id
}

# ProductService
module "product_app" {
  source = "./modules/container_app"

  name                = "${var.project_name}-product"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location

  environment_id = module.container_app_env.environment_id
  image          = var.product_image
  container_port = var.container_port
  cpu            = var.container_cpu
  memory         = var.container_memory

  acr_id           = module.acr.id
  acr_login_server = module.acr.login_server
}

resource "azurerm_role_assignment" "product_acr_pull" {
  scope              = module.acr.id
  role_definition_name = "AcrPull"
  principal_id       = module.product_app.system_assigned_identity_principal_id
}

# UserService
module "user_app" {
  source = "./modules/container_app"

  name                = "${var.project_name}-user"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location

  environment_id = module.container_app_env.environment_id
  image          = var.user_image
  container_port = var.container_port
  cpu            = var.container_cpu
  memory         = var.container_memory

  acr_id           = module.acr.id
  acr_login_server = module.acr.login_server
}

resource "azurerm_role_assignment" "user_acr_pull" {
  scope              = module.acr.id
  role_definition_name = "AcrPull"
  principal_id       = module.user_app.system_assigned_identity_principal_id
}
