# Basket.API

A microservice responsible for managing shopping cart (basket) operations in the eShop application. Built with ASP.NET Core (.NET 10) using a vertical slice architecture.

## Features

- **CRUD operations** for shopping carts per user
- **Checkout** — publishes a `BasketCheckoutEvent` to RabbitMQ via MassTransit, then removes the basket
- **Discount integration** via gRPC — deducts discounts from item prices at storage time
- **Redis caching** — decorator pattern applied transparently over the primary data store
- **PostgreSQL persistence** via Marten (document store) integrated with Wolverine
- **In-process messaging** via Wolverine (`IMessageBus.InvokeAsync<T>()`) for write-side dispatch
- **FluentValidation** via Wolverine's `UseFluentValidation` middleware
- **Health checks** for PostgreSQL and Redis

## Architecture

The service follows a vertical slice architecture. Each feature lives in its own folder under `Basket/`:

```
Basket/
├── BasketEndpoints.cs  # MapGroup("/basket") aggregator — registers all routes
├── GetBasket/          # GET /basket/{userName}
├── StoreBasket/        # POST /basket
├── DeleteBasket/       # DELETE /basket/{userName}
└── CheckoutBasket/     # POST /basket/checkout
```

Each slice contains a **static endpoint class** (native `RouteGroupBuilder` extension method) and a **handler class** (Wolverine convention-based `Handle` method). Endpoints are all registered under the `/basket` `MapGroup` via `BasketEndpoints.MapBasketEndpoints()`.

### Data Layer

| Class | Description |
|---|---|
| `BasketRepository` | Primary repository — reads/writes `ShoppingCart` documents to PostgreSQL via Marten. `GetBasketAsync` returns `ShoppingCart?` (null when not found). |
| `CachedBasketRepository` | Decorator over `BasketRepository` — transparently caches results in Redis using `IDistributedCache`. Cache-set is skipped when the inner repository returns null. |

The `CachedBasketRepository` is registered using **.NET keyed services** — no third-party decorator library required:

```csharp
builder.Services.AddKeyedScoped<IBasketRepository, BasketRepository>("basket:inner");
builder.Services.AddScoped<IBasketRepository>(provider =>
    new CachedBasketRepository(
        provider.GetRequiredKeyedService<IBasketRepository>("basket:inner"),
        provider.GetRequiredService<IDistributedCache>()));
```

## API Endpoints

| Method | Route | Description |
|---|---|---|
| `GET` | `/basket/{userName}` | Retrieve the shopping cart for a user. Returns `404` when not found. |
| `POST` | `/basket` | Create or update the shopping cart for a user |
| `DELETE` | `/basket/{userName}` | Delete the shopping cart for a user |
| `POST` | `/basket/checkout` | Publish a `BasketCheckoutEvent` and clear the basket. Returns `404` when basket does not exist. |
| `GET` | `/health` | Health check endpoint |

## Dependencies

| Package | Source | Purpose |
|---|---|---|
| [WolverineFx](https://wolverine.netlify.app/) | Basket.API | In-process message bus (`IMessageBus`) — replaces MediatR `ISender` |
| [WolverineFx.FluentValidation](https://wolverine.netlify.app/) | Basket.API | Validates commands via `UseFluentValidation(RegistrationBehavior.ExplicitRegistration)` |
| [WolverineFx.Marten](https://wolverine.netlify.app/) | Basket.API | Wolverine–Marten session integration (`.IntegrateWithWolverine()`) |
| [Marten](https://martendb.io/) | Basket.API | PostgreSQL document store / ORM |
| [Mapster](https://github.com/MapsterMapper/Mapster) | Basket.API | Object mapping (`.Adapt<>()`) in endpoints and handlers |
| [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/) | Basket.API | Distributed cache (`IDistributedCache`) |
| [FluentValidation](https://fluentvalidation.net/) | Basket.API | Validator classes; wired into Wolverine pipeline |
| [Grpc.AspNetCore](https://grpc.io/) | Basket.API | gRPC client for Discount service |
| [MassTransit.RabbitMQ](https://masstransit.io/) | BuildingBlocks.Messaging | RabbitMQ transport — publishes `BasketCheckoutEvent` via `IPublishEndpoint` |
| BuildingBlocks | Project ref | Shared generic `CustomExceptionHandler` (`BuildingBlocks.Exceptions.Handler`) |
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
│   ├── BasketEndpoints.cs    # MapGroup aggregator — registers all routes
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
├── Program.cs                # Application entry point & DI registration
└── Dockerfile
```

## Exception Handling

`Program.cs` registers a shared generic exception handler from BuildingBlocks:

```csharp
builder.Services.AddExceptionHandler<CustomExceptionHandler>();
```

Implementation location:

`src/BuildingBlocks/BuildingBlocks/Exceptions/Handler/CustomExceptionHandler.cs`
