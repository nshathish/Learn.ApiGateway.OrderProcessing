# Learn.ApiGateway.OrderProcessing

Microservices sample built with **.NET 10** and an API Gateway pattern.

## Services

- **ApiGateway** (entry point): `http://localhost:8080`
- **ProductService**: `http://localhost:8084`
- **CartService**: `http://localhost:8082`
- **UserService**: `http://localhost:8085`
- **PaymentService**: `http://localhost:8083`

## Run with Docker Compose

From the solution root:

```powershell
docker compose up -d --build
```

## Quick checks

```powershell
Invoke-WebRequest -Uri "http://localhost:8080/health" -UseBasicParsing
Invoke-WebRequest -Uri "http://localhost:8080/api/products" -UseBasicParsing
Invoke-WebRequest -Uri "http://localhost:8080/api/checkout-page/1" -UseBasicParsing
```

## Stop containers

```powershell
docker compose down
```

## Documentation

- Infrastructure and Azure deployment guide: `docs/infra-planning.md`
