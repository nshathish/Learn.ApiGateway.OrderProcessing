Write-Host "Starting all 5 services..." -ForegroundColor Cyan

$services = @(
    @{ Name = "ProductService"; Port = 5001; Path = "ProductService" },
    @{ Name = "CartService"; Port = 5002; Path = "CartService" },
    @{ Name = "UserService"; Port = 5003; Path = "UserService" },
    @{ Name = "PaymentService"; Port = 5004; Path = "PaymentService" },
    @{ Name = "ApiGateway"; Port = 7000; Path = "ApiGateway" }
)

$processes = @()

foreach ($service in $services) {
    $workingDirectory = Join-Path $PSScriptRoot $service.Path

    if (-not (Test-Path $workingDirectory)) {
        Write-Host "Skipping $($service.Name): folder not found -> $workingDirectory" -ForegroundColor Red
        continue
    }

    Write-Host "Starting $($service.Name) on port $($service.Port)..." -ForegroundColor Yellow

    $p = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @("run", "--urls", "http://localhost:$($service.Port)") `
        -WorkingDirectory $workingDirectory `
        -PassThru

    $processes += $p
    Start-Sleep -Seconds 1
}

Write-Host "`n✅ All services running!" -ForegroundColor Green
Write-Host "Gateway: http://localhost:7000/scalar" -ForegroundColor Cyan
Write-Host "Test: curl https://localhost:7000/api/checkout-page/1" -ForegroundColor Cyan

Wait-Event