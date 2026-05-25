# Implementation Plan: Ordering Module — Wolverine & Native Minimal API Modernization

> Spec: [`docs/specs/20260525-orderingModuleWolverineModernization.md`](../specs/20260525-orderingModuleWolverineModernization.md)

**TL;DR** — 5 phases, lowest-risk first: (1) delete dead query record files + `OrderNotFoundException` + update `.csproj` references; (2) strip MediatR from the domain core's `IDomainEvent`; (3) transform all Application-layer handlers — command handler steps (3.1–3.6) and query handler steps (3.7–3.9) run parallel to Phase 2 since they don't touch `IDomainEvent`; domain-event handler steps (3.10–3.11) and the DI update (3.12) run after Phase 2; (4) swap `IMediator` for `IMessageBus` in the EF Core interceptor; (5) rewrite all 6 endpoint files to static classes, create the `MapOrdersEndpoints` aggregator, and wire Wolverine into the host.

---

## Relevant Files

| Action | File |
|--------|------|
| DELETE | `src/Services/Ordering/Ordering.Application/Orders/Queries/GetOrders/GetOrdersQuery.cs` |
| DELETE | `src/Services/Ordering/Ordering.Application/Orders/Queries/GetOrderByCustomer/GetOrderByCustomerQuery.cs` |
| DELETE | `src/Services/Ordering/Ordering.Application/Orders/Queries/GetOrderByName/GetOrdersByNameQuery.cs` |
| DELETE | `src/Services/Ordering/Ordering.Application/Exceptions/OrderNotFoundException.cs` |
| MODIFY | `src/Services/Ordering/Ordering.Core/Ordering.Core.csproj` |
| MODIFY | `src/Services/Ordering/Ordering.Core/Abstractions/IDomainEvent.cs` |
| MODIFY | `src/Services/Ordering/Ordering.Infrastructure/Ordering.Infrastructure.csproj` |
| MODIFY | `src/Services/Ordering/Ordering.Infrastructure/Data/Interceptors/DispatchDomainEventsInterceptor.cs` |
| MODIFY | `src/Services/Ordering/Ordering.API/Ordering.API.csproj` |
| MODIFY | `src/Services/Ordering/Ordering.Application/Orders/Commands/CreateOrder/CreateOrderCommand.cs` |
| MODIFY | `src/Services/Ordering/Ordering.Application/Orders/Commands/CreateOrder/CreateOrderHandler.cs` |
| MODIFY | `src/Services/Ordering/Ordering.Application/Orders/Commands/UpdateOrder/UpdateOrderCommand.cs` |
| MODIFY | `src/Services/Ordering/Ordering.Application/Orders/Commands/UpdateOrder/UpdateOrderHandler.cs` |
| MODIFY | `src/Services/Ordering/Ordering.Application/Orders/Commands/DeleteOrder/DeleteOrderCommand.cs` |
| MODIFY | `src/Services/Ordering/Ordering.Application/Orders/Commands/DeleteOrder/DeleteOrderHandler.cs` |
| MODIFY | `src/Services/Ordering/Ordering.Application/Orders/Queries/GetOrders/GetOrdersHandler.cs` |
| MODIFY | `src/Services/Ordering/Ordering.Application/Orders/Queries/GetOrderByCustomer/GetOrderByCustomerHandler.cs` |
| MODIFY | `src/Services/Ordering/Ordering.Application/Orders/Queries/GetOrderByName/GetOrdersByNameHandler.cs` |
| MODIFY | `src/Services/Ordering/Ordering.Application/Orders/EventHandlers/Domain/OrderCreatedEventHandler.cs` |
| MODIFY | `src/Services/Ordering/Ordering.Application/Orders/EventHandlers/Domain/OrderUpdatedEventHandler.cs` |
| MODIFY | `src/Services/Ordering/Ordering.Application/DepedencyInjection.cs` |
| MODIFY | `src/Services/Ordering/Ordering.API/Endpoints/CreateOrder.cs` |
| MODIFY | `src/Services/Ordering/Ordering.API/Endpoints/UpdateOrder.cs` |
| MODIFY | `src/Services/Ordering/Ordering.API/Endpoints/DeleteOrder.cs` |
| MODIFY | `src/Services/Ordering/Ordering.API/Endpoints/GetOrders.cs` |
| MODIFY | `src/Services/Ordering/Ordering.API/Endpoints/GetOrdersByCustomer.cs` |
| MODIFY | `src/Services/Ordering/Ordering.API/Endpoints/GetOrdersByName.cs` |
| MODIFY | `src/Services/Ordering/Ordering.API/DepedencyInjection.cs` |
| MODIFY | `src/Services/Ordering/Ordering.API/Program.cs` |
| CREATE | `src/Services/Ordering/Ordering.API/Endpoints/OrdersEndpoints.cs` |

---

## Phase 1 — Delete dead code & remove packages

All steps in this phase are **independent and can run in parallel**.

### 1.1 — Delete the three query record files

These files existed solely as MediatR message-carrier types. The result record types inside them (`GetOrdersResult`, `GetOrderByCustomerResult`, `GetOrdersByNameResult`) are **not** deleted — they move up into the handler file (they are already declared in the query files, but keep them alongside the handler).

> **Learning note**: In MediatR, a `record FooQuery : IQuery<FooResult>` was required to route the message. In the Wolverine + direct-injection approach, the handler takes primitive parameters directly — the carrier type adds no value.

- **DELETE**: `src/Services/Ordering/Ordering.Application/Orders/Queries/GetOrders/GetOrdersQuery.cs`
- **DELETE**: `src/Services/Ordering/Ordering.Application/Orders/Queries/GetOrderByCustomer/GetOrderByCustomerQuery.cs`
- **DELETE**: `src/Services/Ordering/Ordering.Application/Orders/Queries/GetOrderByName/GetOrdersByNameQuery.cs`

The result record types (`GetOrdersResult`, `GetOrderByCustomerResult`, `GetOrdersByNameResult`) must be moved into their corresponding handler files before the query files are deleted.

### 1.2 — Delete `OrderNotFoundException.cs`

The throw site in `UpdateOrderHandler` and `DeleteOrderHandler` is replaced by a discriminated result return in Phase 3. With all call sites gone, this file has no references.

- **DELETE**: `src/Services/Ordering/Ordering.Application/Exceptions/OrderNotFoundException.cs`

### 1.3 — Remove `MediatR` from `Ordering.Core.csproj`

`IDomainEvent : INotification` is the only reason this package exists in Core. After Phase 2 strips that inheritance, MediatR has no role in the domain layer.

**File**: `src/Services/Ordering/Ordering.Core/Ordering.Core.csproj`

- Remove: `<PackageReference Include="MediatR" Version="14.1.0" />`
- **Unchanged**: all other `PropertyGroup` and remaining `ItemGroup` contents

### 1.4 — Remove `Carter` from `Ordering.API.csproj`

Carter's `ICarterModule`, `AddCarter()`, and `app.MapCarter()` are all replaced by native `MapGroup` routing in Phase 5.

**File**: `src/Services/Ordering/Ordering.API/Ordering.API.csproj`

- Remove: `<PackageReference Include="Carter" Version="10.0.0" />`
- **Unchanged**: all other references

### 1.5 — Add `WolverineFx` directly to `Ordering.Infrastructure.csproj`

`DispatchDomainEventsInterceptor` will inject `IMessageBus` from `WolverineFx` in Phase 4. Although `WolverineFx` is transitively available through `Application → BuildingBlocks`, an explicit reference documents the dependency and avoids relying on transitive resolution for a runtime-visible type.

**File**: `src/Services/Ordering/Ordering.Infrastructure/Ordering.Infrastructure.csproj`

- Add inside the existing `<ItemGroup>` with package references:
  ```xml
  <PackageReference Include="WolverineFx" Version="5.39.1" />
  ```
- **Unchanged**: all EF Core references, `FrameworkReference`, project references

---

## Phase 2 — Decouple domain core from MediatR *(depends on Phase 1.3)*

### 2.1 — Strip `INotification` from `IDomainEvent`

> **Learning note**: Clean Architecture's golden rule is that the domain core has no external dependencies. `IDomainEvent : INotification` imported a third-party library into the innermost layer solely to satisfy MediatR's message-routing contract. Wolverine's handler discovery is convention-based — no marker interface is needed on the message type.

**File**: `src/Services/Ordering/Ordering.Core/Abstractions/IDomainEvent.cs`

- Remove: `using MediatR;`
- Strip: `: INotification` from the `IDomainEvent` interface declaration
- **Unchanged**: the three default-interface properties (`EventId`, `OcurredOn`, `EventType`) and their implementations

Full file after change:
```csharp
namespace Ordering.Core.Abstractions
{
    public interface IDomainEvent
    {
        Guid EventId => Guid.NewGuid();
        public DateTime OcurredOn => DateTime.UtcNow;
        public string EventType => GetType().AssemblyQualifiedName;
    }
}
```

---

## Phase 3 — Transform Application-layer handlers

> Steps 3.1–3.9 (command + query handlers) have **no dependency on Phase 2** — they do not reference `IDomainEvent`. They can run in parallel with Phase 2. Steps 3.10–3.11 (domain event handlers) **must come after Phase 2** because they implement `INotificationHandler<TEvent>` which becomes invalid once `IDomainEvent` drops `INotification`. Step 3.12 (DI wiring) must come after all other steps in this phase.

### 3.1 — `CreateOrderCommand.cs`: strip CQRS interface

Minimal change — only the interface inheritance is stripped. The validator class is untouched; Wolverine's `UseFluentValidation()` middleware discovers `AbstractValidator<CreateOrderCommand>` from DI automatically.

**File**: `src/Services/Ordering/Ordering.Application/Orders/Commands/CreateOrder/CreateOrderCommand.cs`

- Remove: `using BuildingBlocks.CQRS;`
- Strip: `: ICommand<CreateOrderResult>` from the `CreateOrderCommand` record declaration
- **Unchanged**: `CreateOrderResult` record, `CreateOrderCommandValidator` class and all its rules

### 3.2 — `CreateOrderHandler.cs`: strip CQRS interface, rename method

> **Learning note**: Wolverine discovers a handler by the convention that a class has a `Handle` or `HandleAsync` method whose first parameter is the message type. No base class or interface is needed. `HandleAsync` is preferred over `Handle` for async handlers.

**File**: `src/Services/Ordering/Ordering.Application/Orders/Commands/CreateOrder/CreateOrderHandler.cs`

- Remove: `using BuildingBlocks.CQRS;`
- Strip: `: ICommandHandler<CreateOrderCommand, CreateOrderResult>` from `CreateOrderHandler`
- Rename: `public async Task<CreateOrderResult> Handle(` → `public async Task<CreateOrderResult> HandleAsync(`
- **Unchanged**: constructor primary constructor `(IApplicationDbContext dbContext)`, entire `CreateNewOrder` private method, `dbContext.Orders.Add`, `SaveChangesAsync`, and the return statement

### 3.3 — `UpdateOrderCommand.cs`: strip CQRS interface, add discriminated result hierarchy

> **Learning note**: Instead of throwing `OrderNotFoundException` (an exception for a predictable outcome), the handler returns one of two sealed types. The endpoint layer pattern-matches the return value — HTTP concerns stay in the endpoint, domain logic stays in the handler.

**File**: `src/Services/Ordering/Ordering.Application/Orders/Commands/UpdateOrder/UpdateOrderCommand.cs`

- Remove: `using BuildingBlocks.CQRS;`
- Strip: `: ICommand<UpdateOrderResult>` from `UpdateOrderCommand`
- Add the discriminated result hierarchy **above** the validator:
  ```csharp
  public abstract record UpdateOrderCommandResult;
  public record UpdateOrderResult(bool IsSuccess) : UpdateOrderCommandResult;
  public sealed record UpdateOrderNotFound : UpdateOrderCommandResult;
  ```
- Remove: the standalone `public record UpdateOrderResult(bool IsSuccess);` line (it is now part of the hierarchy above)
- **Unchanged**: `UpdateOrderCommandValidator` and all its rules

### 3.4 — `UpdateOrderHandler.cs`: strip CQRS interface, discriminated return, remove exception throw

**File**: `src/Services/Ordering/Ordering.Application/Orders/Commands/UpdateOrder/UpdateOrderHandler.cs`

- Remove: `using BuildingBlocks.CQRS;`
- Remove: `using Ordering.Application.Exceptions;`
- Strip: `: ICommandHandler<UpdateOrderCommand, UpdateOrderResult>` from `UpdateOrderHandler`
- Change method signature:
  ```csharp
  // Before
  public async Task<UpdateOrderResult> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
  // After
  public async Task<UpdateOrderCommandResult> HandleAsync(UpdateOrderCommand request, CancellationToken cancellationToken)
  ```
- Replace the not-found throw:
  ```csharp
  // Before
  if (order == null)
  {
      throw new OrderNotFoundException(request.Order.Id);
  }
  // After
  if (order is null)
      return new UpdateOrderNotFound();
  ```
- **Unchanged**: constructor, `UpdateOrderWithNewValues` private method and all its Address/Payment/OrderName construction calls, `dbContext.Orders.Update`, `SaveChangesAsync`, and `return new UpdateOrderResult(true)`

### 3.5 — `DeleteOrderCommand.cs`: strip CQRS interface, add discriminated result hierarchy

**File**: `src/Services/Ordering/Ordering.Application/Orders/Commands/DeleteOrder/DeleteOrderCommand.cs`

- Remove: `using BuildingBlocks.CQRS;`
- Strip: `: ICommand<DeleteOrderResult>` from `DeleteOrderCommand`
- Add the discriminated result hierarchy **above** the validator:
  ```csharp
  public abstract record DeleteOrderCommandResult;
  public record DeleteOrderResult(bool IsSuccess) : DeleteOrderCommandResult;
  public sealed record DeleteOrderNotFound : DeleteOrderCommandResult;
  ```
- Remove: the standalone `public record DeleteOrderResult(bool IsSuccess);` line
- **Unchanged**: `DeleteOrderCommandValidator` and its rule

### 3.6 — `DeleteOrderHandler.cs`: strip CQRS interface, discriminated return, remove exception throw

**File**: `src/Services/Ordering/Ordering.Application/Orders/Commands/DeleteOrder/DeleteOrderHandler.cs`

- Remove: `using BuildingBlocks.CQRS;`
- Remove: `using Ordering.Application.Exceptions;`
- Strip: `: ICommandHandler<DeleteOrderCommand, DeleteOrderResult>` from `DeleteOrderHandler`
- Change method signature:
  ```csharp
  // Before
  public async Task<DeleteOrderResult> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
  // After
  public async Task<DeleteOrderCommandResult> HandleAsync(DeleteOrderCommand request, CancellationToken cancellationToken)
  ```
- Replace the not-found throw:
  ```csharp
  // Before
  if (order == null)
  {
      throw new OrderNotFoundException(request.OrderId);
  }
  // After
  if (order is null)
      return new DeleteOrderNotFound();
  ```
- **Unchanged**: constructor, `OrderId.Of(request.OrderId)`, `FindAsync`, `dbContext.Orders.Remove`, `SaveChangesAsync`, `return new DeleteOrderResult(true)`

### 3.7 — `GetOrdersHandler.cs`: strip CQRS interface, change to injectable service

> **Learning note**: A query handler with no business logic beyond an EF Core query chain doesn't need message-routing infrastructure. Registering the class in DI and injecting it directly into the endpoint is simpler, type-safe, and keeps the Application layer as the boundary for data access logic.

**File**: `src/Services/Ordering/Ordering.Application/Orders/Queries/GetOrders/GetOrdersHandler.cs`

- Remove: `using BuildingBlocks.CQRS;` 
- Move the result record here (it was in `GetOrdersQuery.cs` which is deleted): add `public record GetOrdersResult(PaginatedResult<OrderDto> Orders);` at the top of the file, after the `using` statements
- Strip: `: IQueryHandler<GetOrdersQuery, GetOrdersResult>` from `GetOrdersHandler`
- Change method signature to accept primitive parameters:
  ```csharp
  // Before
  public async Task<GetOrdersResult> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
  {
      var pageIndex = request.PaginationRequest.PageIndex;
      var pageSize = request.PaginationRequest.PageSize;
  // After
  public async Task<GetOrdersResult> HandleAsync(PaginationRequest paginationRequest, CancellationToken cancellationToken = default)
  {
      var pageIndex = paginationRequest.PageIndex;
      var pageSize = paginationRequest.PageSize;
  ```
- **Unchanged**: `totalCount` query, the Orders EF Core query chain (`Include`, `OrderBy`, `Skip`, `Take`, `ToListAsync`), `ToOrderDtoList()` extension call, and the `PaginatedResult<OrderDto>` construction

### 3.8 — `GetOrderByCustomerHandler.cs`: strip CQRS interface, change to injectable service

**File**: `src/Services/Ordering/Ordering.Application/Orders/Queries/GetOrderByCustomer/GetOrderByCustomerHandler.cs`

- Remove: `using BuildingBlocks.CQRS;`
- Move the result record here (it was in `GetOrderByCustomerQuery.cs`): add `public record GetOrderByCustomerResult(IEnumerable<OrderDto> Orders);`
- Strip: `: IQueryHandler<GetOrderByCustomerQuery, GetOrderByCustomerResult>` from `GetOrderByCustomerHandler`
- Change method signature:
  ```csharp
  // Before
  public async Task<GetOrderByCustomerResult> Handle(GetOrderByCustomerQuery request, CancellationToken cancellationToken)
  {
      var orders = await dbContext.Orders
          ...
          .Where(o => o.CustomerId == CustomerId.Of(request.CustomerId))
  // After
  public async Task<GetOrderByCustomerResult> HandleAsync(Guid customerId, CancellationToken cancellationToken = default)
  {
      var orders = await dbContext.Orders
          ...
          .Where(o => o.CustomerId == CustomerId.Of(customerId))
  ```
- **Unchanged**: `Include(o => o.OrderItems)`, `AsNoTracking()`, `OrderBy(o => o.OrderName.Value)`, `ToListAsync`, `ToOrderDtoList()`, and the result return

### 3.9 — `GetOrdersByNameHandler.cs`: strip CQRS interface, change to injectable service

**File**: `src/Services/Ordering/Ordering.Application/Orders/Queries/GetOrderByName/GetOrdersByNameHandler.cs`

- Remove: `using BuildingBlocks.CQRS;`
- Move the result record here (it was in `GetOrdersByNameQuery.cs`): add `public record GetOrdersByNameResult(IEnumerable<OrderDto> Orders);`
- Strip: `: IQueryHandler<GetOrdersByNameQuery, GetOrdersByNameResult>` from `GetOrdersByNameHandler`
- Change method signature:
  ```csharp
  // Before
  public async Task<GetOrdersByNameResult> Handle(GetOrdersByNameQuery request, CancellationToken cancellationToken)
  {
      var orders = await dbContext.Orders
          ...
          .Where(o => o.OrderName.Value.Contains(request.Name))
  // After
  public async Task<GetOrdersByNameResult> HandleAsync(string name, CancellationToken cancellationToken = default)
  {
      var orders = await dbContext.Orders
          ...
          .Where(o => o.OrderName.Value.Contains(name))
  ```
- **Unchanged**: `Include(o => o.OrderItems)`, `AsNoTracking()`, `OrderBy`, `ToListAsync`, `ToOrderDtoList()`, and the result return

### 3.10 — `OrderCreatedEventHandler.cs`: strip `INotificationHandler` *(depends on Phase 2)*

> **Learning note**: Wolverine discovers domain event handlers by the same convention as command handlers — a class with a `Handle(TEvent, CancellationToken)` method. The event type doesn't need to implement any interface. Wolverine matches by the first parameter type.

**File**: `src/Services/Ordering/Ordering.Application/Orders/EventHandlers/Domain/OrderCreatedEventHandler.cs`

- Remove: `using MediatR;`
- Strip: `: INotificationHandler<OrderCreatedEvent>` from `OrderCreatedEventHandler`
- **Unchanged**: constructor `(ILogger<OrderCreatedEventHandler> logger)`, `Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)` method signature and its body (the log statement stays identical)

### 3.11 — `OrderUpdatedEventHandler.cs`: strip `INotificationHandler` *(depends on Phase 2)*

**File**: `src/Services/Ordering/Ordering.Application/Orders/EventHandlers/Domain/OrderUpdatedEventHandler.cs`

- Remove: `using MediatR;`
- Strip: `: INotificationHandler<OrderUpdatedEvent>` from `OrderUpdatedEventHandler`
- **Unchanged**: constructor, `Handle` method signature and body

### 3.12 — `Ordering.Application/DepedencyInjection.cs`: remove AddMediatR, register query services *(depends on 3.7–3.9)*

**File**: `src/Services/Ordering/Ordering.Application/DepedencyInjection.cs`

- Remove: `using BuildingBlocks.Behaviors;`
- Remove: `using System.Reflection;` only if no longer needed — **keep it** because `AddValidatorsFromAssembly` still needs it
- Remove: the entire `services.AddMediatR(cfg => { ... })` block
- Add: `services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());` — registers all `AbstractValidator<T>` implementations so Wolverine's `UseFluentValidation()` can pick them up from DI
- Add: the three query handler scoped registrations:
  ```csharp
  services.AddScoped<GetOrdersHandler>();
  services.AddScoped<GetOrderByCustomerHandler>();
  services.AddScoped<GetOrdersByNameHandler>();
  ```

Full file after change:
```csharp
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Application.Orders.Queries.GetOrderByCustomer;
using Ordering.Application.Orders.Queries.GetOrderByName;
using Ordering.Application.Orders.Queries.GetOrders;
using System.Reflection;

namespace Ordering.Application
{
    public static class DepedencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            services.AddScoped<GetOrdersHandler>();
            services.AddScoped<GetOrderByCustomerHandler>();
            services.AddScoped<GetOrdersByNameHandler>();

            return services;
        }
    }
}
```

---

## Phase 4 — Update Infrastructure layer *(depends on Phase 2)*

### 4.1 — `DispatchDomainEventsInterceptor.cs`: replace `IMediator` with `IMessageBus`

> **Learning note**: `IMediator.Publish` and `IMessageBus.PublishAsync` are conceptually identical for in-process domain event dispatch — both locate any handler registered for the event type and invoke it. The difference is the infrastructure: MediatR uses a runtime reflection chain; Wolverine uses source-generated, compile-time-resolved message routing.

**File**: `src/Services/Ordering/Ordering.Infrastructure/Data/Interceptors/DispatchDomainEventsInterceptor.cs`

- Remove: `using MediatR;`
- Add: `using Wolverine;`
- Change constructor parameter: `IMediator mediator` → `IMessageBus bus`
- Change the publish call inside `DispatchDomainEvents`:
  ```csharp
  // Before
  foreach (var domainEvent in domainEvents)
  {
      await mediator.Publish(domainEvent);
  }
  // After
  foreach (var domainEvent in domainEvents)
  {
      await bus.PublishAsync(domainEvent);
  }
  ```
- **Unchanged**: the two `SavingChanges` / `SavingChangesAsync` overrides, the `DispatchDomainEvents` private method structure (`ChangeTracker.Entries<IAggregate>()`, `ClearDomainEvents()` loop, the aggregate-collection logic)

---

## Phase 5 — Rewrite API endpoints and wire host *(depends on Phases 3 + 4)*

**Pattern for every endpoint file**: the `ICarterModule` class becomes a `static` class; `AddRoutes(IEndpointRouteBuilder app)` is replaced by a `Map{Name}(this RouteGroupBuilder group)` static extension method; route paths drop the `/orders` prefix (it moves to the `MapGroup` call in `OrdersEndpoints.cs`).

### 5.1 — `GetOrders.cs`: Carter → static class, inject `GetOrdersHandler`

**File**: `src/Services/Ordering/Ordering.API/Endpoints/GetOrders.cs`

- Remove: `using Carter;`, `using MediatR;`, `using Ordering.Application.Orders.Queries.GetOrders;`
- Add: `using Ordering.Application.Orders.Queries.GetOrders;` (keep — needed for `GetOrdersHandler` and result types)
- Convert class:
  ```csharp
  // Before
  public class GetOrders : ICarterModule
  {
      public void AddRoutes(IEndpointRouteBuilder app)
      {
          app.MapGet("/orders", async ([AsParameters] PaginationRequest request, ISender sender) =>
          {
              var result = await sender.Send(new GetOrdersQuery(request));
              var response = result.Adapt<GetOrdersResponse>();
              return Results.Ok(response);
          })
  // After
  public static class GetOrdersEndpoint
  {
      public static RouteGroupBuilder MapGetOrders(this RouteGroupBuilder group)
      {
          group.MapGet("", async ([AsParameters] PaginationRequest request, GetOrdersHandler handler) =>
          {
              var result = await handler.HandleAsync(request);
              var response = result.Adapt<GetOrdersResponse>();
              return Results.Ok(response);
          })
  ```
- Return `group` at end of method
- **Unchanged**: `GetOrdersResponse` record, `.WithName`, `.Produces<>`, `.WithSummary`, `.WithDescription` calls

### 5.2 — `GetOrdersByName.cs`: Carter → static class, inject `GetOrdersByNameHandler`

**File**: `src/Services/Ordering/Ordering.API/Endpoints/GetOrdersByName.cs`

- Remove: `using Carter;`, `using MediatR;`
- Convert class and method; adjust route path from `"/orders/{orderName}"` → `"/{orderName}"`:
  ```csharp
  public static class GetOrdersByNameEndpoint
  {
      public static RouteGroupBuilder MapGetOrdersByName(this RouteGroupBuilder group)
      {
          group.MapGet("/{orderName}", async (string orderName, GetOrdersByNameHandler handler) =>
          {
              var result = await handler.HandleAsync(orderName);
              var response = result.Adapt<GetOrdersByNameResponse>();
              return Results.Ok(response);
          })
  ```
- Return `group`
- **Unchanged**: `GetOrdersByNameResponse` record, `.WithName`, `.Produces<>`, `.WithSummary`, `.WithDescription`

### 5.3 — `GetOrdersByCustomer.cs`: Carter → static class, inject `GetOrderByCustomerHandler`

**File**: `src/Services/Ordering/Ordering.API/Endpoints/GetOrdersByCustomer.cs`

- Remove: `using Carter;`, `using MediatR;`
- Convert class and method; adjust route from `"/orders/customer/{customerId}"` → `"/customer/{customerId}"`:
  ```csharp
  public static class GetOrdersByCustomerEndpoint
  {
      public static RouteGroupBuilder MapGetOrdersByCustomer(this RouteGroupBuilder group)
      {
          group.MapGet("/customer/{customerId}", async (Guid customerId, GetOrderByCustomerHandler handler) =>
          {
              var result = await handler.HandleAsync(customerId);
              var response = result.Adapt<GetOrdersByCustomerResponse>();
              return Results.Ok(response);
          })
  ```
- Return `group`
- **Unchanged**: `GetOrdersByCustomerResponse` record, `.WithName`, `.Produces<>`, `.WithSummary`, `.WithDescription`

### 5.4 — `CreateOrder.cs`: Carter → static class, `ISender` → `IMessageBus`

> **Learning note**: `bus.InvokeAsync<TResult>(command)` is Wolverine's equivalent of `sender.Send(command)`. It routes the message to the handler, runs FluentValidation middleware first, and returns the result — all in-process.

**File**: `src/Services/Ordering/Ordering.API/Endpoints/CreateOrder.cs`

- Remove: `using Carter;`, `using MediatR;`
- Add: `using Wolverine;`
- Convert class and method; adjust route from `"/orders"` → `""`:
  ```csharp
  public static class CreateOrderEndpoint
  {
      public static RouteGroupBuilder MapCreateOrder(this RouteGroupBuilder group)
      {
          group.MapPost("", async (CreateOrderRequest request, IMessageBus bus) =>
          {
              var command = request.Adapt<CreateOrderCommand>();
              var result = await bus.InvokeAsync<CreateOrderResult>(command);
              var response = result.Adapt<CreateOrderResponse>();
              return Results.Created($"/orders/{response.Id}", response);
          })
  ```
- Return `group`
- **Unchanged**: `CreateOrderRequest` record, `CreateOrderResponse` record, `.WithName`, `.Produces<>`, `.ProducesProblem`, `.WithSummary`, `.WithDescription`, and the `Results.Created` call

### 5.5 — `UpdateOrder.cs`: Carter → static class, `ISender` → `IMessageBus`, pattern-match discriminated result

**File**: `src/Services/Ordering/Ordering.API/Endpoints/UpdateOrder.cs`

- Remove: `using Carter;`, `using MediatR;`
- Add: `using Wolverine;`
- Convert class, method, and route from `"/orders"` → `""`:
  ```csharp
  public static class UpdateOrderEndpoint
  {
      public static RouteGroupBuilder MapUpdateOrder(this RouteGroupBuilder group)
      {
          group.MapPut("", async (UpdateOrderRequest request, IMessageBus bus) =>
          {
              var command = request.Adapt<UpdateOrderCommand>();
              var result = await bus.InvokeAsync<UpdateOrderCommandResult>(command);

              return result switch
              {
                  UpdateOrderResult r    => Results.Ok(new UpdateOrderResponse(r.IsSuccess)),
                  UpdateOrderNotFound    => Results.NotFound(),
                  _                      => Results.StatusCode(500)
              };
          })
          .WithName("UpdateOrder")
          .Produces<UpdateOrderResponse>(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status404NotFound)
          .ProducesProblem(StatusCodes.Status400BadRequest)
          .WithSummary("Updates an existing order.")
          .WithDescription("Updates an existing order with the specified details.");

          return group;
      }
  }
  ```
- **Unchanged**: `UpdateOrderRequest` record, `UpdateOrderResponse` record

### 5.6 — `DeleteOrder.cs`: Carter → static class, `ISender` → `IMessageBus`, pattern-match discriminated result

**File**: `src/Services/Ordering/Ordering.API/Endpoints/DeleteOrder.cs`

- Remove: `using Carter;`, `using MediatR;`
- Add: `using Wolverine;`
- Convert class, method, and route from `"/orders/{id}"` → `"/{id}"`:
  ```csharp
  public static class DeleteOrderEndpoint
  {
      public static RouteGroupBuilder MapDeleteOrder(this RouteGroupBuilder group)
      {
          group.MapDelete("/{id}", async (Guid id, IMessageBus bus) =>
          {
              var result = await bus.InvokeAsync<DeleteOrderCommandResult>(new DeleteOrderCommand(id));

              return result switch
              {
                  DeleteOrderResult r    => Results.Ok(new DeleteOrderResponse(r.IsSuccess)),
                  DeleteOrderNotFound    => Results.NotFound(),
                  _                      => Results.StatusCode(500)
              };
          })
          .WithName("DeleteOrder")
          .Produces<DeleteOrderResponse>(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status404NotFound)
          .ProducesProblem(StatusCodes.Status400BadRequest)
          .ProducesProblem(StatusCodes.Status404NotFound)
          .WithSummary("Deletes an order.")
          .WithDescription("Deletes an order with the specified ID.");

          return group;
      }
  }
  ```
- **Unchanged**: `DeleteOrderResponse` record

### 5.7 — CREATE `OrdersEndpoints.cs`: central `MapOrdersEndpoints` aggregator

> **Learning note**: `app.MapGroup("/orders")` creates a `RouteGroupBuilder` that automatically prepends `/orders` to every route registered on it. This replaces Carter's assembly-scan approach with an explicit, AOT-friendly registration. `MapOrdersEndpoints` is the single entry point that wires all 6 routes.

**File**: `src/Services/Ordering/Ordering.API/Endpoints/OrdersEndpoints.cs` *(new)*

```csharp
namespace Ordering.API.Endpoints
{
    public static class OrdersEndpoints
    {
        public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/orders")
                .WithTags("Orders");

            group.MapGetOrders();
            group.MapGetOrdersByName();
            group.MapGetOrdersByCustomer();
            group.MapCreateOrder();
            group.MapUpdateOrder();
            group.MapDeleteOrder();

            return app;
        }
    }
}
```

### 5.8 — `Ordering.API/DepedencyInjection.cs`: remove Carter, call `MapOrdersEndpoints`

**File**: `src/Services/Ordering/Ordering.API/DepedencyInjection.cs`

- Remove: `using Carter;`
- Remove: `services.AddCarter();` from `AddApiServices`
- Replace: `app.MapCarter();` → `app.MapOrdersEndpoints();` in `UseApiServices`
- **Unchanged**: `AddExceptionHandler<CustomExceptionHandler>()`, `AddHealthChecks().AddSqlServer(...)`, `app.UseExceptionHandler(opt => { })`, and the `app.UseHealthChecks(...)` block

### 5.9 — `Program.cs`: register Wolverine on the host

> **Learning note**: `builder.Host.UseWolverine` integrates Wolverine with the .NET Generic Host. This single call replaces both `AddMediatR` (command dispatch) and the MediatR pipeline behavior registrations (validation, logging). `UseFluentValidation()` tells Wolverine to apply any `IValidator<T>` found in DI as middleware before the matching handler. `IncludeAssembly` scopes handler discovery to `Ordering.Application` per the spec decision.

**File**: `src/Services/Ordering/Ordering.API/Program.cs`

- Add: `using Wolverine;` and `using Ordering.Application.Orders.Commands.CreateOrder;` (used only for the assembly reference)
- Add before `var app = builder.Build();`:
  ```csharp
  builder.Host.UseWolverine(opts =>
  {
      opts.UseFluentValidation();
      opts.Discovery.IncludeAssembly(typeof(CreateOrderHandler).Assembly);
  });
  ```
- **Unchanged**: `AddApplicationServices()`, `AddInfrastructureServices(...)`, `AddApiServices(...)`, `app.UseApiServices()`, `app.InitializeDatabaseAsync()`, `app.RunAsync()`

---

## Verification

1. **Build**: `dotnet build src/eshop-microservies.slnx` must succeed with **0 errors and 0 warnings** related to missing types.

2. **Grep check** — run from `src/Services/Ordering/`; each search must return **no results**:
   - `ICarterModule` — Carter endpoint base
   - `ISender` — MediatR command dispatcher
   - `IMediator` — MediatR mediator
   - `INotificationHandler` — MediatR event handler base
   - `ICommandHandler` — deleted BuildingBlocks CQRS interface
   - `IQueryHandler` — deleted BuildingBlocks CQRS interface
   - `ICommand` — deleted BuildingBlocks CQRS interface
   - `IQuery` — deleted BuildingBlocks CQRS interface
   - `OrderNotFoundException` — deleted typed exception
   - `using Carter` — removed package
   - `using BuildingBlocks.CQRS` — deleted namespace

3. **HTTP smoke tests** (run against `docker-compose up`):

   | Verb | Route | Scenario | Expected |
   |------|-------|----------|----------|
   | GET | `/orders` | default pagination | `200 OK` with `orders` array and pagination metadata |
   | GET | `/orders?pageIndex=0&pageSize=2` | explicit pagination | `200 OK` with at most 2 orders |
   | GET | `/orders/ORD_1` | existing order name | `200 OK` with matching orders |
   | GET | `/orders/DOES_NOT_EXIST` | no match | `200 OK` with empty `orders` array |
   | GET | `/orders/customer/00000000-0000-0000-0000-000000000001` | existing customer | `200 OK` with orders for John Doe |
   | POST | `/orders` | valid body with `orderName`, `customerId`, and `orderItems` | `201 Created` with `id` in body |
   | POST | `/orders` | body with empty `orderName` | `400 Bad Request` (Wolverine FluentValidation — no exception in logs) |
   | POST | `/orders` | body with empty `orderItems` array | `400 Bad Request` |
   | PUT | `/orders` | valid body with existing order `id` | `200 OK` with `isSuccess: true` |
   | PUT | `/orders` | valid body with non-existent `id` | `404 Not Found` (no exception in logs) |
   | DELETE | `/orders/{id}` | existing order `id` | `200 OK` with `isSuccess: true` |
   | DELETE | `/orders/{id}` | non-existent `id` | `404 Not Found` (no exception in logs) |
   | GET | `/health` | — | `200 OK` with SQL Server health check passing |
