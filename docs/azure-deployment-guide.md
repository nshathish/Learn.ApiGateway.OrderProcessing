# Azure Deployment Guide

## Architecture Overview

```
Internet
    │
    ▼
Azure Static Web Apps (Angular SPA)
    │  /api/* proxied to
    ▼
Azure Container Apps — ApiGateway (external ingress, port 8080)
    │  YARP routes via internal DNS
    ├──▶ ca-cartservice    (internal)  ──▶ Azure SQL (carts-db)
    │        └──▶ ca-productservice (internal)
    ├──▶ ca-productservice (internal)  ──▶ Azure SQL (products-db)
    ├──▶ ca-userservice    (internal)  ──▶ Azure SQL (users-db)
    └──▶ ca-paymentservice (internal)  ──▶ Azure SQL (payments-db)
                                           └──▶ Stripe API (external)

All Container Apps:
    ├──▶ Azure Key Vault       (secrets: JWT key, Stripe key, conn strings, Redis)
    ├──▶ Azure Container Registry  (pull images via Managed Identity)
    └──▶ Application Insights  (OpenTelemetry traces/logs)

ApiGateway only:
    └──▶ Azure Cache for Redis (distributed rate limiting)
```

---

## Azure Services Required

| Component | Azure Service | SKU | Notes |
|-----------|--------------|-----|-------|
| ApiGateway | Container Apps | Consumption | External ingress only |
| CartService | Container Apps | Consumption | Internal ingress only |
| ProductService | Container Apps | Consumption | Internal ingress only |
| UserService | Container Apps | Consumption | Internal ingress only |
| PaymentService | Container Apps | Consumption | Internal ingress only |
| Shared networking | Container Apps Environment | — | Already in Terraform |
| Container images | Azure Container Registry | Basic | Already in Terraform |
| Databases × 4 | Azure SQL Database | Basic 5 DTU | One per service |
| Secrets | Azure Key Vault | Standard | One shared vault |
| Caching | Azure Cache for Redis | Basic C0 | ApiGateway rate limiting |
| Frontend SPA | Azure Static Web Apps | Free | Angular 21 app |
| Monitoring | Application Insights | Pay-per-use | Linked to Log Analytics |
| Log store | Log Analytics Workspace | PerGB2018 | Already in Terraform |

---

## Terraform Coverage

| Resource | Status |
|----------|--------|
| Resource Group | Done |
| Azure Container Registry | Done (`modules/acr`) |
| Log Analytics Workspace | Done (`modules/log_analytics`) |
| Container Apps Environment | Done (`modules/container_app_env`) |
| Container App module | Done (`modules/container_app`) — but not instantiated for all services |
| AcrPull role assignments | Done (data-source based, fragile) |
| Azure SQL Server + Databases | **Missing** |
| Azure Key Vault | **Missing** |
| Azure Cache for Redis | **Missing** |
| Azure Static Web Apps | **Missing** |
| Application Insights | **Missing** |
| Container Apps for all 5 services | **Missing** (data sources reference pre-existing resources) |

---

## Missing Terraform Modules

### `modules/key_vault/variables.tf`

```hcl
variable "name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "tenant_id" {
  type        = string
  description = "Azure AD tenant ID — use data.azurerm_client_config.current.tenant_id"
}

variable "tags" {
  type    = map(string)
  default = {}
}
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

  # Allow the deploying principal to manage secrets during bootstrap
  access_policy {
    tenant_id = var.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    secret_permissions = ["Get", "Set", "List", "Delete", "Purge"]
  }

  tags = var.tags
}

# Grant each Container App's Managed Identity read access to secrets
resource "azurerm_key_vault_access_policy" "app" {
  for_each = var.app_principal_ids

  key_vault_id = azurerm_key_vault.this.id
  tenant_id    = var.tenant_id
  object_id    = each.value

  secret_permissions = ["Get", "List"]
}
```

> Add `variable "app_principal_ids" { type = map(string) }` to `variables.tf` — pass a map of
> `{ cart = module.ca_cart.system_assigned_identity_principal_id, ... }` from `main.tf`.

### `modules/key_vault/outputs.tf`

```hcl
output "id" {
  value = azurerm_key_vault.this.id
}

output "uri" {
  value = azurerm_key_vault.this.vault_uri
}
```

---

### `modules/sql_server/variables.tf`

```hcl
variable "server_name" {
  type        = string
  description = "Globally unique SQL Server name"
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "database_names" {
  type        = list(string)
  description = "Names of databases to create (one per service)"
}

variable "admin_login" {
  type        = string
  description = "SQL admin username — store password in Key Vault, not here"
}

variable "admin_password" {
  type      = string
  sensitive = true
}

variable "tags" {
  type    = map(string)
  default = {}
}
```

### `modules/sql_server/main.tf`

```hcl
resource "azurerm_mssql_server" "this" {
  name                         = var.server_name
  resource_group_name          = var.resource_group_name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = var.admin_login
  administrator_login_password = var.admin_password

  azuread_administrator {
    login_username              = "AzureAD Admin"
    object_id                   = data.azurerm_client_config.current.object_id
    azuread_authentication_only = false
  }

  tags = var.tags
}

data "azurerm_client_config" "current" {}

resource "azurerm_mssql_database" "this" {
  for_each  = toset(var.database_names)
  name      = each.key
  server_id = azurerm_mssql_server.this.id
  sku_name  = "Basic"   # 5 DTU — ~$5/month each
  tags      = var.tags
}

# Allow Azure services (Container Apps) to reach the server
resource "azurerm_mssql_firewall_rule" "azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.this.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}
```

### `modules/sql_server/outputs.tf`

```hcl
output "server_fqdn" {
  value = azurerm_mssql_server.this.fully_qualified_domain_name
}

output "database_names" {
  value = keys(azurerm_mssql_database.this)
}
```

---

### `modules/redis/variables.tf`

```hcl
variable "name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "tags" {
  type    = map(string)
  default = {}
}
```

### `modules/redis/main.tf`

```hcl
resource "azurerm_redis_cache" "this" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
  capacity            = 0
  family              = "C"
  sku_name            = "Basic"   # C0 — ~$16/month
  enable_non_ssl_port = false
  minimum_tls_version = "1.2"
  tags                = var.tags
}
```

### `modules/redis/outputs.tf`

```hcl
output "hostname" {
  value = azurerm_redis_cache.this.hostname
}

output "ssl_port" {
  value = azurerm_redis_cache.this.ssl_port
}

output "primary_access_key" {
  value     = azurerm_redis_cache.this.primary_access_key
  sensitive = true
}

output "connection_string" {
  value     = "${azurerm_redis_cache.this.hostname}:${azurerm_redis_cache.this.ssl_port},password=${azurerm_redis_cache.this.primary_access_key},ssl=True,abortConnect=False"
  sensitive = true
}
```

---

### `modules/app_insights/variables.tf`

```hcl
variable "name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "workspace_id" {
  type        = string
  description = "Log Analytics Workspace ID — use module.log_analytics.workspace_id"
}

variable "tags" {
  type    = map(string)
  default = {}
}
```

### `modules/app_insights/main.tf`

```hcl
resource "azurerm_application_insights" "this" {
  name                = var.name
  resource_group_name = var.resource_group_name
  location            = var.location
  workspace_id        = var.workspace_id
  application_type    = "web"
  tags                = var.tags
}
```

### `modules/app_insights/outputs.tf`

```hcl
output "connection_string" {
  value     = azurerm_application_insights.this.connection_string
  sensitive = true
}

output "instrumentation_key" {
  value     = azurerm_application_insights.this.instrumentation_key
  sensitive = true
}
```

---

### `modules/static_web_app/variables.tf`

```hcl
variable "name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type        = string
  description = "Static Web Apps only supports a subset of regions — use eastus2, westus2, centralus, eastasia, westeurope, eastus, or uksouth"
}

variable "tags" {
  type    = map(string)
  default = {}
}
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
output "default_hostname" {
  value = azurerm_static_web_app.this.default_host_name
}

output "api_key" {
  value     = azurerm_static_web_app.this.api_key
  sensitive = true
  description = "Deployment token — add to GitHub Actions secret SWA_DEPLOY_TOKEN"
}
```

---

## Updated `main.tf` — Additions

Add the following blocks to `orderprocessing-infra-terraform/terraform/main.tf`. Replace the
`data "azurerm_container_app"` data sources and the fragile role assignments with proper
`module` calls.

```hcl
# ── Application Insights ────────────────────────────────────────────────────
module "app_insights" {
  source = "./modules/app_insights"

  name                = "${var.project_name}-ai"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  workspace_id        = module.log_analytics.workspace_id
  tags                = var.tags
}

# ── SQL Server + 4 databases ─────────────────────────────────────────────────
module "sql_server" {
  source = "./modules/sql_server"

  server_name         = "${var.project_name}-sql"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  database_names      = ["products-db", "carts-db", "users-db", "payments-db"]
  admin_login         = var.sql_admin_login
  admin_password      = var.sql_admin_password
  tags                = var.tags
}

# ── Redis ────────────────────────────────────────────────────────────────────
module "redis" {
  source = "./modules/redis"

  name                = "${var.project_name}-redis"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  tags                = var.tags
}

# ── Container Apps ───────────────────────────────────────────────────────────
module "ca_product" {
  source = "./modules/container_app"

  name                 = "${var.project_name}-product"
  resource_group_name  = azurerm_resource_group.rg.name
  location             = azurerm_resource_group.rg.location
  environment_id       = module.container_app_env.environment_id
  image                = var.product_image
  cpu                  = var.container_cpu
  memory               = var.container_memory
  container_port       = var.container_port
  acr_id               = module.acr.id
  acr_login_server     = module.acr.login_server
}

module "ca_cart" {
  source = "./modules/container_app"

  name                 = "${var.project_name}-cart"
  resource_group_name  = azurerm_resource_group.rg.name
  location             = azurerm_resource_group.rg.location
  environment_id       = module.container_app_env.environment_id
  image                = var.cart_image
  cpu                  = var.container_cpu
  memory               = var.container_memory
  container_port       = var.container_port
  acr_id               = module.acr.id
  acr_login_server     = module.acr.login_server
}

module "ca_user" {
  source = "./modules/container_app"

  name                 = "${var.project_name}-user"
  resource_group_name  = azurerm_resource_group.rg.name
  location             = azurerm_resource_group.rg.location
  environment_id       = module.container_app_env.environment_id
  image                = var.user_image
  cpu                  = var.container_cpu
  memory               = var.container_memory
  container_port       = var.container_port
  acr_id               = module.acr.id
  acr_login_server     = module.acr.login_server
}

module "ca_payment" {
  source = "./modules/container_app"

  name                 = "${var.project_name}-payment"
  resource_group_name  = azurerm_resource_group.rg.name
  location             = azurerm_resource_group.rg.location
  environment_id       = module.container_app_env.environment_id
  image                = var.payment_image
  cpu                  = var.container_cpu
  memory               = var.container_memory
  container_port       = var.container_port
  acr_id               = module.acr.id
  acr_login_server     = module.acr.login_server
}

module "ca_gateway" {
  source = "./modules/container_app"

  name                 = "${var.project_name}-gateway"
  resource_group_name  = azurerm_resource_group.rg.name
  location             = azurerm_resource_group.rg.location
  environment_id       = module.container_app_env.environment_id
  image                = var.gateway_image
  cpu                  = var.container_cpu
  memory               = var.container_memory
  container_port       = var.container_port
  acr_id               = module.acr.id
  acr_login_server     = module.acr.login_server
}

# ── Key Vault ────────────────────────────────────────────────────────────────
module "key_vault" {
  source = "./modules/key_vault"

  name                = "${var.project_name}-kv"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  tenant_id           = data.azurerm_client_config.current.tenant_id
  tags                = var.tags

  app_principal_ids = {
    cart    = module.ca_cart.system_assigned_identity_principal_id
    product = module.ca_product.system_assigned_identity_principal_id
    user    = module.ca_user.system_assigned_identity_principal_id
    payment = module.ca_payment.system_assigned_identity_principal_id
    gateway = module.ca_gateway.system_assigned_identity_principal_id
  }
}

data "azurerm_client_config" "current" {}

# ── Key Vault Secrets ────────────────────────────────────────────────────────
resource "azurerm_key_vault_secret" "appinsights_conn" {
  name         = "ApplicationInsights--ConnectionString"
  value        = module.app_insights.connection_string
  key_vault_id = module.key_vault.id
}

resource "azurerm_key_vault_secret" "redis_conn" {
  name         = "Redis--ConnectionString"
  value        = module.redis.connection_string
  key_vault_id = module.key_vault.id
}

# Connection strings for each service (Managed Identity auth — no password in string)
resource "azurerm_key_vault_secret" "conn_products" {
  name         = "ConnectionStrings--DefaultConnection--Products"
  value        = "Server=tcp:${module.sql_server.server_fqdn},1433;Initial Catalog=products-db;Authentication=Active Directory Managed Identity;Encrypt=True;"
  key_vault_id = module.key_vault.id
}

resource "azurerm_key_vault_secret" "conn_carts" {
  name         = "ConnectionStrings--DefaultConnection--Carts"
  value        = "Server=tcp:${module.sql_server.server_fqdn},1433;Initial Catalog=carts-db;Authentication=Active Directory Managed Identity;Encrypt=True;"
  key_vault_id = module.key_vault.id
}

resource "azurerm_key_vault_secret" "conn_users" {
  name         = "ConnectionStrings--DefaultConnection--Users"
  value        = "Server=tcp:${module.sql_server.server_fqdn},1433;Initial Catalog=users-db;Authentication=Active Directory Managed Identity;Encrypt=True;"
  key_vault_id = module.key_vault.id
}

resource "azurerm_key_vault_secret" "conn_payments" {
  name         = "ConnectionStrings--DefaultConnection--Payments"
  value        = "Server=tcp:${module.sql_server.server_fqdn},1433;Initial Catalog=payments-db;Authentication=Active Directory Managed Identity;Encrypt=True;"
  key_vault_id = module.key_vault.id
}

# ── AcrPull role assignments (managed here, not via data sources) ─────────────
resource "azurerm_role_assignment" "acr_pull" {
  for_each = {
    cart    = module.ca_cart.system_assigned_identity_principal_id
    product = module.ca_product.system_assigned_identity_principal_id
    user    = module.ca_user.system_assigned_identity_principal_id
    payment = module.ca_payment.system_assigned_identity_principal_id
    gateway = module.ca_gateway.system_assigned_identity_principal_id
  }

  scope                = module.acr.id
  role_definition_name = "AcrPull"
  principal_id         = each.value
}

# ── Static Web App ────────────────────────────────────────────────────────────
module "static_web_app" {
  source = "./modules/static_web_app"

  name                = "${var.project_name}-ui"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  tags                = var.tags
}
```

---

## Additional Variables to Add to `variables.tf`

```hcl
variable "gateway_image" {
  type        = string
  description = "ACR image tag for ApiGateway"
  default     = "az204labsacr.azurecr.io/apigateway:latest"
}

variable "sql_admin_login" {
  type        = string
  description = "SQL Server administrator username"
  default     = "sqladmin"
}

variable "sql_admin_password" {
  type        = string
  sensitive   = true
  description = "SQL Server administrator password — supply via TF_VAR_sql_admin_password env var, never commit"
}
```

---

## Application Code Changes Required

### 1. SQLite → SQL Server (each service)

In each service's `Program.cs`, change the EF Core provider:

```csharp
// Before
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// After
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

Add the SQL Server EF Core package to each service `.csproj`:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.*" />
```

You can remove `Microsoft.EntityFrameworkCore.Sqlite` after switching.

### 2. Key Vault Configuration (each service `Program.cs`)

Add the Azure Key Vault configuration provider at startup so secrets override `appsettings.json`:

```csharp
// Add NuGet: Azure.Extensions.AspNetCore.Configuration.Secrets
// Add NuGet: Azure.Identity

if (builder.Environment.IsProduction())
{
    var keyVaultUri = new Uri($"https://{builder.Configuration["KeyVault:Name"]}.vault.azure.net/");
    builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
}
```

Set the `KeyVault__Name` environment variable on each Container App to `az204labs-kv`.

### 3. EF Core Migrations

Run migrations against each Azure SQL database after the first `terraform apply`:

```bash
# From solution root — repeat for each service
dotnet ef database update \
  --project ProductService \
  --connection "Server=tcp:<server>.database.windows.net,1433;Initial Catalog=products-db;User ID=sqladmin;Password=<password>;Encrypt=True;"
```

---

## Deployment Steps

### First-time Setup

```bash
# 1. Authenticate
az login
az account set --subscription <subscription-id>

# 2. Build and push images
az acr login --name az204labsacr
docker build -t az204labsacr.azurecr.io/apigateway:latest    -f ApiGateway/Dockerfile .
docker build -t az204labsacr.azurecr.io/cartservice:latest   -f CartService/Dockerfile .
docker build -t az204labsacr.azurecr.io/productservice:latest -f ProductService/Dockerfile .
docker build -t az204labsacr.azurecr.io/userservice:latest   -f UserService/Dockerfile .
docker build -t az204labsacr.azurecr.io/paymentservice:latest -f PaymentService/Dockerfile .
docker push az204labsacr.azurecr.io/apigateway:latest
docker push az204labsacr.azurecr.io/cartservice:latest
docker push az204labsacr.azurecr.io/productservice:latest
docker push az204labsacr.azurecr.io/userservice:latest
docker push az204labsacr.azurecr.io/paymentservice:latest

# 3. Apply infrastructure
cd orderprocessing-infra-terraform/terraform
export TF_VAR_sql_admin_password="<strong-password>"
terraform init
terraform apply

# 4. Run EF migrations (after apply)
dotnet ef database update --project ProductService ...
dotnet ef database update --project CartService ...
dotnet ef database update --project UserService ...
dotnet ef database update --project PaymentService ...

# 5. Add Stripe secret to Key Vault manually
az keyvault secret set \
  --vault-name az204labs-kv \
  --name "Stripe--SecretKey" \
  --value "<stripe-secret-key>"

# 6. Deploy Angular SPA
cd OrderProcessingUI
pnpm build
# Upload dist/ to Static Web App using the API key output from terraform
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
| Azure SQL Database × 4 | Basic 5 DTU | ~$20 |
| Azure Container Registry | Basic | ~$5 |
| Azure Key Vault | Standard | ~$1 |
| Azure Cache for Redis | Basic C0 | ~$16 |
| Application Insights | Pay-per-GB | ~$0–5 |
| Static Web Apps | Free | $0 |
| Log Analytics Workspace | PerGB2018 | ~$2 |
| **Total** | | **~$49–59/month** |

Redis is optional for MVP — without it rate limiting in ApiGateway will be per-instance rather
than global, but the service will function correctly. Dropping Redis saves ~$16/month.
