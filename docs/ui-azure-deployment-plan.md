# Plan: Deploy Angular UI to Azure & Connect to API Gateway

## Overview

Host the Angular app on **Azure Static Web Apps (SWA)**. SWA is the natural fit here — it has a free tier, global CDN, auto SSL, native GitHub Actions integration, and a built-in API proxy that forwards `/api` requests to the gateway, eliminating CORS entirely.

---

## Current State

| Item | Value |
|---|---|
| Angular version | 21 (pnpm) |
| `environment.prod.ts` apiUrl | `http://localhost:5182` ← wrong, needs fixing |
| `environment.prod.ts` apiKey | `dev-key-123` ← should be a secret |
| API Gateway CORS | `http://localhost:4200` only ← needs updating |
| Auth interceptor | Sends `X-API-Key` header on every request |
| Package manager | pnpm |

---

## Step 1 — Fix `environment.prod.ts`

Update the production API URL to point at the deployed gateway and move the API key to a build-time environment variable so it is not hardcoded.

**File:** `OrderProcessingUI/src/environments/environment.prod.ts`

```ts
export const environment = {
  production: true,
  apiUrl: 'https://apigateway-app.happybeach-64cb3345.uksouth.azurecontainerapps.io',
  apiKey: '${API_KEY}',   // replaced at build time via sed/envsubst in CI
};
```

> Alternatively, set `apiUrl: ''` and use SWA's built-in API proxy (see Step 3). That way the Angular app never needs to know the gateway URL at all.

---

## Step 2 — Add `staticwebapp.config.json`

SWA needs this file for two things: SPA fallback routing (so Angular's client-side routes work on refresh) and the `/api` proxy to the gateway.

**File:** `OrderProcessingUI/staticwebapp.config.json`

```json
{
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/assets/*", "/*.{css,js,ico,png,svg}"]
  },
  "routes": [
    {
      "route": "/api/*",
      "rewrite": "https://apigateway-app.happybeach-64cb3345.uksouth.azurecontainerapps.io/api/*"
    }
  ],
  "globalHeaders": {
    "X-API-Key": "<your-api-key>"
  }
}
```

> With the proxy in place, set `apiUrl: ''` in `environment.prod.ts` — the app calls `/api/...` and SWA forwards it. No CORS headers needed on the gateway side.

---

## Step 3 — Update API Gateway CORS (if not using the SWA proxy)

If you choose to call the gateway directly (without the SWA proxy), update the CORS policy in `ApiGateway/Configuration/ServiceCollectionExtensions.cs` to allow the SWA domain:

```csharp
policy.WithOrigins(
    "http://localhost:4200",
    "https://<your-swa-name>.azurestaticapps.net"
)
```

> Skip this step if you use the SWA `/api` proxy — the browser never sees a cross-origin request.

---

## Step 4 — Provision Azure Static Web App via Terraform

Add a new resource to `orderprocessing-infra-terraform/terraform/main.tf`:

```hcl
resource "azurerm_static_web_app" "ui" {
  name                = "${var.prefix}-${var.environment}-ui"
  resource_group_name = module.resource_group.name
  location            = "westeurope"   # SWA has limited region availability
  sku_tier            = "Free"
  sku_size            = "Free"

  tags = {
    Environment = var.environment
    Project     = var.prefix
  }
}

output "ui_url" {
  value = "https://${azurerm_static_web_app.ui.default_host_name}"
}

output "ui_deploy_token" {
  value     = azurerm_static_web_app.ui.api_key
  sensitive = true
}
```

Run `terraform apply` to create the SWA and retrieve the deployment token:

```bash
terraform output -raw ui_deploy_token
```

Save the token as a GitHub Actions secret: `AZURE_STATIC_WEB_APPS_API_TOKEN`.

---

## Step 5 — Add GitHub Actions Workflow

**File:** `.github/workflows/ui-deploy.yml`

```yaml
name: Deploy UI to Azure Static Web Apps

on:
  push:
    branches:
      - main
    paths:
      - "OrderProcessingUI/**"
      - ".github/workflows/ui-deploy.yml"
  workflow_dispatch:

permissions:
  id-token: write
  contents: read

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup Node / pnpm
        uses: actions/setup-node@v4
        with:
          node-version: '22'

      - name: Install pnpm
        run: npm install -g pnpm

      - name: Install dependencies
        working-directory: OrderProcessingUI
        run: pnpm install --frozen-lockfile

      - name: Build
        working-directory: OrderProcessingUI
        run: pnpm run build
        env:
          NODE_ENV: production

      - name: Deploy to Azure Static Web Apps
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: upload
          app_location: OrderProcessingUI
          output_location: dist/OrderProcessingUI/browser
```

---

## Step 6 — GitHub Actions Secret

| Secret name | Value |
|---|---|
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Output of `terraform output -raw ui_deploy_token` |

---

## Step 7 — Verify

Once deployed, the SWA URL is printed in the GitHub Actions run log and available as a Terraform output (`ui_url`). Check:

```bash
# Checkout page via SWA proxy
curl https://<your-swa-name>.azurestaticapps.net/api/checkout-page/1

# Angular app loads in browser
open https://<your-swa-name>.azurestaticapps.net
```

---

## Summary of Changes

| File | Change |
|---|---|
| `OrderProcessingUI/src/environments/environment.prod.ts` | Set `apiUrl` to gateway URL (or `''` if using SWA proxy) |
| `OrderProcessingUI/staticwebapp.config.json` | New — SPA routing + `/api` proxy |
| `ApiGateway/Configuration/ServiceCollectionExtensions.cs` | Add SWA domain to CORS (only if not using proxy) |
| `orderprocessing-infra-terraform/terraform/main.tf` | Add `azurerm_static_web_app` resource |
| `orderprocessing-infra-terraform/terraform/outputs.tf` | Add `ui_url` and `ui_deploy_token` outputs |
| `.github/workflows/ui-deploy.yml` | New — build and deploy Angular to SWA |
