locals {
  base_name = "${var.project_name}-${var.environment}"
}

resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location

  tags = {
    project     = var.project_name
    environment = var.environment
    managedBy   = "terraform"
  }
}

resource "azurerm_log_analytics_workspace" "main" {
  name                = "law-${local.base_name}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30

  tags = azurerm_resource_group.main.tags
}

resource "azurerm_application_insights" "main" {
  name                = "appi-${local.base_name}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"

  tags = azurerm_resource_group.main.tags
}

resource "random_string" "acr_suffix" {
  length  = 6
  special = false
  upper   = false
  numeric = true
}

resource "azurerm_container_registry" "main" {
  name                = "acr${replace(var.project_name, "-", "")}${var.environment}${random_string.acr_suffix.result}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "Basic"
  admin_enabled       = false

  tags = azurerm_resource_group.main.tags
}

resource "azurerm_container_app_environment" "main" {
  name                       = "cae-${local.base_name}"
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id

  tags = azurerm_resource_group.main.tags
}

resource "azurerm_key_vault" "main" {
  name                       = "kv-${local.base_name}"
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  rbac_authorization_enabled = true
  purge_protection_enabled   = false
  soft_delete_retention_days = 7

  tags = azurerm_resource_group.main.tags
}

resource "azurerm_redis_cache" "main" {
  count               = var.enable_redis ? 1 : 0
  name                = "redis-${local.base_name}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  capacity            = 0
  family              = "C"
  sku_name            = "Basic"
  minimum_tls_version = "1.2"

  tags = azurerm_resource_group.main.tags
}

data "azurerm_client_config" "current" {}
