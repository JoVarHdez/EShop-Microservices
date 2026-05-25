# Basket.API

A microservice responsible for managing shopping cart (basket) operations in the eShop application. Built with ASP.NET Core (.NET 10) using a vertical slice architecture.

## Features

- **CRUD operations** for shopping carts per user
- **Checkout** — publishes a `BasketCheckoutEvent` to RabbitMQ via MassTransit, then removes the basket
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
└── CheckoutBasket/   # POST /basket/checkout
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
| `POST` | `/basket/checkout` | Publish a `BasketCheckoutEvent` and clear the basket |
| `GET` | `/health` | Health check endpoint |

## Dependencies

| Package | Source | Purpose |
|---|---|---|
| [Carter](https://github.com/CarterCommunity/Carter) | Basket.API | Minimal API endpoint modules (`ICarterModule`) |
| [MediatR](https://github.com/jbogard/MediatR) | BuildingBlocks | CQRS mediator — `ISender`, `IRequest` |
| [Marten](https://martendb.io/) | Basket.API | PostgreSQL document store / ORM |
| [Mapster](https://github.com/MapsterMapper/Mapster) | BuildingBlocks | Object mapping (`.Adapt<>()`) in endpoints |
| [Scrutor](https://github.com/khellang/Scrutor) | Basket.API | Decorator registration for `IBasketRepository` |
| [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/) | Basket.API | Distributed cache (`IDistributedCache`) |
| [FluentValidation](https://fluentvalidation.net/) | BuildingBlocks | Request validation via MediatR pipeline |
| [Grpc.AspNetCore](https://grpc.io/) | Basket.API | gRPC client for Discount service |
| [MassTransit.RabbitMQ](https://masstransit.io/) | BuildingBlocks.Messaging | RabbitMQ transport — publishes `BasketCheckoutEvent` |
| [WolverineFx](https://wolverine.netlify.app/) | Basket.API / BuildingBlocks | Included for planned migration from MediatR |
| [WolverineFx.FluentValidation](https://wolverine.netlify.app/) | Basket.API | Wolverine validation middleware (planned) |
| [WolverineFx.Marten](https://wolverine.netlify.app/) | Basket.API | Wolverine–Marten session integration (planned) |
| BuildingBlocks | Project ref | Shared CQRS interfaces, behaviors, and exception handler |
| BuildingBlocks.Messaging | Project ref | `AddMessageBroker` helper + `BasketCheckoutEvent` definition |

## Configuration

| Key | Description |
|---|---|
| `ConnectionStrings:Database` | PostgreSQL connection string |
| `ConnectionStrings:Redis` | Redis connection string |
| `GrpcSettings:DiscountUrl` | URL for the Discount gRPC service |
| `MessageBroker:Host` | RabbitMQ AMQP URI |
| `MessageBroker:Username` | RabbitMQ username |
| `MessageBroker:Password` | RabbitMQ password |

**Local (`appsettings.json`):**

```json
{
  "ConnectionStrings": {
    "Database": "Server=localhost;Port=5433;Database=BasketDb;User Id=postgres;Password=password;",
    "Redis": "localhost:6379"
  },
  "GrpcSettings": {
    "DiscountUrl": "https://localhost:5052"
  },
  "MessageBroker": {
    "Host": "amqp://localhost:5672",
    "Username": "guest",
    "Password": "guest"
  }
}
```

**Docker (`docker-compose.override.yml`):**

```yaml
ConnectionStrings__Database: Server=basketdb;Port=5432;Database=BasketDb;User Id=postgres;Password=password;
ConnectionStrings__Redis: distributedcache:6379
GrpcSettings__DiscountUrl: https://discount.grpc:8081
MessageBroker__Host: amqp://ecommerce-mq:5672
MessageBroker__Username: guest
MessageBroker__Password: guest
```

## Running Locally

### Prerequisites

- .NET 10 SDK
- Docker (for PostgreSQL and Redis)

### Start infrastructure

```bash
docker-compose up -d
```

This starts PostgreSQL (`basketdb`), Redis (`distributedcache`), Discount gRPC, and **RabbitMQ** (`messagebroker`). The RabbitMQ management UI is available at `http://localhost:15672` (guest/guest).

### Run the service

```bash
cd src/Services/Basket/Basket.API
dotnet run
```

| Profile | URL |
|---|---|
| `http` | http://localhost:5001 |
| `https` | https://localhost:5051 and http://localhost:5001 |
| Docker | http://localhost:6001 (HTTP) / https://localhost:6061 (HTTPS) |

See `Properties/launchSettings.json` for full profile details.

## Async Messaging

Basket.API acts as a **publisher only** — it has no message consumers.

| Event | Trigger | Destination |
|---|---|---|
| `BasketCheckoutEvent` | `POST /basket/checkout` | RabbitMQ exchange → consumed by `Ordering.API` |

### Checkout flow
1. Validate `BasketCheckoutDto` (UserName required)
2. Load the existing basket to calculate `TotalPrice`
3. Map `BasketCheckoutDto` → `BasketCheckoutEvent` and set `TotalPrice`
4. Publish the event via MassTransit `IPublishEndpoint`
5. Delete the basket

MassTransit is registered with `AddMessageBroker(configuration)` (no assembly argument — no consumers to scan).

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
├── DTOs/                     # Data transfer objects
│   └── BasketCheckoutDto.cs
├── Models/                   # Domain models
│   ├── ShoppingCart.cs
│   └── ShoppingCartItem.cs
├── Exceptions/               # Custom exceptions
│   └── BasketNotFoundException.cs
├── Program.cs                # Application entry point & DI registration
└── Dockerfile
```
