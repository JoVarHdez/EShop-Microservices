# Ordering.Application

The application layer of the Ordering microservice. Implements use cases as CQRS commands and queries dispatched through MediatR, with cross-cutting pipeline behaviors for validation and logging. Contains no infrastructure or transport concerns.

## Target Framework & Dependencies

- .NET 10.0
- `Microsoft.EntityFrameworkCore` 10.0.8 (abstractions only — no provider)
- `BuildingBlocks` (project reference — CQRS markers, pipeline behaviors, pagination)
- `BuildingBlocks.Messaging` (project reference — MassTransit integration, `BasketCheckoutEvent`)
- `Ordering.Core` (project reference — domain models, value objects, events)

## Project Structure

```
Ordering.Application/
├── DepedencyInjection.cs            # IServiceCollection extension
├── Data/
│   └── IApplicationDbContext.cs     # Persistence abstraction
├── DTOs/                            # Data transfer objects (records)
├── Exceptions/
│   └── OrderNotFoundException.cs
├── Extensions/
│   └── OrderExtensions.cs           # Order → OrderDto projection
└── Orders/
    ├── Commands/
    │   ├── CreateOrder/
    │   ├── DeleteOrder/
    │   └── UpdateOrder/
    ├── EventHandlers/
    │   ├── Domain/                  # MediatR INotificationHandler implementations
    │   └── Integration/             # Integration event handlers (in progress)
    └── Queries/
        ├── GetOrders/               # Paginated list
        ├── GetOrderByCustomer/
        └── GetOrderByName/
```

## Registration

Call `AddApplicationServices` from the host project:

```csharp
builder.Services.AddApplicationServices(builder.Configuration);
```

This registers:

1. MediatR, scanning the assembly for all handlers, with two open generic pipeline behaviors:
   - `ValidationBehavior<,>` — runs FluentValidation validators before the handler
   - `LoggingBehavior<,>` — logs request/response details around the handler
2. `AddFeatureManagement()` — ASP.NET Core Feature Management (reads `FeatureManagement` config section)
3. `AddMessageBroker(configuration, assembly)` — MassTransit/RabbitMQ consumer registration, scanning the assembly for `IConsumer<T>` implementations

## Persistence Abstraction

`IApplicationDbContext` decouples the application layer from the EF Core implementation. Handlers depend on this interface; `ApplicationDbContext` in `Ordering.Infrastructure` satisfies it.

```csharp
public interface IApplicationDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Product> Products { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

## DTOs

All DTOs are immutable C# `record` types used as the public contract between the API layer and application handlers.

| DTO | Fields |
|---|---|
| `OrderDto` | `Id`, `CustomerId`, `OrderName`, `ShippingAddress`, `BillingAddress`, `Payment`, `Status`, `OrderItems` |
| `OrderItemDto` | `OrderId`, `ProductId`, `Quantity`, `Price` |
| `AddressDto` | `FirstName`, `LastName`, `EmailAddress`, `AddressLine`, `Country`, `State`, `ZipCode` |
| `PaymentDto` | `CardName`, `CardNumber`, `Expiration`, `Cvv`, `PaymentMethod` |

`OrderExtensions` provides two projection helpers backed by a shared private `DtoFromOrder(Order)` method:

| Method | Signature | Used by |
|---|---|---|
| `ToOrderDtoList()` | `IEnumerable<Order>` → `IEnumerable<OrderDto>` | All query handlers |
| `ToOrderDto()` | `Order` → `OrderDto` | `OrderCreatedEventHandler` (integration event) |

## Commands

### `CreateOrderCommand`
| | |
|---|---|
| **Input** | `OrderDto Order` |
| **Output** | `CreateOrderResult(Guid Id)` |
| **Validation** | `OrderName` not empty · `CustomerId` not null · `OrderItems` not empty |
| **Handler** | Constructs value objects, calls `Order.Create(...)`, adds all order items, persists via `IApplicationDbContext` |

### `UpdateOrderCommand`
| | |
|---|---|
| **Input** | `OrderDto Order` |
| **Output** | `UpdateOrderResult(bool IsSuccess)` |
| **Validation** | `Order.Id` not empty · `CustomerId` not null · `OrderName` not empty |
| **Handler** | Loads order by `OrderId`, throws `OrderNotFoundException` if missing, calls `Order.Update(...)`, persists |

### `DeleteOrderCommand`
| | |
|---|---|
| **Input** | `Guid OrderId` |
| **Output** | `DeleteOrderResult(bool IsSuccess)` |
| **Validation** | `OrderId` not empty |
| **Handler** | Loads order by `OrderId`, throws `OrderNotFoundException` if missing, removes and persists |

## Queries

### `GetOrdersQuery`
| | |
|---|---|
| **Input** | `PaginationRequest` (`PageIndex`, `PageSize`) |
| **Output** | `GetOrdersResult(PaginatedResult<OrderDto>)` |
| **Behaviour** | Returns all orders sorted by `OrderName`, paginated; includes `OrderItems`; uses `AsNoTracking` implicitly via projection |

### `GetOrderByCustomerQuery`
| | |
|---|---|
| **Input** | `Guid CustomerId` |
| **Output** | `GetOrderByCustomerResult(IEnumerable<OrderDto>)` |
| **Behaviour** | Filters by `CustomerId` value object, sorted by `OrderName`, includes `OrderItems`, `AsNoTracking` |

### `GetOrdersByNameQuery`
| | |
|---|---|
| **Input** | `string Name` |
| **Output** | `GetOrdersByNameResult(IEnumerable<OrderDto>)` |
| **Behaviour** | Contains-match on `OrderName.Value`, sorted by `OrderName`, includes `OrderItems`, `AsNoTracking` |

## Event Handlers

### Domain Event Handlers

Registered automatically by MediatR. Invoked by `DispatchDomainEventsInterceptor` in the infrastructure layer during `SaveChangesAsync`.

| Handler | Event | Current Behaviour |
|---|---|---|
| `OrderCreatedEventHandler` | `OrderCreatedEvent` | Logs order ID; if the `OrderFullfilment` feature flag is enabled, maps the order to `OrderDto` via `ToOrderDto()` and publishes it as an integration event through MassTransit `IPublishEndpoint` |
| `OrderUpdatedEventHandler` | `OrderUpdatedEvent` | Logs event type name at Information level |

### Integration Event Handlers

| Handler | Consumer type | Behaviour |
|---|---|---|
| `BasketCheckoutEventHandler` | `IConsumer<BasketCheckoutEvent>` | Receives a `BasketCheckoutEvent` from the message broker; maps it to a `CreateOrderCommand` (with a fixed two-item order) and sends it via MediatR `ISender` |

## Exceptions

`OrderNotFoundException` extends `NotFoundException` from `BuildingBlocks.Exceptions` and is thrown by `UpdateOrderHandler` and `DeleteOrderHandler` when the requested order does not exist. The base `NotFoundException` message format is:

```
Order (id) was not found.
```

## Request Flow

```
API layer
  └─► IMediator.Send(command / query)
        └─► LoggingBehavior  ──► logs before/after
              └─► ValidationBehavior  ──► runs FluentValidation; throws on failure
                    └─► Handler  ──► executes use case via IApplicationDbContext
                          └─► SaveChangesAsync
                                ├─ AuditableEntityInterceptor  ──► stamps audit fields
                                └─ DispatchDomainEventsInterceptor  ──► publishes domain events
                                      └─► INotificationHandler  ──► e.g. OrderCreatedEventHandler
```
