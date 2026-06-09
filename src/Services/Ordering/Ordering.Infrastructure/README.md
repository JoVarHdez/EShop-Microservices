# Ordering.Infrastructure

The infrastructure layer of the Ordering microservice. Implements persistence concerns using Entity Framework Core with SQL Server — EF model configuration, SaveChanges interceptors, database migrations, and startup seeding — with no business logic of its own.

## Target Framework & Dependencies

- .NET 10.0
- `Microsoft.EntityFrameworkCore.SqlServer` 10.0.8
- `Microsoft.EntityFrameworkCore.Tools` / `.Design` 10.0.8 (build-time only)
- `Microsoft.AspNetCore.App` (framework reference)
- `WolverineFx` 5.39.1
- `Ordering.Application` (project reference — depends on application layer interfaces)

## Project Structure

```
Ordering.Infrastructure/
├── DepedencyInjection.cs          # IServiceCollection extension — wires up EF Core and interceptors
├── Data/
│   ├── ApplicationDbContext.cs    # EF Core DbContext
│   ├── Configurations/            # IEntityTypeConfiguration implementations per entity
│   ├── Interceptors/              # SaveChangesInterceptor implementations
│   └── Migrations/                # EF Core migration history
└── Extensions/
    ├── DatabaseExtension.cs       # WebApplication extension — auto-migrate + seed on startup
    └── InitialData.cs             # Seed data definitions
```

## Registration

Call `AddInfrastructureServices` from the host project's service configuration:

```csharp
builder.Services.AddInfrastructureServices(builder.Configuration);
```

The extension reads `ConnectionStrings:Database` from `IConfiguration` and registers:

1. `AuditableEntityInterceptor` (scoped `ISaveChangesInterceptor`)
2. `DispatchDomainEventsInterceptor` (scoped `ISaveChangesInterceptor`)
3. `ApplicationDbContext` with SQL Server provider and both interceptors injected via the DI provider
4. `IApplicationDbContext` → `ApplicationDbContext` (scoped) — the abstraction used by the application layer

## ApplicationDbContext

`ApplicationDbContext` exposes four `DbSet<T>` properties and applies all `IEntityTypeConfiguration<T>` implementations found in the assembly automatically via `ApplyConfigurationsFromAssembly`.

| DbSet | Entity |
|---|---|
| `Customers` | `Customer` |
| `Orders` | `Order` |
| `OrderItems` | `OrderItem` |
| `Products` | `Product` |

## Entity Configurations

Value object `Id` properties use EF Core value converters to map between the strongly-typed wrapper and the underlying `Guid`.

### `CustomerConfiguration`
| Column | Constraint |
|---|---|
| `Id` | PK, converted from `CustomerId` |
| `Name` | Required, max 100 |
| `Email` | Max 255, unique index |

### `ProductConfiguration`
| Column | Constraint |
|---|---|
| `Id` | PK, converted from `ProductId` |
| `Name` | Required, max 100 |
| `Price` | `decimal(18,2)` |

### `OrderConfiguration`
| Column / Property | Constraint |
|---|---|
| `Id` | PK, converted from `OrderId` |
| `CustomerId` | FK → Customers (cascade delete) |
| `OrderName` | Complex property, stored as single column, max 100, required |
| `ShippingAddress` | Complex property, seven inline columns (max lengths 50–180), all required |
| `BillingAddress` | Complex property, same shape as `ShippingAddress` (FirstName max 100) |
| `Payment` | Complex property: `CardNumber` max 24 required, `CVV` max 3, `Expiration` max 10, `CardName` max 50 nullable |
| `Status` | Stored as string, defaults to `"Draft"` |
| `OrderItems` | One-to-many, FK on `OrderItem.OrderId` |

### `OrderItemConfiguration`
| Column | Constraint |
|---|---|
| `Id` | PK, converted from `OrderItemId` |
| `OrderId` | FK → Orders (cascade delete) |
| `ProductId` | FK → Products (cascade delete) |
| `Quantity` | Required, `int` |
| `Price` | Required, `decimal(18,2)` |

## Interceptors

### `AuditableEntityInterceptor`
Hooks into `SavingChanges` / `SavingChangesAsync` to populate audit fields on every `IEntity` tracked by the context:

| State | Fields populated |
|---|---|
| `Added` | `CreatedBy = "System"`, `CreatedAt = UtcNow` |
| `Added`, `Modified`, or owned entity changed | `LastModifiedBy = "System"`, `LastModified = UtcNow` |

> The `"System"` placeholder should be replaced with the authenticated user when an HTTP context or identity service is available.

### `DispatchDomainEventsInterceptor`
Hooks into `SavingChanges` / `SavingChangesAsync` to publish domain events collected inside aggregate roots **before** `SaveChanges` completes:

1. Collects all `IAggregate` entries in the change tracker that have pending domain events.
2. Drains and clears the event list from each aggregate (`ClearDomainEvents`).
3. Publishes each event through Wolverine `IMessageBus.PublishAsync`.

This ensures event handlers run within the same unit of work as the database write.

## Database Initialization

Call `InitializeDatabaseAsync` only after the web host has started:

```csharp
await app.StartAsync();
await app.InitializeDatabaseAsync();
await app.WaitForShutdownAsync();
```

This ordering matters because `ApplicationDbContext` uses `DispatchDomainEventsInterceptor`, and that interceptor publishes through Wolverine `IMessageBus`. Wolverine cannot process messages until the underlying host has started, so calling `InitializeDatabaseAsync()` before `StartAsync()` can throw `WolverineHasNotStartedException`.

This extension method:
1. Runs any pending EF Core migrations (`MigrateAsync`).
2. Seeds reference data if the respective tables are empty (idempotent).

### Seed Data (`InitialData`)

| Entity | Records |
|---|---|
| `Customer` | John Doe, Jane Smith |
| `Product` | Laptop ($999.99), Smartphone ($499.99), Headphones ($199.99), Smartwatch ($299.99) |
| `Order` | `ORD_1` (John, Laptop × 1 + Headphones × 2), `ORD_2` (Jane, Smartphone × 1 + Smartwatch × 2) |

## Database Schema

The `InitialCreate` migration produces the following tables:

```
Customers    (Id, Name, Email, audit columns)
Products     (Id, Name, Price, audit columns)
Orders       (Id, CustomerId→Customers, OrderName, Status,
              BillingAddress_*, ShippingAddress_*, Payment_*,
              TotalAmount, audit columns)
OrderItems   (Id, OrderId→Orders, ProductId→Products,
              Quantity, Price, audit columns)
```

**Migration history:**

| Migration | Date | Change |
|---|---|---|
| `InitialCreate` | 2026-05-21 | Initial schema — all four tables |
| `UpdateSomeProperty` | 2026-05-25 | Renames `BillingAddress_EmailAdress` → `BillingAddress_EmailAddress` and `ShippingAddress_EmailAdress` → `ShippingAddress_EmailAddress` in the Orders table (typo fix) |

**Indexes created by the migration:**

| Index | Table | Unique |
|---|---|---|
| `IX_Customers_Email` | Customers | Yes |
| `IX_Orders_CustomerId` | Orders | No |
| `IX_OrderItems_OrderId` | OrderItems | No |
| `IX_OrderItems_ProductId` | OrderItems | No |

## EF Core Migrations

Common commands (run from the solution root or with `--project` / `--startup-project` flags):

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> \
  --project src/Services/Ordering/Ordering.Infrastructure \
  --startup-project src/Services/Ordering/Ordering.API

# Apply migrations manually
dotnet ef database update \
  --project src/Services/Ordering/Ordering.Infrastructure \
  --startup-project src/Services/Ordering/Ordering.API
```

Migrations are applied automatically at startup via `InitializeDatabaseAsync`, so manual `database update` is only required for local development without running the API.
