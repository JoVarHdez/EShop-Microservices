# Implementation Plan: Ordering.Application — BasketCheckoutEventHandler Wolverine Modernization

> Spec: [`docs/specs/20260526-orderingBasketCheckoutEventHandlerModernization.md`](../specs/20260526-orderingBasketCheckoutEventHandlerModernization.md)

**TL;DR** — Single-phase modernization: replace MediatR's `ISender` with Wolverine's `IMessageBus` in the `BasketCheckoutEventHandler` integration event consumer. MassTransit's `IConsumer<BasketCheckoutEvent>` stays unchanged.

---

## Relevant Files

| Action | File |
|--------|------|
| MODIFY | `src/Services/Ordering/Ordering.Application/Orders/EventHandlers/Integration/BasketCheckoutEventHandler.cs` |

---

## Phase 1 — Replace ISender with IMessageBus in BasketCheckoutEventHandler

The handler receives `BasketCheckoutEvent` integration events from RabbitMQ via MassTransit's `IConsumer<T>` interface (which stays), maps the event to a `CreateOrderCommand`, and dispatches it internally. MediatR's `ISender` is replaced with Wolverine's `IMessageBus` for the internal dispatch.

### 1.1 — Update using directives

**File**: `src/Services/Ordering/Ordering.Application/Orders/EventHandlers/Integration/BasketCheckoutEventHandler.cs`

- Remove `using MediatR;`
- Add `using Wolverine;`

```csharp
// BEFORE
using BuildingBlocks.Messaging.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

// AFTER
using BuildingBlocks.Messaging.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Wolverine;
```

### 1.2 — Replace ISender with IMessageBus in the constructor

**File**: `src/Services/Ordering/Ordering.Application/Orders/EventHandlers/Integration/BasketCheckoutEventHandler.cs`

- Change the constructor parameter from `ISender sender` to `IMessageBus bus`
- Keep `ILogger<BasketCheckoutEventHandler> logger` unchanged

```csharp
// BEFORE
public class BasketCheckoutEventHandler(ISender sender, ILogger<BasketCheckoutEventHandler> logger) 
    : IConsumer<BasketCheckoutEvent>

// AFTER
public class BasketCheckoutEventHandler(IMessageBus bus, ILogger<BasketCheckoutEventHandler> logger) 
    : IConsumer<BasketCheckoutEvent>
```

> **Learning note**: The class still implements `IConsumer<BasketCheckoutEvent>` — this is the MassTransit interface for consuming RabbitMQ messages. Only the internal command dispatch mechanism changes.

### 1.3 — Replace sender.Send with bus.InvokeAsync

**File**: `src/Services/Ordering/Ordering.Application/Orders/EventHandlers/Integration/BasketCheckoutEventHandler.cs`

- Change `await sender.Send(command);` to `await bus.InvokeAsync(command);`

```csharp
// BEFORE
public async Task Consume(ConsumeContext<BasketCheckoutEvent> context)
{
    logger.LogInformation("Integration Event: {EventName} - {Id}", context.Message.GetType().Name, context.Message.Id);

    var command = MapToCreateOrderCommand(context.Message);
    await sender.Send(command);
}

// AFTER
public async Task Consume(ConsumeContext<BasketCheckoutEvent> context)
{
    logger.LogInformation("Integration Event: {EventName} - {Id}", context.Message.GetType().Name, context.Message.Id);

    var command = MapToCreateOrderCommand(context.Message);
    await bus.InvokeAsync(command);
}
```

> **Learning note**: `IMessageBus.InvokeAsync()` is Wolverine's in-process dispatch. Wolverine's FluentValidation middleware automatically applies `CreateOrderCommandValidator` before the handler runs (configured in the first spec's `UseWolverine()` call).

- **Unchanged**: `MapToCreateOrderCommand` private static method, logging statement, `ConsumeContext<BasketCheckoutEvent> context` parameter, `IConsumer<BasketCheckoutEvent>` interface implementation

---

## Verification

1. **Build**: `dotnet build src/eshop-microservies.slnx` must succeed with 0 errors.

2. **Grep check**: Search `src/Services/Ordering/Ordering.Application/Orders/EventHandlers/Integration/` for the following — must return no results:
   - `ISender`
   - `using MediatR;`

3. **Grep check**: Search `src/Services/Ordering/Ordering.Application/Orders/EventHandlers/Integration/BasketCheckoutEventHandler.cs` for the following — must return 1 result each:
   - `using Wolverine;`
   - `IMessageBus bus`
   - `bus.InvokeAsync`

4. **Integration smoke test** (run against `docker-compose up` with Basket.API and Ordering.API):
   - Publish a `BasketCheckoutEvent` to RabbitMQ (via Basket.API `POST /basket/checkout` or directly via RabbitMQ admin)
   - Verify the `BasketCheckoutEventHandler` receives the event (check logs: "Integration Event: BasketCheckoutEvent - {Id}")
   - Verify a new order is created in the Ordering database with the event's customer details
   - Verify no MediatR references appear in the Ordering.Application logs

5. **Health check**: `GET /health` on Ordering.API → `200 OK` with SQL Server health check passing
