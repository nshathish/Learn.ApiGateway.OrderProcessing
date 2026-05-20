# KQL Queries — Container Apps Log Analytics

Run these queries in the **Log Analytics workspace** linked to your Container Apps environment.
Azure Portal: Container Apps Environment > Monitoring > Logs.

> **Table name depends on your log destination setting:**
> - `ContainerAppConsoleLogs` — resource-specific tables (newer, recommended)
> - `ContainerAppConsoleLogs_CL` — custom logs (older workspaces)
>
> To check which tables exist in your workspace, run:
> ```kql
> search * | distinct $table | sort by $table asc
> ```

## Errors for a specific service

```kql
ContainerAppConsoleLogs
| where TimeGenerated > ago(1h)
| where ContainerAppName == "cartservice-app"
| where Log has_any ("Exception", "Error", "fail", "error", "unhandled")
| project TimeGenerated, ContainerAppName, RevisionName, Log
| order by TimeGenerated desc
| take 50
```

## Errors across all services

```kql
ContainerAppConsoleLogs
| where TimeGenerated > ago(1h)
| where ContainerAppName in ("cartservice-app", "productservice-app", "userservice-app", "paymentservice-app", "apigateway-app")
| where Log has_any ("Exception", "Error", "fail", "unhandled", "500")
| project TimeGenerated, ContainerAppName, Log
| order by TimeGenerated desc
| take 100
```

## If using the older `_CL` tables

```kql
ContainerAppConsoleLogs_CL
| where TimeGenerated > ago(1h)
| where ContainerAppName_s in ("cartservice-app", "productservice-app", "userservice-app", "paymentservice-app", "apigateway-app")
| where Log_s has_any ("Exception", "Error", "fail", "unhandled", "500")
| project TimeGenerated, ContainerAppName_s, Log_s
| order by TimeGenerated desc
| take 100
```
