# Azure Deployment Guide — Learn.ApiGateway.OrderProcessing

## Local Container Runtime (Already Implemented)

Dockerfiles and Docker Compose files are now part of the repo and can run all backend services locally in containers.

### Current local container topology

- ApiGateway: `http://localhost:8080`
- CartService: `http://localhost:8082`
- PaymentService: `http://localhost:8083`
- ProductService: `http://localhost:8084`
- UserService: `http://localhost:8085`

### Run locally with Docker Compose

```powershell
docker compose up -d --build
```

### Verify key endpoints

```powershell
Invoke-WebRequest -Uri "http://localhost:8080/health" -UseBasicParsing
Invoke-WebRequest -Uri "http://localhost:8080/api/products" -UseBasicParsing
Invoke-WebRequest -Uri "http://localhost:8080/api/checkout-page/1" -UseBasicParsing
```

### Check logs / restart one service

```powershell
docker compose logs --tail 200 apigateway productservice cartservice userservice paymentservice
docker compose up -d --build apigateway
```

---

## Recommended Architecture: Azure Container Apps + Azure Static Web Apps

### Why Azure Container Apps (not App Service)

| Factor | Azure Container Apps | Azure App Service |
|---|---|---|
| Microservices fit | Native — internal DNS, built-in service discovery | 5 separate plans needed; no internal networking without VNet ($) |
| Cost at idle | Scale-to-zero on Consumption plan — $0 when unused | ~$13/month minimum per Basic B1 plan |
| .NET 10 support | Any container image — no runtime lag | Depends on Azure stack availability for preview releases |
| Internal networking | Backend services invisible to the internet by design | Requires VNet integration add-on |
| Learning value | Containers, ACR, Managed Identity — industry-standard skills | Simpler but less representative of real microservices ops |

**Estimated monthly cost (Consumption plan, active learning usage):** ~$45–51/month.  
Removing Redis (optional) saves $16/month if you skip caching for now.

---

## Azure Resource Inventory

| Resource | Name | SKU | Purpose |
|---|---|---|---|
| Resource Group | `rg-orderprocessing-dev` | — | Single group; one `az group delete` tears everything down |
| Log Analytics Workspace | `law-orderprocessing-dev` | PerGB2018 | Required backing store for Container Apps logs |
| Application Insights | `appi-orderprocessing-dev` | Workspace-based | Already wired up in ApiGateway via OpenTelemetry exporter |
| Azure Container Registry | `acrorderprocessingdev001` | Basic (~$5/mo) | Stores all 5 Docker images |
| Container Apps Environment | `cae-orderprocessing-dev` | Consumption | Shared networking + logging plane for all 5 apps |
| Azure Cache for Redis | `redis-orderprocessing-dev` | Basic C0 (~$16/mo) | ApiGateway distributed cache |
| Azure SQL Server (logical) | `sql-orderprocessing-dev` | — | Hosts all 4 databases |
| Azure SQL Database ×4 | `products-db`, `carts-db`, `users-db`, `payments-db` | Basic 5 DTU (~$5/mo each) | Replaces local SQLite files |
| Azure Key Vault | `kv-orderprocessing-dev` | Standard | All secrets; never in appsettings or env vars |
| Container Apps ×5 | `ca-apigateway`, `ca-productservice`, `ca-cartservice`, `ca-userservice`, `ca-paymentservice` | Consumption | One per .NET service |
| Azure Static Web Apps | `swa-orderprocessing-dev` | Free | Angular 21 SPA hosting with CDN |

---

## Required Code Changes Before Deploying

Make these changes first — the Dockerfiles and deployment steps assume them.

### 1. EF Core: SQLite → SQL Server (4 files)

In each of `ProductService/Program.cs`, `CartService/Program.cs`, `UserService/Program.cs`, `PaymentService/Program.cs`:

```csharp
// Before
options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))

// After
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
```

In each matching `.csproj`, remove the Sqlite package and add SqlServer:

```xml
<!-- Remove -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.7" />

<!-- Add -->
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.7" />
```

> `EnsureCreated()` works identically on SQL Server — no migration files needed. The `DbInitializer` pattern requires no changes.

### 2. Azure Key Vault Configuration Provider (5 files)

Add to every service's `Program.cs` **before** `builder.Build()`:

```csharp
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;

var keyVaultUri = builder.Configuration["KeyVaultUri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential(),
        new AzureKeyVaultConfigurationOptions
        {
            // Strips "ProductService-" prefix so secrets named
            // "ProductService-ConnectionStrings--DefaultConnection"
            // become "ConnectionStrings:DefaultConnection" in IConfiguration
            Manager = new PrefixKeyVaultSecretManager("<ServiceName>")
        });
}
```

Replace `<ServiceName>` with `ApiGateway`, `ProductService`, `CartService`, `UserService`, or `PaymentService` for each respective file.

Add NuGet packages to each `.csproj`:

```xml
<PackageReference Include="Azure.Identity" Version="1.13.2" />
<PackageReference Include="Azure.Extensions.AspNetCore.Configuration.Secrets" Version="1.3.2" />
```

With this in place, each Container App only needs one environment variable (`KeyVaultUri`) — all other config is pulled from Key Vault at startup. `DefaultAzureCredential` resolves to Managed Identity in Azure and falls back to your `az login` credentials locally.

### 3. ApiGateway: Read API Key from Configuration

`ApiGateway/Security/ApiKeyAuthenticationHandler.cs`, line 15 — replace the hardcoded constant:

```csharp
// Remove this line
private const string ValidApiKey = "dev-key-123";

// Add field and update constructor
private readonly string _validApiKey;

public ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeySchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : base(options, logger, encoder)
{
    _validApiKey = configuration["ApiKey:ValidKey"] ?? "dev-key-123";
}

// In HandleAuthenticateAsync, replace:
if (apiKey != ValidApiKey)
// With:
if (apiKey != _validApiKey)
```

### 4. ApiGateway: CORS — Add SWA Production Origin

`ApiGateway/Configuration/ServiceCollectionExtensions.cs`, line 60:

```csharp
// Before
policy.WithOrigins("http://localhost:4200")

// After
policy.WithOrigins(
    "http://localhost:4200",
    configuration["AllowedOrigins:SwaUrl"] ?? "")
```

Add `AllowedOrigins--SwaUrl` to Key Vault after the SWA is deployed (see Phase 7).

### 5. ApiGateway: Disable HTTPS Redirection Inside Containers

`ApiGateway/Program.cs`, line 26 — TLS is terminated at the Container Apps ingress; the container receives plain HTTP on port 8080:

```csharp
// Before (always redirects)
app.UseHttpsRedirection();

// After
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
```

Apply the same change to all 4 backend `Program.cs` files.

### 6. Angular Production Environment

`OrderProcessingUI/src/environments/environment.prod.ts` — update after the gateway URL is known (end of Phase 6):

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://ca-apigateway.<suffix>.eastus2.azurecontainerapps.io',
  apiKey: '<value-of-ApiKey--ValidKey-secret-from-KeyVault>',
};
```

> The `apiKey` is visible to anyone with DevTools. This is acceptable for a learning project. For production, replace API key auth with OAuth2/PKCE against Azure Entra ID (already partially configured in the gateway).

### 7. Angular: SPA Fallback Routing for Static Web Apps

Create `OrderProcessingUI/public/staticwebapp.config.json`:

```json
{
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/assets/*", "/*.{js,css,ico,png,svg,woff2}"]
  },
  "globalHeaders": {
    "Cache-Control": "public, max-age=3600"
  }
}
```

Files in `public/` are copied to the build output as-is by the Angular Vite builder, so this file will be served from the root of the SWA.

---

## New Files to Create

### `.dockerignore` (solution root)

> Already created in this repository. Keep it updated when adding new build artifacts.

```
**/bin/
**/obj/
OrderProcessingUI/node_modules/
OrderProcessingUI/dist/
OrderProcessingUI/.angular/
**/*.user
**/*.DotSettings.user
```

### Dockerfile pattern (one per service)

> Dockerfiles are already created for all backend services (`ApiGateway`, `ProductService`, `CartService`, `UserService`, `PaymentService`). The pattern below is retained as reference.

All 5 Dockerfiles follow this multi-stage pattern. Place each at the root of its service folder. **Build context is always the solution root** (one level up) so the `.csproj` restore can be cached independently from the source copy.

**`ApiGateway/Dockerfile`**

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ApiGateway/ApiGateway.csproj ApiGateway/
RUN dotnet restore ApiGateway/ApiGateway.csproj
COPY ApiGateway/ ApiGateway/
RUN dotnet publish ApiGateway/ApiGateway.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
```

**`ProductService/Dockerfile`**

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ProductService/ProductService.csproj ProductService/
RUN dotnet restore ProductService/ProductService.csproj
COPY ProductService/ ProductService/
RUN dotnet publish ProductService/ProductService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProductService.dll"]
```

Use the same pattern for `CartService`, `UserService`, and `PaymentService` — only the folder name and DLL name change.

---

## Step-by-Step Deployment

### Prerequisites

```powershell
# Verify tools
az version          # needs 2.60+
docker version      # Docker Desktop must be running
az login
az account set --subscription "<your-subscription-id>"
az extension add --name containerapp --upgrade
```

---

### Phase 1: Foundation Resources

```bash
# 1. Resource group
az group create --name rg-orderprocessing-dev --location eastus2

# 2. Log Analytics
az monitor.log-analytics.workspace create \
  --resource-group rg-orderprocessing-dev \
  --workspace-name law-orderprocessing-dev

# 3. Application Insights (save the connectionString from output)
az monitor app-insights component create \
  --app appi-orderprocessing-dev \
  --location eastus2 \
  --resource-group rg-orderprocessing-dev \
  --workspace law-orderprocessing-dev \
  --kind web

# 4. Container Registry
az acr create \
  --resource-group rg-orderprocessing-dev \
  --name acrorderprocessingdev001 \
  --sku Basic

# 5. Redis (takes 10–15 min)
az redis create \
  --name redis-orderprocessing-dev \
  --resource-group rg-orderprocessing-dev \
  --location eastus2 \
  --sku Basic \
  --vm-size c0

# 6. SQL Server + 4 databases
az sql server create \
  --name sql-orderprocessing-dev \
  --resource-group rg-orderprocessing-dev \
  --location eastus2 \
  --admin-user sqladmin \
  --admin-password "<strong-password>"

az sql server firewall-rule create \
  --resource-group rg-orderprocessing-dev \
  --server sql-orderprocessing-dev \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

az sql db create --resource-group rg-orderprocessing-dev --server sql-orderprocessing-dev --name products-db  --edition Basic --capacity 5
az sql db create --resource-group rg-orderprocessing-dev --server sql-orderprocessing-dev --name carts-db     --edition Basic --capacity 5
az sql db create --resource-group rg-orderprocessing-dev --server sql-orderprocessing-dev --name users-db     --edition Basic --capacity 5
az sql db create --resource-group rg-orderprocessing-dev --server sql-orderprocessing-dev --name payments-db  --edition Basic --capacity 5

# 7. Key Vault
az keyvault create \
  --name kv-orderprocessing-dev \
  --resource-group rg-orderprocessing-dev \
  --location eastus2 \
  --enable-rbac-authorization true

# Grant yourself Secrets Officer so you can write secrets
az role assignment create \
  --role "Key Vault Secrets Officer" \
  --assignee "$(az ad signed-in-user show --query id -o tsv)" \
  --scope "$(az keyvault show --name kv-orderprocessing-dev --resource-group rg-orderprocessing-dev --query id -o tsv)"
```

---

### Phase 2: Populate Key Vault Secrets

```bash
KV=kv-orderprocessing-dev

# Redis connection string
REDIS_KEY=$(az redis list-keys --name redis-orderprocessing-dev --resource-group rg-orderprocessing-dev --query primaryKey -o tsv)
REDIS_HOST=$(az redis show --name redis-orderprocessing-dev --resource-group rg-orderprocessing-dev --query hostName -o tsv)
az keyvault secret set --vault-name $KV --name "ApiGateway-Redis--ConnectionString"         --value "${REDIS_HOST}:6380,password=${REDIS_KEY},ssl=True,abortConnect=False"

# Application Insights (use value saved from Phase 1 step 3)
az keyvault secret set --vault-name $KV --name "ApiGateway-ApplicationInsights--ConnectionString" --value "<connection-string-from-phase-1>"

# Azure AD (Entra ID) — from your app registration
az keyvault secret set --vault-name $KV --name "ApiGateway-Auth--TenantId"                 --value "<your-tenant-id>"
az keyvault secret set --vault-name $KV --name "ApiGateway-Auth--ClientId"                 --value "<your-client-id>"

# API Key — generate a random 32-char string
az keyvault secret set --vault-name $KV --name "ApiGateway-ApiKey--ValidKey"               --value "<random-32-char-string>"

# JWT (UserService)
az keyvault secret set --vault-name $KV --name "UserService-Jwt--Key"                      --value "<random-64-char-string>"
az keyvault secret set --vault-name $KV --name "UserService-Jwt--Issuer"                   --value "UserService"
az keyvault secret set --vault-name $KV --name "UserService-Jwt--Audience"                 --value "OrderProcessing"

# Stripe (PaymentService) — from Stripe dashboard
az keyvault secret set --vault-name $KV --name "PaymentService-Stripe--SecretKey"          --value "<stripe-secret-key>"
az keyvault secret set --vault-name $KV --name "PaymentService-Stripe--PublicKey"          --value "<stripe-public-key>"
az keyvault secret set --vault-name $KV --name "PaymentService-Stripe--WebhookSecret"      --value "<stripe-webhook-secret>"

# PayPal (PaymentService) — from PayPal developer portal
az keyvault secret set --vault-name $KV --name "PaymentService-PayPal--ClientId"           --value "<paypal-client-id>"
az keyvault secret set --vault-name $KV --name "PaymentService-PayPal--ClientSecret"       --value "<paypal-client-secret>"

# SQL connection strings (Managed Identity — no username/password in the string)
SQL="sql-orderprocessing-dev.database.windows.net"
az keyvault secret set --vault-name $KV --name "ProductService-ConnectionStrings--DefaultConnection" --value "Server=tcp:${SQL},1433;Initial Catalog=products-db;Authentication=Active Directory Managed Identity;Encrypt=True;"
az keyvault secret set --vault-name $KV --name "CartService-ConnectionStrings--DefaultConnection"    --value "Server=tcp:${SQL},1433;Initial Catalog=carts-db;Authentication=Active Directory Managed Identity;Encrypt=True;"
az keyvault secret set --vault-name $KV --name "UserService-ConnectionStrings--DefaultConnection"    --value "Server=tcp:${SQL},1433;Initial Catalog=users-db;Authentication=Active Directory Managed Identity;Encrypt=True;"
az keyvault secret set --vault-name $KV --name "PaymentService-ConnectionStrings--DefaultConnection" --value "Server=tcp:${SQL},1433;Initial Catalog=payments-db;Authentication=Active Directory Managed Identity;Encrypt=True;"
```

**Secret naming convention:** The prefix before `-` (e.g., `ProductService-`) is stripped by `PrefixKeyVaultSecretManager`. The `--` separator maps to `:` in .NET IConfiguration. So `ProductService-ConnectionStrings--DefaultConnection` becomes `ConnectionStrings:DefaultConnection` inside the ProductService container.

---

### Phase 3: Container Apps Environment

```bash
WORKSPACE_ID=$(az monitor log-analytics workspace show \
  --resource-group rg-orderprocessing-dev \
  --workspace-name law-orderprocessing-dev \
  --query customerId -o tsv)

WORKSPACE_KEY=$(az monitor log-analytics workspace get-shared-keys \
  --resource-group rg-orderprocessing-dev \
  --workspace-name law-orderprocessing-dev \
  --query primarySharedKey -o tsv)

az containerapp env create \
  --name cae-orderprocessing-dev \
  --resource-group rg-orderprocessing-dev \
  --location eastus2 \
  --logs-workspace-id $WORKSPACE_ID \
  --logs-workspace-key $WORKSPACE_KEY
```

---

### Phase 4: Build and Push Docker Images

Run all commands from the **solution root** (`Learn.ApiGateway.OrderProcessing/`).

```bash
ACR=acrorderprocessingdev001.azurecr.io

az acr login --name acrorderprocessingdev001

docker build -f ApiGateway/Dockerfile     -t ${ACR}/apigateway:v1.0.0     .
docker build -f ProductService/Dockerfile -t ${ACR}/productservice:v1.0.0 .
docker build -f CartService/Dockerfile    -t ${ACR}/cartservice:v1.0.0    .
docker build -f UserService/Dockerfile    -t ${ACR}/userservice:v1.0.0    .
docker build -f PaymentService/Dockerfile -t ${ACR}/paymentservice:v1.0.0 .

docker push ${ACR}/apigateway:v1.0.0
docker push ${ACR}/productservice:v1.0.0
docker push ${ACR}/cartservice:v1.0.0
docker push ${ACR}/userservice:v1.0.0
docker push ${ACR}/paymentservice:v1.0.0
```

---

### Phase 5: Deploy Backend Services (Internal Ingress)

Deploy the 4 backend services first so their internal FQDNs are known before configuring YARP.

```bash
ACR=acrorderprocessingdev001.azurecr.io
RG=rg-orderprocessing-dev
ENV=cae-orderprocessing-dev
KV_URI=https://kv-orderprocessing-dev.vault.azure.net/

for SERVICE in productservice cartservice userservice paymentservice; do
  az containerapp create \
    --name ca-${SERVICE} \
    --resource-group $RG \
    --environment $ENV \
    --image ${ACR}/${SERVICE}:v1.0.0 \
    --registry-server ${ACR} \
    --target-port 8080 \
    --ingress internal \
    --min-replicas 0 \
    --max-replicas 2 \
    --system-assigned \
    --env-vars "KeyVaultUri=${KV_URI}" "ASPNETCORE_ENVIRONMENT=Production"
done
```

**Grant each service access to Key Vault:**

```bash
for SERVICE in productservice cartservice userservice paymentservice; do
  PRINCIPAL=$(az containerapp show \
    --name ca-${SERVICE} \
    --resource-group $RG \
    --query identity.principalId -o tsv)

  az role assignment create \
    --role "Key Vault Secrets User" \
    --assignee $PRINCIPAL \
    --scope "$(az keyvault show --name kv-orderprocessing-dev --resource-group $RG --query id -o tsv)"
done
```

**Grant Managed Identity access to Azure SQL** (run in SSMS or Azure Data Studio, connected as `sqladmin`):

```sql
-- Run in products-db
CREATE USER [ca-productservice] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [ca-productservice];
ALTER ROLE db_datawriter ADD MEMBER [ca-productservice];
ALTER ROLE db_ddladmin   ADD MEMBER [ca-productservice];  -- needed for EnsureCreated()
```

Repeat for `carts-db` / `ca-cartservice`, `users-db` / `ca-userservice`, `payments-db` / `ca-paymentservice`.

**Set CartService's ProductService URL** (inter-service call via internal DNS):

```bash
PRODUCT_FQDN=$(az containerapp show --name ca-productservice --resource-group $RG \
  --query properties.configuration.ingress.fqdn -o tsv)

az containerapp update \
  --name ca-cartservice \
  --resource-group $RG \
  --set-env-vars "ServiceUrls__ProductService=http://${PRODUCT_FQDN}"
```

---

### Phase 6: Deploy ApiGateway (External Ingress)

Collect all internal FQDNs first:

```bash
PRODUCT_FQDN=$(az containerapp show --name ca-productservice --resource-group $RG --query properties.configuration.ingress.fqdn -o tsv)
CART_FQDN=$(az containerapp show    --name ca-cartservice    --resource-group $RG --query properties.configuration.ingress.fqdn -o tsv)
USER_FQDN=$(az containerapp show    --name ca-userservice    --resource-group $RG --query properties.configuration.ingress.fqdn -o tsv)
PAYMENT_FQDN=$(az containerapp show --name ca-paymentservice --resource-group $RG --query properties.configuration.ingress.fqdn -o tsv)
```

Deploy the gateway with external ingress and YARP cluster addresses overridden via environment variables:

```bash
az containerapp create \
  --name ca-apigateway \
  --resource-group $RG \
  --environment $ENV \
  --image ${ACR}/apigateway:v1.0.0 \
  --registry-server ${ACR} \
  --target-port 8080 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 3 \
  --system-assigned \
  --env-vars \
    "KeyVaultUri=${KV_URI}" \
    "ASPNETCORE_ENVIRONMENT=Production" \
    "ReverseProxy__Clusters__product-cluster__Destinations__product1__Address=http://${PRODUCT_FQDN}/" \
    "ReverseProxy__Clusters__cart-cluster__Destinations__cart1__Address=http://${CART_FQDN}/" \
    "ReverseProxy__Clusters__user-cluster__Destinations__user1__Address=http://${USER_FQDN}/" \
    "ReverseProxy__Clusters__payment-cluster__Destinations__payment1__Address=http://${PAYMENT_FQDN}/" \
    "ServiceUrls__ProductService=http://${PRODUCT_FQDN}" \
    "ServiceUrls__CartService=http://${CART_FQDN}" \
    "ServiceUrls__UserService=http://${USER_FQDN}" \
    "ServiceUrls__PaymentService=http://${PAYMENT_FQDN}"
```

> `__` (double underscore) in env var names maps to `:` and JSON hierarchy in .NET configuration. YARP reads its config from `IConfiguration`, so no code changes are needed — env vars override `appsettings.json` automatically.

Grant Key Vault access to the gateway:

```bash
GW_PRINCIPAL=$(az containerapp show --name ca-apigateway --resource-group $RG --query identity.principalId -o tsv)
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee $GW_PRINCIPAL \
  --scope "$(az keyvault show --name kv-orderprocessing-dev --resource-group $RG --query id -o tsv)"
```

Get the public FQDN — you will need this for the Angular environment file and Key Vault CORS secret:

```bash
GW_FQDN=$(az containerapp show --name ca-apigateway --resource-group $RG \
  --query properties.configuration.ingress.fqdn -o tsv)
echo "Gateway URL: https://${GW_FQDN}"
```

---

### Phase 7: Deploy Angular Frontend (Static Web Apps)

**Update Angular environment file** with the gateway URL (from Phase 6) and the API key value (from Key Vault), then commit and push.

```bash
az staticwebapp create \
  --name swa-orderprocessing-dev \
  --resource-group $RG \
  --location eastus2 \
  --source "https://github.com/<your-username>/Learn.ApiGateway.OrderProcessing" \
  --branch main \
  --app-location "OrderProcessingUI" \
  --output-location "dist/order-processing-ui/browser" \
  --login-with-github
```

This generates a GitHub Actions workflow file at `.github/workflows/azure-static-web-apps-*.yml`. The workflow installs pnpm and runs `ng build --configuration production` on every push to `main`.

Get the SWA URL:

```bash
SWA_URL=$(az staticwebapp show --name swa-orderprocessing-dev --resource-group $RG --query defaultHostname -o tsv)
echo "SWA URL: https://${SWA_URL}"
```

Add the SWA URL to Key Vault so CORS picks it up on the next gateway redeploy:

```bash
az keyvault secret set --vault-name kv-orderprocessing-dev \
  --name "ApiGateway-AllowedOrigins--SwaUrl" \
  --value "https://${SWA_URL}"
```

Then restart the gateway to reload config:

```bash
az containerapp revision restart \
  --name ca-apigateway \
  --resource-group $RG \
  --revision $(az containerapp revision list --name ca-apigateway --resource-group $RG --query "[0].name" -o tsv)
```

---

## Networking Architecture

```
Internet
    │  HTTPS (managed cert)
    ▼
ca-apigateway  [External Ingress — only public entry point]
    │
    │  Internal HTTP port 8080 (private DNS within environment)
    ├──▶ ca-productservice  [Internal only]
    ├──▶ ca-cartservice     [Internal only]
    ├──▶ ca-userservice     [Internal only]
    └──▶ ca-paymentservice  [Internal only]

Azure Static Web Apps (global CDN)
    └──▶ Browser calls ca-apigateway public FQDN for all /api/* requests

All services ──▶ Azure Key Vault    (Managed Identity, HTTPS)
All services ──▶ Azure SQL Database (Managed Identity, port 1433, TLS enforced)
ApiGateway   ──▶ Azure Cache for Redis (TLS port 6380)
```

**Internal DNS format:**  
`ca-<name>.internal.<env-unique-suffix>.eastus2.azurecontainerapps.io`  
Resolvable only from within the same Container Apps Environment.

**TLS strategy:**
- External traffic (browser → gateway): TLS terminated at Container Apps ingress; free managed certificate issued automatically.
- Internal traffic (gateway → backend): HTTP only on port 8080. Microsoft manages network-level encryption for intra-environment traffic.
- PaaS services (SQL, Redis, Key Vault): All enforce TLS by default.

---

## Verification Checklist

### Phase 1 — Infrastructure
- [ ] `az containerapp env show --name cae-orderprocessing-dev --resource-group rg-orderprocessing-dev --query properties.provisioningState` → `"Succeeded"`
- [ ] `az keyvault secret list --vault-name kv-orderprocessing-dev -o table` shows all secrets
- [ ] `az sql db show --resource-group rg-orderprocessing-dev --server sql-orderprocessing-dev --name products-db --query status` → `"Online"`

### Phase 2 — Container Images
- [ ] `az acr repository list --name acrorderprocessingdev001` shows all 5 repositories
- [ ] Local smoke test before push: `docker run --rm -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Production <image> & curl http://localhost:8080/health`

### Phase 3 — Backend Services
Exec into the gateway container to test internal endpoints:

```bash
az containerapp exec --name ca-apigateway --resource-group rg-orderprocessing-dev --command "/bin/sh"
# Inside the container:
curl http://ca-productservice.internal.<suffix>/api/products    # → JSON array with seed products
curl http://ca-userservice.internal.<suffix>/api/users          # → seed users
curl http://ca-cartservice.internal.<suffix>/api/cart/1         # → empty cart
curl http://ca-paymentservice.internal.<suffix>/api/payments/methods  # → payment methods
```

Check for EF Core startup errors:
```bash
az containerapp logs show --name ca-productservice --resource-group rg-orderprocessing-dev --follow
```

### Phase 4 — ApiGateway
```bash
GW=https://ca-apigateway.<suffix>.eastus2.azurecontainerapps.io
API_KEY=<value-from-KeyVault>

curl ${GW}/health                                     # → {"status":"healthy"}
curl -H "X-API-Key: ${API_KEY}" ${GW}/api/products   # → products JSON (tests YARP routing)
curl -H "X-API-Key: ${API_KEY}" \
     -H "Content-Type: application/json" \
     -X POST -d '{"email":"john@example.com","password":"password123"}' \
     ${GW}/api/users/login                            # → JWT token
curl -H "X-API-Key: ${API_KEY}" ${GW}/api/checkout-page/1  # → aggregated checkout data
```

Check Application Insights Live Metrics in the Azure Portal to confirm traces are flowing.

### Phase 5 — Angular SPA
- [ ] GitHub Actions workflow completes (green check in Actions tab)
- [ ] `https://<swa-url>` loads the Angular app in a browser
- [ ] DevTools → Network: API calls go to the gateway FQDN (not localhost)
- [ ] No `Access-Control-Allow-Origin` errors in browser console
- [ ] Login with `john@example.com` / `password123` stores a JWT in localStorage
- [ ] Cart page loads products; checkout page renders

### Phase 6 — Security
- [ ] `az containerapp show --name ca-apigateway --resource-group rg-orderprocessing-dev --query properties.configuration.secrets` → empty or only `KeyVaultUri` visible; no raw secret values
- [ ] Direct HTTP request to `https://ca-productservice.internal.<suffix>/api/products` from outside → no response (internal ingress blocks external access)
- [ ] 6 rapid requests to `${GW}/api/checkout-page/1` within 10 seconds → 6th returns HTTP 429

### Phase 7 — Cost
- [ ] Set a budget alert: Azure Portal → Cost Management → Budgets → $30/month, alert at 80%
- [ ] After 24 hours: verify actual spend in Cost Management is tracking below estimate

---

## Monthly Cost Estimate

| Resource | SKU | Est. Cost/Month |
|---|---|---|
| Azure Container Registry | Basic | ~$5 |
| Container Apps ×5 (Consumption, learning usage) | Scale-to-zero | ~$2–8 |
| Azure SQL Database ×4 | Basic 5 DTU | ~$20 |
| Azure Cache for Redis | Basic C0 | ~$16 |
| Log Analytics Workspace | PerGB2018 | ~$2 |
| Application Insights | Workspace-based (5 GB free) | ~$0 |
| Azure Key Vault | Standard | ~$0 |
| Azure Static Web Apps | Free | $0 |
| **Total** | | **~$45–51/month** |

**To reduce cost:** Remove Redis from ApiGateway for now (comment out `StackExchangeRedis` registration in `ServiceCollectionExtensions.cs`). This saves $16/month and does not affect core functionality — caching is an enhancement.

---

## Updating After Code Changes

For any subsequent code change, rebuild only the affected image and restart its Container App revision:

```bash
# Example: update ApiGateway after a code change
docker build -f ApiGateway/Dockerfile -t ${ACR}/apigateway:v1.0.1 .
docker push ${ACR}/apigateway:v1.0.1

az containerapp update \
  --name ca-apigateway \
  --resource-group rg-orderprocessing-dev \
  --image ${ACR}/apigateway:v1.0.1
```

The Angular SPA redeploys automatically on every push to `main` via the GitHub Actions workflow.
