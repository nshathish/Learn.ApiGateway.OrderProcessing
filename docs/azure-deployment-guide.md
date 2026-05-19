# Azure Deployment Guide

## Architecture Overview

```
Internet
    │
    ▼
Azure Static Web Apps (Angular SPA)
    │  /api/* proxied to
    ▼
Azure Container Apps — apigateway-app (external ingress, port 80)
    │  YARP routes via internal DNS
    ├──▶ cartservice-app    (internal)  ──▶ SQLite (ephemeral or Azure Files volume)
    │        └──▶ productservice-app (internal)
    ├──▶ productservice-app (internal)  ──▶ SQLite (ephemeral or Azure Files volume)
    ├──▶ userservice-app    (internal)  ──▶ SQLite (ephemeral or Azure Files volume)
    └──▶ paymentservice-app (internal)  ──▶ SQLite (ephemeral or Azure Files volume)
                                           └──▶ Stripe API (external)

All Container Apps:
    ├──▶ Azure Key Vault       (secrets: JWT key, Stripe key, Redis conn string)
    ├──▶ Azure Container Registry  (pull images via User-Assigned Managed Identity)
    └──▶ Log Analytics Workspace   (container logs)

ApiGateway only:
    └──▶ Azure Cache for Redis (distributed rate limiting)
```

> **SQLite note:** Each service's SQLite database file lives on the container filesystem.
> Data is **ephemeral** by default — it resets on every redeploy/restart. For persistence,
> mount an Azure Files share (see [SQLite Persistence](#sqlite-persistence-in-containers)).
> Because SQLite cannot handle concurrent writers, keep `min_replicas = 0` and
> `max_replicas = 1` per service (already the Container Apps Consumption default).

---

## Azure Services Required

| Component | Azure Service | SKU | Notes |
|-----------|--------------|-----|-------|
| ApiGateway | Container Apps | Consumption | External ingress only |
| CartService | Container Apps | Consumption | Internal ingress only |
| ProductService | Container Apps | Consumption | Internal ingress only |
| UserService | Container Apps | Consumption | Internal ingress only |
| PaymentService | Container Apps | Consumption | Internal ingress only |
| Shared networking | Container Apps Environment | — | Deployed by `modules/container_app` |
| Container images | Azure Container Registry | Basic | Deployed by `modules/container_registry` |
| Secrets | Azure Key Vault | Standard | One shared vault — **Missing** |
| Caching | Azure Cache for Redis | Basic C0 | ApiGateway rate limiting — **Missing** |
| Frontend SPA | Azure Static Web Apps | Free | Angular app — **Missing** |
| Monitoring | Log Analytics Workspace | PerGB2018 | Direct resource in `main.tf` — Done |
| SQLite persistence (optional) | Azure Files share | LRS | For data that survives redeployments |

---

## Terraform Coverage

The actual module structure differs from earlier drafts of this guide.

| Resource | Terraform reference | Status |
|----------|--------------------|---------| 
| Resource Group | `module.resource_group` | Done |
| Azure Container Registry | `module.container_registry` | Done |
| Log Analytics Workspace | `azurerm_log_analytics_workspace.this` (direct resource) | Done |
| User-Assigned Identity (ACR pull) | `azurerm_user_assigned_identity.acr_pull_identity` | Done |
| AcrPull role assignment | `azurerm_role_assignment.acr_pull_role` | Done |
| Container Apps Environment | Embedded in `module.container_apps` | Done |
| Container Apps (4 services) | `module.container_apps` via `microservices` map | Done |
| ApiGateway Container App | Add to `microservices` map in `main.tf` | **Missing** |
| Azure Key Vault | `modules/key_vault` (not yet created) | **Missing** |
| Azure Cache for Redis | `modules/redis` (not yet created) | **Missing** |
| Azure Static Web Apps | `modules/static_web_app` (not yet created) | **Missing** |
| Application Insights | Not planned — using Log Analytics directly | Not needed |

### Naming conventions in use

| Pattern | Example (prefix=`azlabs13600`, env=`dev`) |
|---------|------------------------------------------|
| Resource Group | `azlabs13600-dev-rg` |
| ACR | `azlabs13600devacr` (no hyphens — ACR constraint) |
| Log Analytics | `azlabs13600-dev-logs` |
| Container App Env | `azlabs13600-dev-env` |
| Container Apps | `cartservice-app`, `productservice-app`, … |
| Key Vault | `azlabs13600-dev-kv` |
| Redis | `azlabs13600-dev-redis` |
| Static Web App | `azlabs13600-dev-ui` |

---

## Step 1 — Add ApiGateway to the microservices map

In `orderprocessing-infra-terraform/terraform/main.tf`, add `apigateway` to the `microservices`
map inside `module "container_apps"`. Also switch `cartservice` to `external = false` so only
the gateway is publicly reachable:

```hcl
module "container_apps" {
  source = "./modules/container_app"

  environment_name           = "${var.prefix}-${var.environment}-env"
  resource_group_name        = module.resource_group.name
  location                   = module.resource_group.location
  log_analytics_workspace_id = azurerm_log_analytics_workspace.this.id
  acr_login_server           = module.container_registry.login_server
  acr_pull_identity_id       = azurerm_user_assigned_identity.acr_pull_identity.id

  microservices = {
    apigateway = {
      tag         = "latest"
      cpu         = "0.5"
      memory      = "1.0Gi"
      target_port = 80
      external    = true   # Only the gateway is public
      env_vars = {
        "ASPNETCORE_ENVIRONMENT" = var.environment
        "SERVICE_NAME"           = "ApiGateway"
      }
    }

    cartservice = {
      tag         = "latest"
      cpu         = "0.5"
      memory      = "1.0Gi"
      target_port = 80
      external    = false  # Internal — reached via gateway
      env_vars = {
        "ASPNETCORE_ENVIRONMENT" = var.environment
        "SERVICE_NAME"           = "CartService"
      }
    }

    paymentservice = {
      tag         = "latest"
      cpu         = "0.5"
      memory      = "1.0Gi"
      target_port = 80
      external    = false
      env_vars = {
        "ASPNETCORE_ENVIRONMENT" = var.environment
        "SERVICE_NAME"           = "PaymentService"
      }
    }

    productservice = {
      tag         = "latest"
      cpu         = "0.5"
      memory      = "1.0Gi"
      target_port = 80
      external    = false
      env_vars = {
        "ASPNETCORE_ENVIRONMENT" = var.environment
        "SERVICE_NAME"           = "ProductService"
      }
    }

    userservice = {
      tag         = "latest"
      cpu         = "0.5"
      memory      = "1.0Gi"
      target_port = 80
      external    = false
      env_vars = {
        "ASPNETCORE_ENVIRONMENT" = var.environment
        "SERVICE_NAME"           = "UserService"
      }
    }
  }

  depends_on = [azurerm_role_assignment.acr_pull_role]

  tags = {
    Environment = var.environment
    Project     = var.prefix
  }
}
```

Add the gateway URL to `outputs.tf`:

```hcl
output "apigateway_url" {
  value = module.container_apps.app_urls["apigateway"]
}
```

---

## Step 2 — Missing Terraform Modules

### `modules/key_vault/variables.tf`

```hcl
variable "name" { type = string }
variable "resource_group_name" { type = string }
variable "location" { type = string }
variable "tenant_id" { type = string }
variable "app_principal_ids" { type = map(string) }
variable "tags" { type = map(string); default = {} }
```

### `modules/key_vault/main.tf`

```hcl
data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "this" {
  name                       = var.name
  resource_group_name        = var.resource_group_name
  location                   = var.location
  tenant_id                  = var.tenant_id
  sku_name                   = "standard"
  soft_delete_retention_days = 7
  purge_protection_enabled   = false

  access_policy {
    tenant_id = var.tenant_id
    object_id = data.azurerm_client_config.current.object_id
    secret_permissions = ["Get", "Set", "List", "Delete", "Purge"]
  }

  tags = var.tags
}

resource "azurerm_key_vault_access_policy" "app" {
  for_each = var.app_principal_ids

  key_vault_id = azurerm_key_vault.this.id
  tenant_id    = var.tenant_id
  object_id    = each.value
  secret_permissions = ["Get", "List"]
}
```

### `modules/key_vault/outputs.tf`

```hcl
output "id"  { value = azurerm_key_vault.this.id }
output "uri" { value = azurerm_key_vault.this.vault_uri }
```

---

### `modules/redis/variables.tf`

```hcl
variable "name" { type = string }
variable "resource_group_name" { type = string }
variable "location" { type = string }
variable "tags" { type = map(string); default = {} }
```

### `modules/redis/main.tf`

```hcl
resource "azurerm_redis_cache" "this" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
  capacity            = 0
  family              = "C"
  sku_name            = "Basic"
  enable_non_ssl_port = false
  minimum_tls_version = "1.2"
  tags                = var.tags
}
```

### `modules/redis/outputs.tf`

```hcl
output "hostname"   { value = azurerm_redis_cache.this.hostname }
output "ssl_port"   { value = azurerm_redis_cache.this.ssl_port }

output "connection_string" {
  value     = "${azurerm_redis_cache.this.hostname}:${azurerm_redis_cache.this.ssl_port},password=${azurerm_redis_cache.this.primary_access_key},ssl=True,abortConnect=False"
  sensitive = true
}

output "primary_access_key" {
  value     = azurerm_redis_cache.this.primary_access_key
  sensitive = true
}
```

---

### `modules/static_web_app/variables.tf`

```hcl
variable "name" { type = string }
variable "resource_group_name" { type = string }
variable "location" {
  type        = string
  description = "Supported regions: eastus2, westus2, centralus, eastasia, westeurope, eastus, uksouth"
}
variable "tags" { type = map(string); default = {} }
```

### `modules/static_web_app/main.tf`

```hcl
resource "azurerm_static_web_app" "this" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
  sku_tier            = "Free"
  sku_size            = "Free"
  tags                = var.tags
}
```

### `modules/static_web_app/outputs.tf`

```hcl
output "default_hostname" { value = azurerm_static_web_app.this.default_host_name }

output "api_key" {
  value       = azurerm_static_web_app.this.api_key
  sensitive   = true
  description = "Deployment token — add to GitHub Actions secret SWA_DEPLOY_TOKEN"
}
```

---

## Step 3 — Wire up Key Vault, Redis, and Static Web App in `main.tf`

The `module.container_apps` module uses a single `UserAssigned` identity shared across all
apps (`azurerm_user_assigned_identity.acr_pull_identity`). Use that identity's principal ID for
Key Vault access policies.

```hcl
data "azurerm_client_config" "current" {}

# ── Key Vault ────────────────────────────────────────────────────────────────
module "key_vault" {
  source = "./modules/key_vault"

  name                = "${var.prefix}-${var.environment}-kv"
  resource_group_name = module.resource_group.name
  location            = module.resource_group.location
  tenant_id           = data.azurerm_client_config.current.tenant_id
  tags = {
    Environment = var.environment
    Project     = var.prefix
  }

  # All services share the same user-assigned identity for ACR pull;
  # grant it Key Vault read access for secrets.
  app_principal_ids = {
    apps = azurerm_user_assigned_identity.acr_pull_identity.principal_id
  }
}

# ── Redis ────────────────────────────────────────────────────────────────────
module "redis" {
  source = "./modules/redis"

  name                = "${var.prefix}-${var.environment}-redis"
  resource_group_name = module.resource_group.name
  location            = module.resource_group.location
  tags = {
    Environment = var.environment
    Project     = var.prefix
  }
}

# ── Key Vault Secrets ─────────────────────────────────────────────────────────
resource "azurerm_key_vault_secret" "redis_conn" {
  name         = "Redis--ConnectionString"
  value        = module.redis.connection_string
  key_vault_id = module.key_vault.id
}

# Add Stripe secret manually after first apply (see Deployment Steps)

# ── Static Web App ────────────────────────────────────────────────────────────
module "static_web_app" {
  source = "./modules/static_web_app"

  name                = "${var.prefix}-${var.environment}-ui"
  resource_group_name = module.resource_group.name
  location            = module.resource_group.location
  tags = {
    Environment = var.environment
    Project     = var.prefix
  }
}
```

Add to `outputs.tf`:

```hcl
output "static_web_app_url" {
  value = module.static_web_app.default_hostname
}

output "key_vault_uri" {
  value = module.key_vault.uri
}
```

---

## SQLite Persistence in Containers

By default, SQLite database files live on the container's ephemeral filesystem and are
**deleted on every redeploy or restart**. For a dev/learning environment this is often fine —
EF Core recreates the schema on startup if you call `EnsureCreated()`.

### Option A — Ephemeral (default, no extra Azure resources)

No changes needed. Data resets on each deploy. Suitable for testing.

### Option B — Persistent via Azure Files

Create an Azure Files share and mount it into each container app. The SQLite file persists
across restarts and redeployments.

1. Create a storage account and file share (add to `main.tf`):

```hcl
resource "azurerm_storage_account" "sqlite" {
  name                     = "${var.prefix}${var.environment}sqlite"
  resource_group_name      = module.resource_group.name
  location                 = module.resource_group.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_share" "db" {
  for_each = toset(["cartservice", "productservice", "userservice", "paymentservice"])

  name                 = "${each.key}-db"
  storage_account_id   = azurerm_storage_account.sqlite.id
  quota                = 1
}
```

2. Add the storage account to the Container Apps Environment:

```hcl
resource "azurerm_container_app_environment_storage" "db" {
  for_each = toset(["cartservice", "productservice", "userservice", "paymentservice"])

  name                         = "${each.key}-storage"
  container_app_environment_id = azurerm_container_app_environment.this.id
  account_name                 = azurerm_storage_account.sqlite.name
  share_name                   = azurerm_storage_share.db[each.key].name
  access_key                   = azurerm_storage_account.sqlite.primary_access_key
  access_mode                  = "ReadWrite"
}
```

3. Mount the volume in each service's container template via the `modules/container_app` module
   (requires adding `volume` and `volume_mount` blocks to `main.tf` there), then set the
   connection string to point at the mounted path, e.g.
   `Data Source=/data/app.db`.

> The `azurerm_container_app_environment_storage` resource is not yet in the
> `modules/container_app` module — you'll need to add volume mount support there or wire
> it up directly in `main.tf`.

---

## Application Code — No Database Change Required

Keep SQLite and the existing EF Core setup. No provider switch needed.

### Key Vault Configuration (each service `Program.cs`)

```csharp
// NuGet: Azure.Extensions.AspNetCore.Configuration.Secrets + Azure.Identity
if (builder.Environment.IsProduction())
{
    var kvUri = new Uri($"https://{builder.Configuration["KeyVault:Name"]}.vault.azure.net/");
    builder.Configuration.AddAzureKeyVault(kvUri, new DefaultAzureCredential());
}
```

Set `KeyVault__Name` as an environment variable on each Container App to
`${var.prefix}-${var.environment}-kv` (e.g. `azlabs13600-dev-kv`). Add it to the
`env_vars` map for each microservice in `main.tf`.

### SQLite connection string for containers

In `appsettings.json` (or via env var), point the connection string at a writable path:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/tmp/app.db"
  }
}
```

Use `/tmp/app.db` for ephemeral storage or `/data/app.db` if you mount an Azure Files volume
at `/data`.

---

## Deployment Steps

### First-time Setup

```bash
# 1. Authenticate
az login
az account set --subscription <subscription-id>

# 2. Build and push images to ACR (name: azlabs13600devacr)
az acr login --name azlabs13600devacr
docker build -t azlabs13600devacr.azurecr.io/apigateway:latest     -f ApiGateway/Dockerfile .
docker build -t azlabs13600devacr.azurecr.io/cartservice:latest    -f CartService/Dockerfile .
docker build -t azlabs13600devacr.azurecr.io/productservice:latest -f ProductService/Dockerfile .
docker build -t azlabs13600devacr.azurecr.io/userservice:latest    -f UserService/Dockerfile .
docker build -t azlabs13600devacr.azurecr.io/paymentservice:latest -f PaymentService/Dockerfile .
docker push azlabs13600devacr.azurecr.io/apigateway:latest
docker push azlabs13600devacr.azurecr.io/cartservice:latest
docker push azlabs13600devacr.azurecr.io/productservice:latest
docker push azlabs13600devacr.azurecr.io/userservice:latest
docker push azlabs13600devacr.azurecr.io/paymentservice:latest

# 3. Apply infrastructure
cd orderprocessing-infra-terraform/terraform
terraform init
terraform apply

# 4. Add Stripe secret to Key Vault manually (after apply)
az keyvault secret set \
  --vault-name azlabs13600-dev-kv \
  --name "Stripe--SecretKey" \
  --value "<stripe-secret-key>"

# 5. Deploy Angular SPA
cd OrderProcessingUI
pnpm build
# Upload dist/ to Static Web App using the api_key output from terraform
```

### Updating a service image

```bash
docker build -t azlabs13600devacr.azurecr.io/productservice:latest -f ProductService/Dockerfile .
docker push azlabs13600devacr.azurecr.io/productservice:latest
az containerapp update \
  --name productservice-app \
  --resource-group azlabs13600-dev-rg \
  --image azlabs13600devacr.azurecr.io/productservice:latest
```

### GitHub Actions CI/CD (recommended)

Add the Static Web App API key and ACR credentials as GitHub Actions secrets, then use:
- `azure/container-apps-deploy-action` for the backend services
- `azure/static-web-apps-deploy` for the Angular frontend

---

## Estimated Monthly Cost

| Service | SKU | Cost/month |
|---------|-----|-----------|
| Container Apps × 5 (low traffic) | Consumption | ~$5–10 |
| Azure Container Registry | Basic | ~$5 |
| Azure Key Vault | Standard | ~$1 |
| Azure Cache for Redis | Basic C0 | ~$16 |
| Log Analytics Workspace | PerGB2018 | ~$2 |
| Static Web Apps | Free | $0 |
| Azure Files (SQLite persistence, optional) | Standard LRS | ~$1 |
| **Total** | | **~$29–35/month** |

> Redis is optional for MVP — without it, rate limiting in ApiGateway is per-instance rather
> than global, but everything else works. Dropping Redis saves ~$16/month (~$13–19/month total).
>
> Compared to using Azure SQL (4 × Basic = ~$20/month), SQLite saves ~$20/month with no
> meaningful tradeoff for a single-instance dev/learning setup.
