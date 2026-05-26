# Ordering.Application — BasketCheckoutEventHandler Wolverine Modernization

## 1. Feature Summary

The `BasketCheckoutEventHandler` integration event handler was added after the initial Ordering module modernization spec (`20260525-orderingModuleWolverineModernization.md`). It follows the original .NET 8 course pattern: MassTransit's `IConsumer<BasketCheckoutEvent>` for receiving integration events from RabbitMQ, but MediatR's `ISender` for dispatching the internal `CreateOrderCommand`. This creates a hybrid architecture where inter-service messaging (MassTransit) and in-process command dispatch (MediatR) coexist unnecessarily, and it reintroduces MediatR as a runtime dependency after the first spec removed it.

This spec modernizes the handler to match the pattern established in the Basket.API CheckoutBasket implementation (`20260526-basketApiCheckoutWolverineModernization.md`): MassTransit stays for integration event consumption (it is the RabbitMQ consumer mechanism and is a different concern from Wolverine's in-process messaging), but MediatR's `ISender` is replaced with Wolverine's `IMessageBus` for internal command dispatch. The `AddMessageBroker()` registration in `DepedencyInjection.cs` and the `BuildingBlocks.Messaging` project are explicitly kept — they are already wired correctly.

---

## 2. Data Model / Entities

### BasketCheckoutEvent *(unchanged, lives in BuildingBlocks.Messaging)*
- `UserName`: `string`
- `CustomerId`: `Guid`
- `TotalPrice`: `decimal`
- `FirstName`, `LastName`, `EmailAddress`, `AddressLine`, `Country`, `State`, `ZipCode`: `string`
- `CardName`, `CardNumber`, `Expiration`, `CVV`: `string`
- `PaymentMethod`: `int`
- Extends `IntegrationEvent` (`Id`, `OccurredOn`, `EventType`)

### CreateOrderCommand *(unchanged, from Ordering.Application)*
- `Order`: `OrderDto`
- Result: `CreateOrderResult(Guid Id)`

### OrderDto *(unchanged)*
- `Id`: `Guid`
- `CustomerId`: `Guid`
- `OrderName`: `string`
- `ShippingAddress`: `AddressDto`
- `BillingAddress`: `AddressDto`
- `Payment`: `PaymentDto`
- `Status`: `OrderStatus`
- `OrderItems`: `List<OrderItemDto>`

---

## 3. Business Rules & Constraints

### Rule 1 — MassTransit's `IConsumer<BasketCheckoutEvent>` must remain

**Why it stays**: MassTransit's `IConsumer<T>` interface is the mechanism for consuming integration events from RabbitMQ. Wolverine handles in-process command dispatch; MassTransit handles inter-service integration events. They serve different layers and can coexist. Removing MassTransit from the handler would require reimplementing the RabbitMQ consumer via Wolverine's transport — that is outside the scope of this modernization.

The class signature `BasketCheckoutEventHandler : IConsumer<BasketCheckoutEvent>` is unchanged. The `ConsumeContext<BasketCheckoutEvent> context` parameter and the `Consume` method signature are unchanged.

### Rule 2 — MediatR's `ISender` must be replaced with Wolverine's `IMessageBus`

**Old approach**: The handler constructor injects `ISender sender` (MediatR) and calls `await sender.Send(command)` to dispatch the `CreateOrderCommand`.

**New approach**: The handler constructor injects `IMessageBus bus` (Wolverine) and calls `await bus.InvokeAsync(command)` to dispatch the `CreateOrderCommand`. Wolverine's FluentValidation middleware automatically applies `CreateOrderCommandValidator` before the handler runs (this is already configured in the first spec's Wolverine setup).

### Rule 3 — The `using MediatR;` directive must be removed

After replacing `ISender` with `IMessageBus`, the handler no longer references any MediatR types. The `using MediatR;` directive is removed. The `using Wolverine;` directive is added.

### Rule 4 — The `MapToCreateOrderCommand` mapping method is unchanged

The private static method `MapToCreateOrderCommand(BasketCheckoutEvent message)` that constructs the `CreateOrderCommand` from the event fields is unchanged. The temporary two-item order initialization logic is retained as-is.

### Rule 5 — MassTransit and `AddMessageBroker()` registration are unchanged

The `AddMessageBroker(configuration, Assembly.GetExecutingAssembly())` call in `DepedencyInjection.cs` scans the Application assembly for `IConsumer<T>` implementations and registers them with MassTransit. This registration is unchanged and must remain — it is the mechanism by which `BasketCheckoutEventHandler` is discovered and wired to the RabbitMQ queue.

The `BuildingBlocks.Messaging.MassTransit` namespace and the `BuildingBlocks.Messaging` project are unchanged.

### Rule 6 — The logging statement is unchanged

The `logger.LogInformation("Integration Event: {EventName} - {Id}", ...)` call is unchanged. The `ILogger<BasketCheckoutEventHandler>` constructor parameter is unchanged.

---

## 4. Acceptance Criteria

The feature is complete when:

- [ ] `BasketCheckoutEventHandler` class still implements `IConsumer<BasketCheckoutEvent>`
- [ ] The constructor parameter changes from `ISender sender` to `IMessageBus bus`
- [ ] The dispatch call changes from `await sender.Send(command)` to `await bus.InvokeAsync(command)`
- [ ] `using MediatR;` is removed from `BasketCheckoutEventHandler.cs`
- [ ] `using Wolverine;` is added to `BasketCheckoutEventHandler.cs`
- [ ] The `MapToCreateOrderCommand` private method is unchanged
- [ ] The `Consume` method signature (`ConsumeContext<BasketCheckoutEvent> context`) is unchanged
- [ ] The logging statement is unchanged
- [ ] `AddMessageBroker(configuration, Assembly.GetExecutingAssembly())` in `DepedencyInjection.cs` is unchanged
- [ ] `dotnet build src/eshop-microservies.slnx` succeeds with 0 errors
- [ ] No file in `Ordering.Application/Orders/EventHandlers/Integration/` references `ISender` or `using MediatR;`
- [ ] Publishing a `BasketCheckoutEvent` to RabbitMQ triggers the handler, which dispatches `CreateOrderCommand` via Wolverine, creates an order, and logs the integration event

---

## 5. Out of Scope

- Changes to `BasketCheckoutEvent` structure or the `BuildingBlocks.Messaging` project
- Changes to `BuildingBlocks.Messaging.MassTransit.Extensions` (`AddMessageBroker`)
- Replacing MassTransit with Wolverine's messaging transport for RabbitMQ consumption
- Changes to the `CreateOrderCommand`, `CreateOrderCommandHandler`, or `CreateOrderCommandValidator` — covered by the first spec
- Changes to the `MapToCreateOrderCommand` mapping logic or the temporary two-item order initialization
- Changes to `DepedencyInjection.cs` beyond the `ISender` → `IMessageBus` replacement (no changes needed — `AddMessageBroker` stays, `AddMediatR` is already removed per the first spec)
- Changes to any other Ordering.Application files
- Adding OpenTelemetry or Serilog
- Authentication or authorization
- Health check changes

---

## 6. Open Questions

*(None — the pattern is established by the Basket.API CheckoutBasket spec and the first Ordering modernization spec.)*

---

## Cross-Module Consistency Note

This spec follows the same pattern established by:

1. **Basket.API CheckoutBasket** (`20260526-basketApiCheckoutWolverineModernization.md`): MassTransit stays for integration event *publishing* (`IPublishEndpoint`), MediatR is replaced with Wolverine for in-process command dispatch
2. **Ordering First Spec** (`20260525-orderingModuleWolverineModernization.md`): Wolverine replaces MediatR for command handlers, domain event handlers, and endpoint dispatch

The `BasketCheckoutEventHandler` is the mirror operation: it *consumes* a MassTransit integration event (instead of publishing one) and then uses Wolverine (instead of MediatR) to dispatch the internal command. The two messaging systems operate at different layers (inter-service vs. in-process) and do not conflict.
