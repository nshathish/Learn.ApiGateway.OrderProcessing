variable "name" {
  description = "Name of the resource group"
  type        = string

  validation {
    condition     = can(regex("^[a-zA-Z0-9-._()]+$", var.name))
    error_message = "Resource group name can only contain alphanumeric characters, periods, underscores, hyphens, and parentheses."
  }
}

variable "location" {
  description = "Location of the resource group"
  type        = string
}

variable "tags" {
  description = "Tags to apply to the resource group"
  type        = map(string)
  default     = {}
}

variable "prevent_deletion" {
  description = "Whether to prevent deletion of the resource group"
  type        = bool
  default     = false
}
