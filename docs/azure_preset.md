# Azure Prerequisites for GitHub CI/CD

This Terraform project creates the application infrastructure in Azure. You do not need to pre-create the resources defined in Terraform (`main.tf`).

## What must exist before running in GitHub Actions

### 1. Azure identity for GitHub OIDC
- Create a Microsoft Entra app registration / service principal for GitHub Actions.
- Add a federated credential for your repository (branch or environment scoped).
- Grant the identity sufficient permissions (recommended: `Contributor` at subscription or target scope).

### 2. GitHub repository secrets/variables for Azure login
Set these in repository or environment settings:
- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

### 3. Terraform input variables
Required:
- `TF_VAR_resource_group_name`

Optional overrides (defaults are in `variables.tf`):
- `TF_VAR_location`
- `TF_VAR_environment`
- `TF_VAR_project_name`
- `TF_VAR_enable_redis`

### 4. GitHub Actions workflow permissions
Your workflow must include:
- `id-token: write`
- `contents: read`

## Strongly recommended: Remote Terraform state
This project currently has no backend block in `providers.tf`, so CI runners will use local ephemeral state.

For team-safe CI/CD, configure a remote backend (Azure Storage). If you do this, pre-create:
- A resource group for state (optional but recommended)
- A storage account for Terraform state
- A blob container for the state file

## Minimal workflow flow
1. Azure login using OIDC (`azure/login` action)
2. `terraform init`
3. `terraform plan`
4. `terraform apply` (typically guarded by environment approval)
