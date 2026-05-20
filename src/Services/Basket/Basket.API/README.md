# Basket.API

A microservice responsible for managing shopping cart (basket) operations in the eShop application. Built with ASP.NET Core (.NET 10) using a vertical slice architecture.

## Features

- **CRUD operations** for shopping carts per user
- **Discount integration** via gRPC — deducts discounts from item prices at storage time
- **Redis caching** — decorator pattern applied transparently over the primary data store
- **PostgreSQL persistence** via Marten (document store)
- **CQRS** with MediatR and strongly-typed commands/queries
- **FluentValidation** for input validation
- **Health checks** for PostgreSQL and Redis

## Architecture

The service follows a vertical slice architecture. Each feature lives in its own folder under `Basket/`:

```
Basket/
├── GetBasket/        # GET /basket/{userName}
├── StoreBasket/      # POST /basket
├── DeleteBasket/     # DELETE /basket/{userName}
└── CheckoutBasket/   # (in progress)
```

Each slice contains an **Endpoint** (Carter `ICarterModule`) and a **Handler** (MediatR command/query).

### Data Layer

| Class | Description |
|---|---|
| `BasketRepository` | Primary repository — reads/writes `ShoppingCart` documents to PostgreSQL via Marten |
| `CachedBasketRepository` | Decorator over `BasketRepository` — transparently caches results in Redis using `IDistributedCache` |

The `CachedBasketRepository` is registered using **Scrutor's** decorator pattern, requiring no changes to consumers.

## API Endpoints

| Method | Route | Description |
|---|---|---|
| `GET` | `/basket/{userName}` | Retrieve the shopping cart for a user |
| `POST` | `/basket` | Create or update the shopping cart for a user |
| `DELETE` | `/basket/{userName}` | Delete the shopping cart for a user |
| `GET` | `/health` | Health check endpoint |

## Dependencies

| Package | Purpose |
|---|---|
| [Carter](https://github.com/CarterCommunity/Carter) | Minimal API endpoint modules |
| [MediatR](https://github.com/jbogard/MediatR) | CQRS mediator |
| [Marten](https://martendb.io/) | PostgreSQL document store / ORM |
| [Scrutor](https://github.com/khellang/Scrutor) | Decorator registration for `IBasketRepository` |
| [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/) | Distributed cache (Redis) |
| [FluentValidation](https://fluentvalidation.net/) | Request validation |
| [Grpc.AspNetCore](https://grpc.io/) | gRPC client for Discount service |
| [WolverineFx](https://wolverine.netlify.app/) | Messaging and saga support |
| BuildingBlocks | Shared CQRS interfaces, behaviors, and exception handler |

## Configuration

| Key | Description |
|---|---|
| `ConnectionStrings:Database` | PostgreSQL connection string |
| `ConnectionStrings:Redis` | Redis connection string |
| `GrpcSettings:DiscountUrl` | URL for the Discount gRPC service |

**Example `appsettings.json`:**

```json
{
  "ConnectionStrings": {
    "Database": "Server=localhost;Port=5433;Database=BasketDb;User Id=postgres;Password=password;",
    "Redis": "localhost:6379"
  },
  "GrpcSettings": {
    "DiscountUrl": "https://localhost:5052"
  }
}
```

## Running Locally

### Prerequisites

- .NET 10 SDK
- Docker (for PostgreSQL and Redis)

### Start infrastructure

```bash
docker-compose up -d
```

### Run the service

```bash
cd src/Services/Basket/Basket.API
dotnet run
```

The API will be available at `https://localhost:5001` (or as configured in `launchSettings.json`).

## Project Structure

```
Basket.API/
├── Basket/                   # Feature slices
│   ├── GetBasket/
│   ├── StoreBasket/
│   ├── DeleteBasket/
│   └── CheckoutBasket/
├── Data/                     # Repository implementations
│   ├── IBasketRepository.cs
│   ├── BasketRepository.cs
│   └── CachedBasketRepository.cs
├── Models/                   # Domain models
│   ├── ShoppingCart.cs
│   └── ShoppingCartItem.cs
├── Exceptions/               # Custom exceptions
│   └── BasketNotFoundException.cs
├── Program.cs                # Application entry point & DI registration
└── Dockerfile
```
