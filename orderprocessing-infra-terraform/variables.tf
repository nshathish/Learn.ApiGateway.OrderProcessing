variable "resource_group_name" {
  description = "Azure Resource Group name. In GitHub, set TF_VAR_resource_group_name."
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "eastus2"
}

variable "environment" {
  description = "Environment label (dev/test/prod)"
  type        = string
  default     = "dev"
}

variable "project_name" {
  description = "Short project name used in resource naming"
  type        = string
  default     = "orderprocessing"
}

variable "enable_redis" {
  description = "Whether to create Azure Cache for Redis"
  type        = bool
  default     = false
}
