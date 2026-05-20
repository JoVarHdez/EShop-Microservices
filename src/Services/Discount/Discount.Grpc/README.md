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

> [!WARNING]
> **Container data loss risk.** The connection string `Data Source=discountdb` resolves to the current working directory inside the container. The `discountdb` file lives in the container's writable layer and is **permanently lost when the container is removed or replaced**. This is acceptable for local development and course exercises, but is not suitable for any persistent workload. See [Database Alternatives](#database-alternatives) below.

**Seed data** (applied on first run):

| Id | ProductName | Description | Amount |
|---|---|---|---|
| 1 | IPhone X | IPhone Discount | 150 |
| 2 | Samsung 10 | Samsung Discount | 100 |

### Applying Migrations Manually

```bash
dotnet ef database update
```

### Database Alternatives

When moving beyond local development, replace SQLite with a database that supports persistent, shared storage in a containerized environment. The EF Core model and migrations require minimal changes for each option.

#### PostgreSQL *(recommended)*

The strongest choice for this microservices stack. Catalog.API already uses PostgreSQL (via Marten/Marten), so the infrastructure is already present in the Docker Compose environment. PostgreSQL supports concurrent access, is fully ACID-compliant, and has excellent .NET tooling.

| Item | Value |
|---|---|
| EF Core provider | `Npgsql.EntityFrameworkCore.PostgreSQL` |
| Connection string | `Host=postgres;Database=discountdb;Username=postgres;Password=...` |
| Docker image | `postgres:16-alpine` |

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

In `Program.cs`, replace:
```csharp
options.UseSqlite(builder.Configuration.GetConnectionString("Database"))
```
with:
```csharp
options.UseNpgsql(builder.Configuration.GetConnectionString("Database"))
```

Then regenerate migrations: `dotnet ef migrations add InitialCreate --context DiscountContext`

#### SQL Server

A natural fit if the team is already operating SQL Server infrastructure or running in an Azure-hosted environment (Azure SQL).

| Item | Value |
|---|---|
| EF Core provider | `Microsoft.EntityFrameworkCore.SqlServer` |
| Connection string | `Server=sqlserver;Database=DiscountDb;User Id=sa;Password=...` |
| Docker image | `mcr.microsoft.com/mssql/server:2022-latest` |

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

#### MySQL / MariaDB

A widely deployed open-source option with strong container support.

| Item | Value |
|---|---|
| EF Core provider | `Pomelo.EntityFrameworkCore.MySql` |
| Connection string | `Server=mysql;Database=discountdb;User=root;Password=...` |
| Docker image | `mysql:8.4` or `mariadb:11` |

```bash
dotnet add package Pomelo.EntityFrameworkCore.MySql
```

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| `Grpc.AspNetCore` | 2.80.0 | gRPC server |
| `Grpc.AspNetCore.Server.Reflection` | 2.80.0 | gRPC reflection |
| `Mapster` | 10.0.7 | Object mapping (entity ↔ proto model) |
| `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.8 | SQLite provider |
| `Microsoft.EntityFrameworkCore.Tools` | 10.0.8 | EF Core CLI tooling |
