# Discount.Grpc

A gRPC microservice that manages discount coupons for products. It exposes a Protobuf-defined service consumed by other services (e.g., Basket.API) to retrieve and manage discount amounts per product.

## Overview

| Property | Value |
|---|---|
| Framework | .NET 10 / ASP.NET Core |
| Protocol | gRPC (HTTP/2) |
| Database | SQLite (`discountdb`) |
| ORM | Entity Framework Core 10 |

## gRPC Service

Defined in [`Protos/discount.proto`](Protos/discount.proto).

**Service:** `DiscountProtoService`

| RPC | Request | Response | Description |
|---|---|---|---|
| `GetDiscount` | `GetDiscountRequest` (productName) | `CouponModel` | Returns the coupon for a product. Returns amount `0` if none found. |
| `CreateDiscount` | `CreateDiscountRequest` (coupon) | `CouponModel` | Creates a new coupon. |
| `UpdateDiscount` | `UpdateDiscountRequest` (coupon) | `CouponModel` | Updates an existing coupon. |
| `DeleteDiscount` | `DeleteDiscountRequest` (productName) | `DeleteDiscountResponse` (success) | Deletes the coupon for a product. |

### CouponModel

| Field | Type | Description |
|---|---|---|
| `id` | int32 | Unique identifier |
| `productName` | string | Product the coupon applies to |
| `description` | string | Human-readable description |
| `amount` | int32 | Discount amount |

## Project Structure

```
Discount.Grpc/
├── Data/
│   ├── DiscountContext.cs      # EF Core DbContext with seed data
│   └── Extensions.cs           # Migration auto-apply helper
├── Migrations/                 # EF Core migration files
├── Models/
│   └── Coupon.cs               # Domain entity
├── Protos/
│   └── discount.proto          # Protobuf service definition
├── Services/
│   └── DiscountService.cs      # gRPC service implementation
├── Program.cs
└── appsettings.json
```

## Configuration

**`appsettings.json`**

```json
{
  "ConnectionStrings": {
    "Database": "Data Source=discountdb"
  },
  "Kestrel": {
    "EndpointDefaults": {
      "Protocols": "Http2"
    }
  }
}
```

The database connection string can be overridden via environment variable:
```
ConnectionStrings__Database=Data Source=/path/to/discountdb
```

## Running Locally

```bash
dotnet run --project Discount.Grpc.csproj
```

Default local URLs:

| Profile | URL |
|---|---|
| HTTP | `http://localhost:5002` |
| HTTPS | `https://localhost:5052` |

> gRPC reflection is enabled in the `Development` environment, allowing tools like [grpcurl](https://github.com/fullstorydev/grpcurl) or [Postman](https://www.postman.com/) to introspect the service.

## Running with Docker

```bash
docker build -t discount-grpc -f Dockerfile ../../..
docker run -p 8080:8080 discount-grpc
```

Or via Docker Compose from the `src/` folder:

```bash
docker-compose up discount.grpc
```

Container ports: `8080` (HTTP) / `8081` (HTTPS).

## Database

SQLite is used for persistence. The database file (`discountdb`) is created automatically on startup via EF Core migrations.

**Seed data** (applied on first run):

| Id | ProductName | Description | Amount |
|---|---|---|---|
| 1 | IPhone X | IPhone Discount | 150 |
| 2 | Samsung 10 | Samsung Discount | 100 |

### Applying Migrations Manually

```bash
dotnet ef database update
```

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| `Grpc.AspNetCore` | 2.80.0 | gRPC server |
| `Grpc.AspNetCore.Server.Reflection` | 2.80.0 | gRPC reflection |
| `Mapster` | 10.0.7 | Object mapping (entity ↔ proto model) |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.8 | SQLite provider |
| `Microsoft.EntityFrameworkCore.Tools` | 10.0.8 | EF Core CLI tooling |
