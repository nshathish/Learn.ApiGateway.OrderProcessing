resource "azurerm_resource_group" "this" {
  name     = var.name
  location = var.location

  tags = merge(var.tags, {
    ManagedBy = "Terraform"
    Module    = "resource_group"
  })
}

