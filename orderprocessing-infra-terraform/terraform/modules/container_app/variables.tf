variable "environment_name" {
  description = "Name of the Container Apps Environment"
  type        = string
}

variable "resource_group_name" {
  description = "Resource group name"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
}

variable "log_analytics_workspace_id" {
  description = "Log Analytics Workspace ID for monitoring"
  type        = string
}

variable "infrastructure_subnet_id" {
  description = "Subnet ID for VNET integration (optional)"
  type        = string
  default     = null
}

variable "acr_login_server" {
  description = "ACR login server URL"
  type        = string
}

variable "acr_pull_identity_id" {
  description = "User-assigned identity ID for pulling from ACR"
  type        = string
}

variable "microservices" {
  description = "Map of microservices to deploy"
  type = map(object({
    tag         = string
    cpu         = string
    memory      = string
    target_port = number
    external    = bool
    env_vars    = map(string)
  }))
  default = {}
}

variable "tags" {
  description = "Tags for resources"
  type        = map(string)
  default     = {}
}
