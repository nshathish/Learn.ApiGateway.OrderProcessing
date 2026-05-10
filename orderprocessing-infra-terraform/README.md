# OrderProcessing Azure Infrastructure (Terraform)

Terraform project for provisioning the core Azure infrastructure for `Learn.ApiGateway.OrderProcessing`.

## What this creates

- Resource Group
- Log Analytics Workspace
- Application Insights (workspace-based)
- Azure Container Registry (Basic)
- Azure Container Apps Environment
- Azure Key Vault (RBAC enabled)
- Azure Cache for Redis (optional)

## GitHub environment variable for Resource Group name

This project reads the resource group name from Terraform variable `resource_group_name`.
In GitHub Actions, set it as:

- `TF_VAR_resource_group_name`

Example GitHub Actions env:

```yaml
env:
  TF_VAR_resource_group_name: rg-orderprocessing-dev
  TF_VAR_location: eastus2
  TF_VAR_environment: dev
```

## Usage

```bash
cd azure/orderprocessing-infra-terraform
terraform init
terraform plan
terraform apply
```

## Notes

- Authenticate with Azure before running Terraform (OIDC in GitHub Actions or `az login` locally).
- Redis is enabled by default. Set `TF_VAR_enable_redis=false` to skip it.
