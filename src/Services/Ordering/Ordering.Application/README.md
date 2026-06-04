# Ordering.Application

Application layer for the Ordering service. This layer now follows Wolverine handler conventions and direct DI for read-side query services.

## Target Framework & Dependencies

- .NET 10.0
- Microsoft.EntityFrameworkCore 10.0.8 (abstractions only)
- Project references: BuildingBlocks, BuildingBlocks.Messaging, Ordering.Core

## Project Structure

```
Ordering.Application/
├── DepedencyInjection.cs
├── Data/
│   └── IApplicationDbContext.cs
├── DTOs/
├── Extensions/
│   └── OrderExtensions.cs
└── Orders/
      ├── Commands/
      │   ├── CreateOrder/
      │   ├── DeleteOrder/
      │   └── UpdateOrder/
      ├── EventHandlers/
      │   ├── Domain/
      │   └── Integration/
      └── Queries/
            ├── GetOrders/
            ├── GetOrderByCustomer/
            └── GetOrderByName/
```

## Registration

AddApplicationServices now registers:

- FluentValidation validators from this assembly.
- Query services for direct endpoint injection:
- GetOrdersHandler
- GetOrderByCustomerHandler
- GetOrdersByNameHandler
- Feature management.
- MassTransit broker integration via AddMessageBroker.

## Command Handlers

Commands are plain records and handlers use HandleAsync methods discovered by Wolverine.

### CreateOrder

- Input: CreateOrderCommand(OrderDto Order)
- Output: CreateOrderResult(Guid Id)
- Validation: non-empty order name, valid customer, at least one order item.

### UpdateOrder

- Input: UpdateOrderCommand(OrderDto Order)
- Output: UpdateOrderCommandResult discriminated hierarchy:
- UpdateOrderResult(bool IsSuccess)
- UpdateOrderNotFound
- Behavior: no not-found exception throw; not-found is modeled as return type.

### DeleteOrder

- Input: DeleteOrderCommand(Guid OrderId)
- Output: DeleteOrderCommandResult discriminated hierarchy:
- DeleteOrderResult(bool IsSuccess)
- DeleteOrderNotFound
- Behavior: no not-found exception throw; not-found is modeled as return type.

## Query Handlers

The previous query message-carrier records were removed. Query handlers are injectable services that accept primitive parameters.

- GetOrdersHandler.HandleAsync(PaginationRequest)
- GetOrderByCustomerHandler.HandleAsync(Guid customerId)
- GetOrdersByNameHandler.HandleAsync(string name)

Each query handler keeps response contract records alongside implementation:

- GetOrdersResult
- GetOrderByCustomerResult
- GetOrdersByNameResult

## Domain and Integration Events

### Domain event handlers

Domain handlers no longer implement MediatR interfaces and are discovered by Wolverine convention:

- OrderCreatedEventHandler
- OrderUpdatedEventHandler

### Integration event handler

BasketCheckoutEventHandler remains an IConsumer<BasketCheckoutEvent> and now dispatches CreateOrderCommand through Wolverine IMessageBus.

## Persistence Boundary

IApplicationDbContext remains the persistence abstraction consumed by command and query handlers.

## Request and Event Flow

```
API endpoint
  -> IMessageBus.InvokeAsync(command)
        -> FluentValidation middleware
              -> command handler (HandleAsync)
                    -> SaveChangesAsync
                          -> Infrastructure interceptors
                                -> DispatchDomainEventsInterceptor
                                      -> IMessageBus.PublishAsync(domainEvent)

API query endpoint
  -> direct DI query handler
        -> HandleAsync(...)
```

## Related Implementation Files

- [DepedencyInjection.cs](DepedencyInjection.cs)
- [Orders/Commands/CreateOrder/CreateOrderCommand.cs](Orders/Commands/CreateOrder/CreateOrderCommand.cs)
- [Orders/Commands/CreateOrder/CreateOrderHandler.cs](Orders/Commands/CreateOrder/CreateOrderHandler.cs)
- [Orders/Commands/UpdateOrder/UpdateOrderCommand.cs](Orders/Commands/UpdateOrder/UpdateOrderCommand.cs)
- [Orders/Commands/UpdateOrder/UpdateOrderHandler.cs](Orders/Commands/UpdateOrder/UpdateOrderHandler.cs)
- [Orders/Commands/DeleteOrder/DeleteOrderCommand.cs](Orders/Commands/DeleteOrder/DeleteOrderCommand.cs)
- [Orders/Commands/DeleteOrder/DeleteOrderHandler.cs](Orders/Commands/DeleteOrder/DeleteOrderHandler.cs)
- [Orders/Queries/GetOrders/GetOrdersHandler.cs](Orders/Queries/GetOrders/GetOrdersHandler.cs)
- [Orders/Queries/GetOrderByCustomer/GetOrderByCustomerHandler.cs](Orders/Queries/GetOrderByCustomer/GetOrderByCustomerHandler.cs)
- [Orders/Queries/GetOrderByName/GetOrdersByNameHandler.cs](Orders/Queries/GetOrderByName/GetOrdersByNameHandler.cs)
- [Orders/EventHandlers/Domain/OrderCreatedEventHandler.cs](Orders/EventHandlers/Domain/OrderCreatedEventHandler.cs)
- [Orders/EventHandlers/Domain/OrderUpdatedEventHandler.cs](Orders/EventHandlers/Domain/OrderUpdatedEventHandler.cs)
- [Orders/EventHandlers/Integration/BasketCheckoutEventHandler.cs](Orders/EventHandlers/Integration/BasketCheckoutEventHandler.cs)
