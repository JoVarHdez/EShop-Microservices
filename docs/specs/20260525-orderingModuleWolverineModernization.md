# Ordering Module — Wolverine & Native Minimal API Modernization

## 1. Feature Summary

The Ordering module was built following a .NET 8 course that taught Clean Architecture across four projects (Core, Application, Infrastructure, API) with CQRS using MediatR, endpoint organization using Carter, and cross-cutting concerns via MediatR pipeline behaviors. While functionally sound, the module carries several .NET 8-era patterns that conflict with .NET 10 best practices: MediatR is embedded in the domain core (`IDomainEvent : INotification`), domain event handlers couple to MediatR's `INotificationHandler<T>`, query handlers wrap EF Core calls in unnecessary abstraction layers, and Carter replaces the now-native `MapGroup` routing.

Additionally, the `BuildingBlocks` CQRS wrapper interfaces (`ICommand`, `ICommandHandler`, `IQuery`, `IQueryHandler`) were already deleted as part of the Catalog API modernization — Ordering is the last module still referencing them, causing compile-time errors that block the build.

This modernization brings the Ordering module in line with the .NET 10 pattern established by the Catalog API: Wolverine replaces MediatR for command dispatch and domain event publishing, native `MapGroup` routing replaces Carter, query handlers are inlined into endpoints using `IApplicationDbContext` directly, and exception-driven HTTP responses are replaced with `TypedResults` and discriminated command results. The external API contract (all 6 endpoints, their URLs, verbs, and response shapes) remains identical — only the internal wiring changes.

---

## 2. Data Model / Entities

### Order *(unchanged)*
- `Id`: `OrderId` — strongly-typed value object wrapping `Guid`
- `CustomerId`: `CustomerId` — strongly-typed value object
- `OrderName`: `OrderName` — strongly-typed value object
- `ShippingAddress`: `Address` — owned value object
- `BillingAddress`: `Address` — owned value object
- `Payment`: `Payment` — owned value object
- `Status`: `OrderStatus` — enum (`Pending`, `Processing`, `Shipped`, `Delivered`, `Cancelled`)
- `OrderItems`: `IReadOnlyList<OrderItem>` — aggregate-owned collection
- `TotalAmount`: `decimal` — computed from order items, EF Core-mapped via private setter

### OrderItem *(unchanged)*
- `Id`: `OrderItemId` — strongly-typed value object
- `OrderId`: `OrderId` — foreign key back to Order
- `ProductId`: `ProductId` — strongly-typed value object
- `Quantity`: `int` — must be greater than zero
- `Price`: `decimal` — must be greater than zero

### Customer *(unchanged)*
- `Id`: `CustomerId` — strongly-typed value object
- `Name`: `string`
- `Email`: `string`

### Product *(unchanged)*
- `Id`: `ProductId` — strongly-typed value object
- `Name`: `string`
- `Price`: `decimal`

### Command Records *(shape unchanged, interface removed)*

The three write-side command records keep their current properties but drop the `: ICommand<TResult>` inheritance:

- `CreateOrderCommand(OrderDto Order)` → result: `CreateOrderResult(Guid Id)`
- `UpdateOrderCommand(OrderDto Order)` → result: discriminated union of `UpdateOrderResult(bool IsSuccess)` or `NotFound`
- `DeleteOrderCommand(Guid Id)` → result: discriminated union of `DeleteOrderResult(bool IsSuccess)` or `NotFound`

### Query Records *(deleted)*

The three query record types (`GetOrdersQuery`, `GetOrderByCustomerQuery`, `GetOrdersByNameQuery`) are deleted — they existed solely as MediatR message carrier types. Their handler classes are retained in the Application layer as plain injectable services (see Rule 6).

### Query Handler Services *(shape preserved, CQRS interface removed)*

- `GetOrdersHandler` — injectable service; method takes `(PaginationRequest, CancellationToken)` and returns `GetOrdersResult`
- `GetOrderByCustomerHandler` — injectable service; method takes `(Guid customerId, CancellationToken)` and returns `GetOrderByCustomerResult`
- `GetOrdersByNameHandler` — injectable service; method takes `(string name, CancellationToken)` and returns `GetOrdersByNameResult`

### Domain Events *(shape unchanged, MediatR dependency removed)*

- `OrderCreatedEvent(Order Order)` — raised by `Order.Create`; `IDomainEvent` no longer extends `INotification`
- `OrderUpdatedEvent(Order Order)` — raised by `Order.Update`; same

---

## 3. Business Rules & Constraints

> Each rule includes an explanation of how the old approach worked vs. the new approach, to support learning.

### Rule 1 — BuildingBlocks CQRS interface usages must be removed from Ordering

**Old approach — MediatR wrapper interfaces**: The Catalog API modernization deleted the four files in `BuildingBlocks/CQRS/` (`ICommand.cs`, `ICommandHandler.cs`, `IQuery.cs`, `IQueryHandler.cs`). The Ordering Application layer still references `BuildingBlocks.CQRS` across its command and query handlers, causing compile-time errors. Every `using BuildingBlocks.CQRS` statement, every `: ICommand<TResult>` and `: ICommandHandler<TCommand, TResult>` and `: IQueryHandler<TQuery, TResult>` declaration in the Ordering module must be removed.

**New approach — Wolverine convention**: Wolverine discovers handlers at compile time through source generation. A class with a `Handle()` or `HandleAsync()` method whose first parameter matches the message type is automatically recognized as a handler — no interface required. The three command handler classes (`CreateOrderHandler`, `UpdateOrderHandler`, `DeleteOrderHandler`) keep their class structure but drop their `: ICommandHandler<,>` inheritance.

### Rule 2 — MediatR must be removed from the Core project

**Old approach — `IDomainEvent : INotification`**: `IDomainEvent` in `Ordering.Core` inherits from MediatR's `INotification`, making the domain model directly dependent on a third-party messaging library. This breaks Clean Architecture's principle that the domain core should have no infrastructure dependencies. `Ordering.Core.csproj` carries MediatR as a direct NuGet reference solely because of this interface. Domain event handlers in `Ordering.Application` implement `INotificationHandler<T>` from MediatR.

**New approach — plain marker interface**: `IDomainEvent` becomes a self-contained marker interface with no external dependencies. Its existing default-interface properties (`EventId`, `OcurredOn`, `EventType`) are kept as-is. `Ordering.Core.csproj` removes its `MediatR` package reference entirely. Domain event handlers in `Ordering.Application` drop `INotificationHandler<T>` and become plain classes with a `Handle(TEvent notification)` method — Wolverine's discovery convention.

### Rule 3 — `DispatchDomainEventsInterceptor` must replace `IMediator.Publish` with `IMessageBus.PublishAsync`

**Old approach — `IMediator.Publish`**: The EF Core `SaveChangesInterceptor` collects all domain events from tracked aggregates and publishes them synchronously via `IMediator.Publish`. This couples the EF Core infrastructure layer to MediatR. After MediatR is removed from the solution, this interceptor will fail to compile.

**New approach — `IMessageBus.PublishAsync`**: Wolverine's `IMessageBus` is injected into `DispatchDomainEventsInterceptor` (replacing `IMediator`). The loop that calls `await mediator.Publish(domainEvent)` becomes `await bus.PublishAsync(domainEvent)`. Wolverine's `PublishAsync` routes the message to any registered handler via its convention-based discovery. The `Ordering.Infrastructure.csproj` drops the `MediatR` transitive reference and gains a direct reference to `WolverineFx` for this use.

### Rule 4 — MediatR pipeline behaviors must no longer be referenced from Ordering

**Old approach — `ValidationBehavior` and `LoggingBehavior`**: The Application layer registered `ValidationBehavior<,>` and `LoggingBehavior<,>` as MediatR open pipeline behaviors in `AddApplicationServices`. Both behavior classes lived in `BuildingBlocks/Behaviors/` and were deleted during the Catalog API modernization. The `AddMediatR(...)` registration block in `Ordering.Application/DepedencyInjection.cs` currently references these non-existent types.

**New approach — Wolverine middleware**: `AddMediatR(...)` and its `AddOpenBehavior` calls are removed from `AddApplicationServices`. Wolverine's `UseFluentValidation()` option (configured during host setup) automatically discovers all `IValidator<TCommand>` implementations registered in DI and applies them as compile-time middleware before each handler invocation. On validation failure, Wolverine returns `400 Bad Request` with a validation problem details body — no exception is thrown. Wolverine's built-in OpenTelemetry activity tracking replaces `LoggingBehavior`.

### Rule 5 — Carter must be removed; endpoints must use native MapGroup routing

**Old approach — `ICarterModule`**: Each of the six endpoint files in `Ordering.API/Endpoints/` implements `ICarterModule`, registered via `AddCarter()` and `app.MapCarter()`. Carter was a valuable workaround before .NET 7 introduced proper support for organized minimal API routing.

**New approach — `MapGroup` + static extension methods**: Each endpoint file's class is converted from `ICarterModule` to a static class. Route registration moves into a single static extension method (e.g., `MapOrdersEndpoints(this IEndpointRouteBuilder app)`) that groups all order routes under a `"/orders"` prefix via `app.MapGroup("/orders")`. `Program.cs` calls `app.MapOrdersEndpoints()` explicitly, eliminating Carter's runtime assembly scan. `Carter` is removed from `Ordering.API.csproj`.

### Rule 6 — Query records must be deleted; query handler classes must become plain injectable services in the Application layer

**Old approach — Query handler via MediatR**: Read operations travel through: (1) endpoint constructs a query record and calls `sender.Send(query)`, (2) MediatR routes to the query handler class, (3) the handler executes the EF Core query against `IApplicationDbContext` and returns a result record. For reads with no business logic beyond an EF Core `Where`/`Include`/`ToListAsync` chain, this is pure boilerplate.

**New approach — Plain injectable query services**: To preserve Clean Architecture's Application/API layer boundary, query handler classes remain in `Ordering.Application/Orders/Queries/` but shed all CQRS interface inheritance. Each handler becomes a plain class injected directly into the endpoint via the DI container — no Wolverine dispatch, no MediatR dispatch. The query record types (`GetOrdersQuery`, `GetOrderByCustomerQuery`, `GetOrdersByNameQuery`) are deleted because they existed solely as MediatR message carriers; handler methods accept primitive parameters instead. Each handler is registered as a scoped service in `AddApplicationServices` and injected by the endpoint lambda. `AsNoTracking()` is applied to all read-only EF Core queries. The result record types (`GetOrdersResult`, `GetOrderByCustomerResult`, `GetOrdersByNameResult`) are retained as the return types.

### Rule 7 — `ISender` must be replaced with `IMessageBus` in command endpoints

In the three command endpoints (Create, Update, Delete), `ISender sender` (MediatR) is replaced with `IMessageBus bus` (WolverineFx). `sender.Send(command)` becomes `bus.InvokeAsync<TResult>(command)`. `IMessageBus.InvokeAsync<T>` is Wolverine's synchronous in-process equivalent — it locates the handler, runs Wolverine middleware (including FluentValidation), and returns the result. The `MediatR` NuGet reference is removed from `Ordering.Application.csproj`.

### Rule 8 — `AddApplicationServices` must be updated; Wolverine must be registered on the host

- **Remove**: `AddMediatR(...)` and its `AddOpenBehavior` calls from `Ordering.Application/DepedencyInjection.cs`
- **Remove**: `AddCarter()` and `app.MapCarter()` from `Ordering.API/DepedencyInjection.cs`
- **Add**: `builder.Host.UseWolverine(opts => opts.UseFluentValidation())` in `Program.cs` — registers Wolverine, enables convention-based handler discovery across all referenced assemblies, and activates FluentValidation middleware
- **Keep**: `AddValidatorsFromAssembly(...)` — Wolverine's FluentValidation middleware picks validators up from DI
- **Keep**: `AddExceptionHandler<CustomExceptionHandler>()` — kept as a generic 500 safety net; `OrderNotFoundException` is deleted along with its call sites
- **Add**: explicit `app.MapOrdersEndpoints()` replacing `app.MapCarter()`

### Rule 9 — Exception-driven HTTP responses must be replaced with TypedResults and discriminated command results

**Old approach — throw exception, catch in middleware**: `UpdateOrderHandler` and `DeleteOrderHandler` throw `OrderNotFoundException` when the order is not found. This unwinds the call stack, reaches `CustomExceptionHandler`, pattern-matches the type, builds `ProblemDetails`, and writes the response. The exception class lives in `Ordering.Application/Exceptions/OrderNotFoundException.cs`.

**New approach for query endpoints — inline null check with `TypedResults`**: With query logic inlined into endpoints (Rule 6), queries that return empty results simply return `TypedResults.Ok(emptyList)` — no not-found check is needed for list queries. Paginated `GetOrders` retains its existing behavior.

**New approach for command handlers (Update, Delete) — discriminated result**: `UpdateOrderHandler` and `DeleteOrderHandler` return a discriminated result — a sealed type hierarchy where the success case is the existing result record and a `NotFound` marker is added as the failure case. The endpoint pattern-matches the result: `TypedResults.Ok(result)` on success, `TypedResults.NotFound()` on not-found. `OrderNotFoundException` is deleted from `Ordering.Application/Exceptions/`.

---

## 4. Acceptance Criteria

The feature is complete when:

- [ ] `Ordering.Core.csproj` no longer has a `MediatR` NuGet reference
- [ ] `IDomainEvent` no longer inherits from `INotification`; it is a self-contained marker interface
- [ ] `Carter` NuGet reference is removed from `Ordering.API.csproj`
- [ ] No file in the Ordering module references `ICarterModule`, `ICommand`, `ICommandHandler`, `IQuery`, `IQueryHandler`, `ISender`, `IMediator`, `INotificationHandler`, or `IPipelineBehavior`
- [ ] `WolverineFx` is registered on the host via `builder.Host.UseWolverine(opts => opts.UseFluentValidation())` in `Program.cs`
- [ ] All 6 endpoint files no longer implement `ICarterModule`; they are grouped under a single `MapOrdersEndpoints` static extension method using `app.MapGroup("/orders")`
- [ ] The three query record types (`GetOrdersQuery`, `GetOrderByCustomerQuery`, `GetOrdersByNameQuery`) are deleted
- [ ] The three query handler classes (`GetOrdersHandler`, `GetOrderByCustomerHandler`, `GetOrdersByNameHandler`) are retained in `Ordering.Application/Orders/Queries/` as plain injectable services — no `IQueryHandler<,>` inheritance
- [ ] All three query handler classes are registered as scoped services in `AddApplicationServices`
- [ ] All three query handler classes are injected directly into their respective endpoint lambdas (no `ISender`/`IMessageBus` dispatch)
- [ ] `CreateOrderHandler`, `UpdateOrderHandler`, and `DeleteOrderHandler` are plain classes with a `HandleAsync` method — no interface inheritance
- [ ] `OrderCreatedEventHandler` and `OrderUpdatedEventHandler` are plain classes with a `Handle` method — no `INotificationHandler<T>` inheritance
- [ ] `DispatchDomainEventsInterceptor` uses `IMessageBus.PublishAsync` to dispatch domain events; `IMediator` is removed from its constructor
- [ ] `GET /orders` returns a paginated list of orders
- [ ] `GET /orders/{orderName}` returns orders matching the name substring
- [ ] `GET /orders/customer/{customerId}` returns all orders for the customer
- [ ] `POST /orders` with a valid body creates an order and returns `201 Created` with the new order ID
- [ ] `POST /orders` with an invalid body (empty `OrderName` or no `OrderItems`) returns `400 Bad Request` via Wolverine's FluentValidation middleware — no exception thrown
- [ ] `PUT /orders` with a valid body updates the order and returns `200 OK`
- [ ] `PUT /orders` with a non-existent order ID returns `404 Not Found` — no exception thrown
- [ ] `DELETE /orders/{id}` with an existing ID deletes the order and returns `200 OK`
- [ ] `DELETE /orders/{id}` with a non-existent ID returns `404 Not Found` — no exception thrown
- [ ] `UpdateOrderHandler` and `DeleteOrderHandler` return a discriminated result type; the endpoint pattern-matches it to produce the HTTP response
- [ ] `OrderNotFoundException` is deleted from `Ordering.Application/Exceptions/`
- [ ] `GET /health` returns the SQL Server health check result
- [ ] The solution builds with zero errors after the migration (the CQRS compile errors introduced by the Catalog modernization are resolved)
- [ ] Mapster (`request.Adapt<TCommand>()`) is kept for all endpoint request-to-command mappings

---

## 5. Out of Scope

The following are explicitly NOT part of this modernization:

- Changes to any domain model (`Order.cs`, `OrderItem.cs`, `Customer.cs`, `Product.cs`) or value objects
- Changes to EF Core configurations, migrations, or the `ApplicationDbContext` schema
- Changes to seed data in `InitialData.cs`
- Changes to Docker, docker-compose, or deployment configuration
- Adding new endpoints or changing the API surface (URLs, verbs, request/response shapes stay identical)
- Implementing the `BasketCheckoutEventHandler` integration event — it remains a stub; inter-service messaging across Basket and Ordering is a separate concern
- Replacing EF Core with Marten — SQL Server remains the Ordering store; this is architecturally appropriate for a transactional, relational domain
- Introducing a Wolverine durable outbox for domain or integration event delivery
- Adding `IHttpContextAccessor` to `AuditableEntityInterceptor` to capture the real authenticated user (currently hardcoded to `"System"`)
- Authentication or authorization
- OpenTelemetry or Serilog structured logging beyond what Wolverine provides by default
- Health check UI endpoint — only the existing `/health` endpoint is in scope

---

## 6. Decisions

All open questions resolved on 2026-05-25:

1. **Query inlining strategy → Option B**: Query handler classes are kept as plain injectable services in the `Ordering.Application` layer. Query record types are deleted (they were MediatR message carriers only). Handler methods accept primitive parameters, are registered in DI as scoped services, and are injected directly into endpoint lambdas. This preserves Clean Architecture's Application/API boundary — the API layer never touches EF Core types directly.

2. **`OrderNotFoundException` strategy → Option A**: `OrderNotFoundException` is deleted entirely. `UpdateOrderHandler` and `DeleteOrderHandler` return a discriminated result type. The endpoint pattern-matches the result to either `TypedResults.Ok(...)` or `TypedResults.NotFound()`. All typed exceptions in the Ordering module are eliminated.

3. **Wolverine assembly scanning → Option B**: Wolverine scans only `Ordering.Application`. All command handlers (`CreateOrderHandler`, `UpdateOrderHandler`, `DeleteOrderHandler`) and domain event handlers (`OrderCreatedEventHandler`, `OrderUpdatedEventHandler`) live in that assembly. `Ordering.Core` contains no handler classes — scanning it is unnecessary.
