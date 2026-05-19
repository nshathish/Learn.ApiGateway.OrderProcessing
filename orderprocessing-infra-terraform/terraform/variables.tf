variable "prefix" {
  description = "Prefix for resource names"
  type        = string
  default     = "azlabs13600"
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "uksouth"
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "dev"
}

variable "acr_sku" {
  description = "Container registry SKU"
  type        = string
  default     = "Basic"
}

variable "enable_acr_managed_identity" {
  description = "Enable managed identity for ACR"
  type        = bool
  default     = true
}

# Microservice specific variables (optional overrides)
variable "microservice_tags" {
  description = "Tags for each microservice"
  type        = map(string)
  default = {
    cartservice    = "latest"
    paymentservice = "latest"
    productservice = "latest"
    userservice    = "latest"
    apigateway     = "latest"
  }
}
