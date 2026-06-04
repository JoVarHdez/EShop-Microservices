# Ordering.Core

The domain layer of the Ordering microservice. Contains all business logic, domain models, value objects, domain events, and abstractions — with no dependencies on infrastructure or application concerns.

## Target Framework

- .NET 10.0

## Project Structure

```
Ordering.Core/
├── Abstractions/       # Base classes and interfaces for DDD building blocks
├── Enums/              # Domain enumerations
├── Events/             # Domain events
├── Exceptions/         # Domain-specific exceptions
├── Models/             # Aggregate roots and entities
└── ValueObjects/       # Immutable value objects
```

## Abstractions

| Type | Description |
|---|---|
| `IEntity` | Audit fields: `CreatedAt`, `CreatedBy`, `LastModified`, `LastModifiedBy` |
| `IEntity<T>` | Extends `IEntity` with a strongly-typed `Id` |
| `Entity<T>` | Base class implementing `IEntity<T>` |
| `IAggregate` | Exposes `DomainEvents` collection and `ClearDomainEvents()` |
| `IAggregate<T>` | Combines `IAggregate` and `IEntity<T>` |
| `Aggregate<TId>` | Base class for aggregate roots; manages the internal domain event list |
| `IDomainEvent` | Domain event contract with default metadata (`EventId`, `OcurredOn`, `EventType`) |

## Models

### `Order` (Aggregate Root)
The central aggregate. Owns a collection of `OrderItem`s and raises domain events on state changes.

| Member | Description |
|---|---|
| `CustomerId` | Reference to the owning customer |
| `OrderName` | Human-readable order identifier (value object) |
| `ShippingAddress` / `BillingAddress` | Delivery and billing addresses (value objects) |
| `Payment` | Payment details (value object) |
| `Status` | Current `OrderStatus` (defaults to `Pending`) |
| `TotalAmount` | Computed as the sum of `Price × Quantity` across all items |
| `Create(...)` | Factory method; raises `OrderCreatedEvent` |
| `Update(...)` | Updates order fields; raises `OrderUpdatedEvent` |
| `Add(productId, quantity, price)` | Adds an `OrderItem`; validates positive quantity and price |
| `Remove(orderItemId)` | Removes an `OrderItem` by id |

### `OrderItem` (Entity)
Represents a single line item within an order. Created only through `Order.Add(...)`.

### `Customer` (Entity)
Holds customer identity (`Name`, `Email`). Created via `Customer.Create(...)` factory with null/whitespace guards.

### `Product` (Entity)
Holds product details (`Name`, `Price`). Created via `Product.Create(...)` factory with validation.

## Value Objects

All value objects are C# `record` types with private constructors and a static `Of(...)` factory.

| Value Object | Wrapped Type | Validation |
|---|---|---|
| `OrderId` | `Guid` | Must not be `Guid.Empty` |
| `OrderItemId` | `Guid` | Must not be `Guid.Empty` |
| `CustomerId` | `Guid` | Must not be `Guid.Empty` |
| `ProductId` | `Guid` | Must not be `Guid.Empty` |
| `OrderName` | `string` | Must not be null or whitespace |
| `Address` | Composite | `AddressLine` and `EmailAddress` must not be null or whitespace |
| `Payment` | Composite (card details) | Validated at factory level |

## Domain Events

Raised inside aggregate methods and dispatched by the application layer after persistence.

| Event | Trigger |
|---|---|
| `OrderCreatedEvent(Order)` | `Order.Create(...)` |
| `OrderUpdatedEvent(Order)` | `Order.Update(...)` |

## Enums

```csharp
public enum OrderStatus
{
    Draft     = 1,
    Pending   = 2,
    Completed = 3,
    Cancelled = 4
}
```

## Exceptions

`DomainException` — thrown when a domain invariant is violated. Formats the message as:

```
Domain Exception: "<message>" throws from Domain Layer.
```

## Design Notes

- **No external framework dependency.** The domain event contract does not depend on MediatR or Wolverine abstractions.
- **Encapsulation.** All model mutations go through factory methods or explicit domain operations; setters are `private`.
- **Validation at the boundary.** Value objects and factory methods use `ArgumentException`/`ArgumentOutOfRangeException` guards so invalid state is impossible to construct.
- **Domain event pattern.** Events are collected inside the aggregate and cleared by the application layer after dispatch, keeping the domain free of side-effect concerns.
