terraform {
  required_version = ">= 1.6.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.72.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  backend "azurerm" {
    resource_group_name  = "learn-labs"
    storage_account_name = "softinovgenericstorage"
    container_name       = "tfstatestoragev2"
    key                  = "terraform.tfstate"
  }
}

provider "azurerm" {
  features {}
}
