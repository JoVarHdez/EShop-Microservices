# BuildingBlocks.Messaging

A shared messaging library for the eShop microservices solution, providing integration event definitions and MassTransit/RabbitMQ configuration helpers used across all services.

## Target Framework

- .NET 10.0

## Dependencies

| Package | Version |
|---|---|
| `MassTransit.RabbitMQ` | 9.1.1 |

## Project Structure

```
BuildingBlocks.Messaging/
├── Events/
│   ├── IntegrationEvent.cs       # Base record for all integration events
│   └── BasketCheckoutEvent.cs    # Event published when a basket is checked out
└── MassTransit/
    └── Extensions.cs             # IServiceCollection extension to register MassTransit + RabbitMQ
```

## Key Components

### `IntegrationEvent` (base record)

Located in `Events/IntegrationEvent.cs`. All integration events inherit from this record.

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Unique identifier generated per instance |
| `OccurredOn` | `DateTime` | UTC timestamp of when the event occurred |
| `EventType` | `string` | Assembly-qualified type name of the concrete event |

### `BasketCheckoutEvent`

Located in `Events/BasketCheckoutEvent.cs`. Published by **Basket.API** when a user completes checkout. Consumed by **Ordering.API** to create a new order.

| Property | Type | Description |
|---|---|---|
| `UserName` | `string` | Buyer's username |
| `CustomerId` | `Guid` | Buyer's customer ID |
| `TotalPrice` | `decimal` | Total order price |
| `FirstName` / `LastName` | `string` | Shipping/billing name |
| `EmailAddress` | `string` | Contact email |
| `AddressLine` / `Country` / `State` / `ZipCode` | `string` | Shipping address |
| `CardName` / `CardNumber` / `Expiration` / `CVV` | `string` | Payment card details |
| `PaymentMethod` | `int` | Payment method identifier |

### `Extensions.AddMessageBroker`

Located in `MassTransit/Extensions.cs`. Registers MassTransit with a RabbitMQ transport via a single `IServiceCollection` extension method.

```csharp
builder.Services.AddMessageBroker(builder.Configuration, typeof(Program).Assembly);
```

Reads the following configuration keys:

| Key | Description |
|---|---|
| `MessageBroker:Host` | RabbitMQ host URI (e.g. `amqp://localhost`) |
| `MessageBroker:Username` | RabbitMQ username |
| `MessageBroker:Password` | RabbitMQ password |

Endpoint names are formatted using **kebab-case**. Consumers are auto-discovered from the provided assembly (optional).

## Configuration Example

```json
{
  "MessageBroker": {
    "Host": "amqp://localhost",
    "Username": "guest",
    "Password": "guest"
  }
}
```

## Usage

1. Add a project reference to `BuildingBlocks.Messaging`.
2. Call `AddMessageBroker` in your service's `Program.cs`, passing in the scanning assembly if the service has consumers:

```csharp
// Producer only (e.g. Basket.API)
builder.Services.AddMessageBroker(builder.Configuration);

// Producer + Consumer (e.g. Ordering.API)
builder.Services.AddMessageBroker(builder.Configuration, typeof(Program).Assembly);
```

3. Implement `IConsumer<BasketCheckoutEvent>` (or any other `IntegrationEvent` subclass) in the consuming service and MassTransit will wire it up automatically.
