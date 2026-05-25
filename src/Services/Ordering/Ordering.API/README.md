# Ordering.API

The HTTP entry point for the Ordering microservice. Exposes a minimal-API surface using Carter modules, wires together the application and infrastructure layers, and provides health checking and global exception handling.

## Target Framework & Dependencies

- .NET 10.0
- `Carter` 10.0.0 — minimal API module routing
- `Mapster` — object mapping between request/response records and CQRS commands/queries
- `AspNetCore.HealthChecks.SqlServer` 9.0.0 — SQL Server liveness probe
- `AspNetCore.HealthChecks.UI.Client` 9.0.0 — JSON health response formatter
- `Microsoft.EntityFrameworkCore.Design` 10.0.8 (build-time only — EF CLI tooling)
- `Ordering.Application` (project reference)
- `Ordering.Infrastructure` (project reference)

## Project Structure

```
Ordering.API/
├── Program.cs                  # App bootstrap
├── DepedencyInjection.cs       # AddApiServices / UseApiServices extensions
├── appsettings.json            # Default configuration
├── appsettings.Development.json
├── Dockerfile
├── Properties/
│   └── launchSettings.json
└── Endpoints/                  # Carter ICarterModule implementations
    ├── CreateOrder.cs
    ├── UpdateOrder.cs
    ├── DeleteOrder.cs
    ├── GetOrders.cs
    ├── GetOrdersByCustomer.cs
    └── GetOrdersByName.cs
```

## Bootstrap

`Program.cs` composes all three service layers and starts the application:

```csharp
builder.Services
    .AddApplicationServices()       // MediatR + pipeline behaviors
    .AddInfrastructureServices(...)  // EF Core + SQL Server + interceptors
    .AddApiServices(...);            // Carter + health checks + exception handler

app.UseApiServices();               // MapCarter + UseExceptionHandler + health endpoint

if (app.Environment.IsDevelopment())
    await app.InitializeDatabaseAsync(); // auto-migrate + seed
```

## Registration

### `AddApiServices`
| Registration | Detail |
|---|---|
| Carter | Scans the assembly for `ICarterModule` implementations |
| `CustomExceptionHandler` | Global `IExceptionHandler` from `BuildingBlocks`; maps domain/application exceptions to RFC 7807 problem responses |
| SQL Server health check | Uses `ConnectionStrings:Database` |

### `UseApiServices`
| Middleware | Detail |
|---|---|
| `MapCarter()` | Registers all Carter module routes |
| `UseExceptionHandler` | Activates the global exception handler |
| `/health` endpoint | Returns JSON health status via `UIResponseWriter` |

## Endpoints

All endpoints are implemented as Carter `ICarterModule` classes. Request/response mapping to CQRS types uses Mapster `Adapt<T>()`.

### `POST /orders` — Create Order
| | |
|---|---|
| **Request body** | `CreateOrderRequest { OrderDto Order }` |
| **Success** | `201 Created` · `CreateOrderResponse { Guid Id }` · `Location: /orders/{id}` |
| **Error** | `400 Bad Request` (validation failure) |

### `PUT /orders` — Update Order
| | |
|---|---|
| **Request body** | `UpdateOrderRequest { OrderDto Order }` |
| **Success** | `200 OK` · `UpdateOrderResponse { bool IsSuccess }` |
| **Error** | `400 Bad Request` |

### `DELETE /orders/{id}` — Delete Order
| | |
|---|---|
| **Route param** | `id` — order GUID |
| **Success** | `200 OK` · `DeleteOrderResponse { bool IsSuccess }` |
| **Errors** | `400 Bad Request` · `404 Not Found` |

### `GET /orders` — Get Orders (paginated)
| | |
|---|---|
| **Query params** | `pageIndex` (int), `pageSize` (int) via `[AsParameters] PaginationRequest` |
| **Success** | `200 OK` · `GetOrdersResponse { PaginatedResult<OrderDto> Orders }` |
| **Error** | `400 Bad Request` |

### `GET /orders/customer/{customerId}` — Get Orders by Customer
| | |
|---|---|
| **Route param** | `customerId` — customer GUID |
| **Success** | `200 OK` · `GetOrdersByCustomerResponse { IEnumerable<OrderDto> Orders }` |
| **Errors** | `400 Bad Request` · `404 Not Found` |

### `GET /orders/{orderName}` — Get Orders by Name
| | |
|---|---|
| **Route param** | `orderName` — partial or full order name (contains-match) |
| **Success** | `200 OK` · `GetOrdersByNameResponse { IEnumerable<OrderDto> Orders }` |
| **Errors** | `400 Bad Request` · `404 Not Found` |

> **Route collision note:** `GET /orders/{orderName}` and `GET /orders/customer/{customerId}` share the `/orders/` prefix. Carter resolves them by matching the literal segment `customer` before the catch-all `{orderName}` parameter.

## Configuration

### `appsettings.json`
```json
{
  "ConnectionStrings": {
    "Database": "Server=localhost;Database=OrderDb;User Id=sa;Password=...;Encrypt=False;TrustServerCertificate=True;"
  }
}
```

Override `ConnectionStrings:Database` in environment variables or `appsettings.Development.json` for local development.

## Health Check

```
GET /health
```

Returns a JSON document with the SQL Server connection status. Format produced by `HealthChecks.UI.Client.UIResponseWriter`.

## Running Locally

### .NET CLI
```bash
# From the solution root
dotnet run --project src/Services/Ordering/Ordering.API
```

Default local URLs (from `launchSettings.json`):

| Profile | URL |
|---|---|
| `http` | `http://localhost:5003` |
| `https` | `https://localhost:5053` · `http://localhost:5003` |

The `Development` environment triggers automatic database migration and seeding on startup.

## Docker

### Build & run
```bash
# Build context is the src/ directory (DockerfileContext = ..\..\..  relative to the csproj)
docker build -f src/Services/Ordering/Ordering.API/Dockerfile -t ordering-api src/

docker run -p 8080:8080 \
  -e ConnectionStrings__Database="Server=host.docker.internal;Database=OrderDb;..." \
  ordering-api
```

### Dockerfile stages
| Stage | Base image | Purpose |
|---|---|---|
| `base` | `mcr.microsoft.com/dotnet/aspnet:10.0` | Runtime image; exposes 8080 / 8081 |
| `build` | `mcr.microsoft.com/dotnet/sdk:10.0` | Restore + compile |
| `publish` | `build` | `dotnet publish` to `/app/publish` |
| `final` | `base` | Copies published output; entrypoint `dotnet Ordering.API.dll` |

Container ports: `8080` (HTTP) · `8081` (HTTPS).
