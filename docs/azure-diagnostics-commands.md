# Azure Container Apps — Diagnostics Commands

## List all container apps

```bash
az containerapp list --query "[].name" -o tsv
```

## Get a specific app's FQDN and running status

```bash
az containerapp show \
  --name <app-name> \
  --resource-group <resource-group> \
  --query "{fqdn: properties.configuration.ingress.fqdn, status: properties.runningStatus}" \
  -o json
```

## Get the resource group for a specific app

```bash
az containerapp list --query "[?name=='<app-name>'].resourceGroup" -o tsv
```

## Stream recent logs from a container app

```bash
az containerapp logs show \
  --name <app-name> \
  --resource-group <resource-group> \
  --tail 50
```

## Filter logs for errors and exceptions

```bash
az containerapp logs show \
  --name <app-name> \
  --resource-group <resource-group> \
  --tail 100 \
  | grep -E "(Exception|Error|fail|System\.|Unable|Cannot|refused|timeout)" \
  | head -40
```

## Hit a service endpoint directly to confirm it is responding

```bash
curl -v https://<app-fqdn>/api/<endpoint>
```

A **500 with a long delay** (e.g. 20+ seconds before response) usually indicates a dependency timeout — database connection string missing, downstream service unreachable, etc.

A **500 with `'<' is an invalid start of a value`** in the gateway logs means a downstream service returned an HTML error page instead of JSON.
