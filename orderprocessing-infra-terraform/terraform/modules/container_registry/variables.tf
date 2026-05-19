variable "name" {
  description = "Name of the container registry (must be globally unique)"
  type        = string

  validation {
    condition     = can(regex("^[a-zA-Z0-9]+$", var.name))
    error_message = "Container registry name must contain only letters and numbers, no hyphens."
  }
}

variable "resource_group_name" {
  description = "Name of the resource group where ACR will be created"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
}

variable "sku" {
  description = "ACR SKU: Basic (cheapest), Standard, or Premium"
  type        = string
  default     = "Basic"

  validation {
    condition     = contains(["Basic", "Standard", "Premium"], var.sku)
    error_message = "SKU must be Basic, Standard, or Premium."
  }
}

variable "enable_managed_identity" {
  description = "Enable system-assigned managed identity for the ACR"
  type        = bool
  default     = true # Enabled by default for security
}

variable "tags" {
  description = "Tags to apply to the container registry"
  type        = map(string)
  default     = {}
}
