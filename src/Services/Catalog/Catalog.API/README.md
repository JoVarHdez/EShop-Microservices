# Catalog API

A microservice for managing the product catalog in the eShop application. Built with ASP.NET Core (.NET 10) using a vertical slice architecture and CQRS pattern.

## Overview

The Catalog API provides full CRUD operations for products. It uses [Marten](https://martendb.io/) as a document database backed by PostgreSQL, [Carter](https://github.com/CarterCommunity/Carter) for minimal API endpoint routing, and [MediatR](https://github.com/jbogard/MediatR) to dispatch commands and queries.

## Technology Stack

| Concern | Library |
|---|---|
| Web framework | ASP.NET Core 10 Minimal APIs |
| Endpoint routing | Carter 10 |
| CQRS / Mediator | MediatR (via BuildingBlocks) |
| Document database | Marten 8 + PostgreSQL |
| Messaging | WolverineFx 5 + WolverineFx.Marten |
| Validation | FluentValidation |
| Health checks | AspNetCore.HealthChecks.NpgSql |
| Containerization | Docker (Linux) |

## Project Structure

```
Catalog.API/
├── Data/
│   └── CatalogInitialData.cs        # Seed data for development
├── Exceptions/
│   └── ProductNotFoundException.cs  # Domain-specific exception
├── Models/
│   └── Product.cs                   # Product entity
├── Products/                        # Vertical slices (one folder per feature)
│   ├── CreateProduct/
│   ├── DeleteProduct/
│   ├── GetProductByCategory/
│   ├── GetProductById/
│   ├── GetProducts/
│   └── UpdateProduct/
├── Program.cs                       # App composition root
├── appsettings.json
└── Dockerfile
```

Each feature folder contains an `*Endpoint.cs` (Carter route) and a `*Handler.cs` (MediatR command/query + FluentValidation).

## Product Model

```csharp
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<string> Categories { get; set; }
    public string Description { get; set; }
    public string ImageUrl { get; set; }
    public decimal Price { get; set; }
}
```

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/products` | List all products (paginated) |
| `GET` | `/products/{id}` | Get a product by ID |
| `GET` | `/products/category/{categoryId}` | Get products by category |
| `POST` | `/products` | Create a new product |
| `PUT` | `/products/{id}` | Update an existing product |
| `DELETE` | `/products/{id}` | Delete a product |
| `GET` | `/health` | Health check (PostgreSQL connectivity) |

### Pagination

The `GET /products` endpoint accepts optional query parameters:

| Parameter | Default | Description |
|-----------|---------|-------------|
| `pageNumber` | `1` | Page number (1-based) |
| `pageSize` | `10` | Number of items per page |

### Create Product — Request Body

```json
{
  "name": "string",
  "description": "string",
  "price": 0.00,
  "imageUrl": "https://example.com/image.png",
  "categories": ["string"]
}
```

**Validation rules:**
- `name` — required
- `categories` — at least one entry required
- `description` — required, max 250 characters
- `imageUrl` — required, must be a valid absolute URL
- `price` — must be greater than 0

## Configuration

The database connection string is read from `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Database": "Server=localhost;Port=5432;Database=CatalogDb;User Id=postgres;Password=password;Include Error Detail=true;"
  }
}
```

Override this value via environment variables or Docker Compose when deploying.

## Running Locally

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker Desktop (for PostgreSQL or full compose stack)

### With Docker Compose (recommended)

From the `src/` directory:

```bash
docker-compose up
```

This starts both the API and a PostgreSQL instance. Marten automatically seeds initial product data in the development environment.

### Without Docker

1. Start a local PostgreSQL instance and update the connection string in `appsettings.Development.json`.
2. Run the API:

```bash
cd src/Services/Catalog/Catalog.API
dotnet run
```

The API will be available at `https://localhost:5001` (or the port configured in `Properties/launchSettings.json`).

## Architecture Notes

- **Vertical slice architecture** — each feature is self-contained in its own folder with its endpoint, handler, command/query record, and validator.
- **CQRS** — commands and queries are defined as records implementing `ICommand<TResult>` / `IQuery<TResult>` from the shared `BuildingBlocks` project.
- **Cross-cutting behaviors** — `ValidationBehavior` (runs FluentValidation before every handler) and `LoggingBehavior` (logs request/response) are registered as MediatR pipeline behaviors via the `BuildingBlocks` library.
- **Exception handling** — `CustomExceptionHandler` from `BuildingBlocks` maps domain exceptions (e.g., `ProductNotFoundException`) to appropriate HTTP problem responses.
