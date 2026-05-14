variable "project_name" {
  description = "Prefix for resource names"
  type        = string
  default     = "az204labs"
}

variable "resource_group_name" {
  description = "The name of the resource group"
  type        = string
  default     = "az204labs25715rg"
}

variable "location" {
  description = "Azure region for resources"
  type        = string
  default     = "uksouth"
}

variable "acr_sku" {
  type    = string
  default = "Basic"
}

variable "cart_image" {
  type        = string
  description = "ACR image tag for CartService"
}

variable "payment_image" {
  type        = string
  description = "ACR image tag for PaymentService"
}

variable "product_image" {
  type        = string
  description = "ACR image tag for ProductService"
}

variable "user_image" {
  type        = string
  description = "ACR image tag for UserService"
}

variable "container_cpu" {
  type    = number
  default = 0.5
}

variable "container_memory" {
  type    = string
  default = "1Gi"
}

variable "container_port" {
  type    = number
  default = 8080
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default = {
    Environment = "Development"
    ManagedBy   = "Terraform"
    Project     = "Azure.Infrastructure"
  }
}
