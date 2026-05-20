# Basket.API — Wolverine & Native Minimal API Modernization

## 1. Feature Summary

The Basket.API microservice was built following the same .NET 8 course pattern as the original Catalog.API: MediatR for CQRS dispatch, Carter for endpoint routing, `ICommand<T>`/`IQuery<T>` marker interfaces from `BuildingBlocks.CQRS`, and exception-driven HTTP responses via `BasketNotFoundException`. The Catalog.API modernization (spec `20260515-catalogApiWolverineModernization.md`) has already deleted all `BuildingBlocks/CQRS/`, `BuildingBlocks/Behaviors/`, and `BuildingBlocks/Exceptions/` source files, leaving Basket.API in a broken state — it still references these deleted types.

This modernization brings Basket.API to the same .NET 10 standard now in place for Catalog.API: Wolverine replaces MediatR, native `MapGroup` extension methods replace Carter, the single query handler (`GetBasketQueryHandler`) is inlined into its endpoint using `IBasketRepository` directly, command handlers drop the `ICommand<T>`/`ICommandHandler<T,R>` interfaces, and exception-throwing is replaced with nullable repository returns and `TypedResults` inline null checks. The external API contract (all 3 endpoints, their URLs, verbs, and response shapes) remains identical — only the internal wiring changes.

Basket.API differs from Catalog.API in one structural way: it uses the `IBasketRepository` abstraction (backed by a `CachedBasketRepository` decorator) rather than `IQuerySession` directly. This repository pattern stays intact; however, the `Scrutor` package that provided the `.Decorate<>()` convenience method is replaced by .NET 8+ keyed services — a native platform feature that removes a third-party dependency and follows the same principle that drove Carter's removal.

---

## 2. Data Model / Entities

### ShoppingCart *(unchanged)*
- `UserName`: `string` — Marten document identity (via `.Identity(x => x.UserName)`)
- `Items`: `List<ShoppingCartItem>` — the line items in the cart

### ShoppingCartItem *(unchanged)*
- `Quantity`: `int`
- `Color`: `string`
- `Price`: `decimal`
- `ProductId`: `Guid`
- `ProductName`: `string`

### IBasketRepository *(interface signature change)*
- `GetBasketAsync(string userName, CancellationToken)` → return type changes from `Task<ShoppingCart>` to `Task<ShoppingCart?>`
- `StoreBasketAsync(ShoppingCart basket, CancellationToken)` → unchanged
- `DeleteBasketAsync(string userName, CancellationToken)` → unchanged

### Command Records *(shape unchanged, interface removed)*

The two write-side command records keep their current properties but drop the `: ICommand<TResult>` inheritance:

- `StoreBasketCommand(ShoppingCart Cart)` → result: `StoreBasketResult(string UserName)`
- `DeleteBasketCommand(string UserName)` → result: `DeleteBasketResult(bool Success)`

### Query Records *(deleted)*

`GetBasketQuery(string UserName)` and its companion `GetBasketResult(ShoppingCart Cart)` are deleted entirely — their logic moves inline into the endpoint lambda.

---

## 3. Business Rules & Constraints

> Each rule explains the old approach vs. the new approach to support learning.

### Rule 1 — CQRS interfaces must be stripped from command records and handlers

**Old approach — BuildingBlocks.CQRS marker interfaces**: `StoreBasketCommand` extends `ICommand<StoreBasketResult>`, `DeleteBasketCommand` extends `ICommand<DeleteBasketResult>`. The handler classes extend `ICommandHandler<TCommand, TResult>`, which in turn extends MediatR's `IRequestHandler<TRequest, TResponse>`. These compile-time markers are what MediatR uses to route a `sender.Send(command)` call to the correct handler at runtime via reflection.

**New approach — Wolverine convention**: Wolverine discovers handlers at compile time via source generation. Any class with a method named `Handle` or `HandleAsync` whose first parameter matches the message type is automatically registered — no interface required. The handler class declaration `StoreBasketCommandHandler(IBasketRepository, DiscountProtoServiceClient)` needs no change beyond stripping the `: ICommandHandler<>` inheritance. The `using BuildingBlocks.CQRS;` import is removed from both handler files.

### Rule 2 — `GetBasketQueryHandler` must be deleted; the query must be inlined in the endpoint using `IBasketRepository`

**Old approach — Query handler via MediatR**: `GetBasket` flows through three layers: (1) endpoint builds `GetBasketQuery(userName)` and calls `sender.Send(query)`, (2) MediatR routes to `GetBasketQueryHandler`, (3) the handler calls `repository.GetBasketAsync(userName)` and wraps the result. For a read with no business logic beyond a not-found check, layers 1 and 2 add indirection with no benefit.

**New approach — Direct `IBasketRepository` injection**: The `GetBasket` endpoint lambda receives `IBasketRepository repository` directly as a parameter. It calls `repository.GetBasketAsync(userName, ct)` inline. This is the "thin read model" side of CQRS applied to the Basket domain — queries against the repository are simple data fetches and belong in the endpoint. `GetBasketQueryHandler.cs` is deleted entirely. `GetBasketQuery` and `GetBasketResult` records (defined in that file) are also deleted.

Unlike Catalog.API (where the query sessions use `IQuerySession`), Basket.API injects `IBasketRepository` so the Redis caching decorator remains active for all reads — this is an explicit design constraint.

### Rule 3 — `IBasketRepository.GetBasketAsync` must return `ShoppingCart?` instead of throwing

**Old approach — throw on null**: `BasketRepository.GetBasketAsync` calls `session.LoadAsync<ShoppingCart>(userName)`. If the result is `null`, it throws `new BasketNotFoundException(userName)`, which bubbles up through the call stack to `CustomExceptionHandler`, which pattern-matches the type and returns a `404 ProblemDetails` response. This is a predictable outcome (basket not found) expressed as an exceptional control flow.

**New approach — nullable return + inline TypedResults**: The repository changes its `GetBasketAsync` signature to return `Task<ShoppingCart?>`. The `session.LoadAsync` result is returned directly (null included). The endpoint lambda receives the nullable result and handles it inline:
```
if (basket is null) return TypedResults.NotFound();
return TypedResults.Ok(new GetBasketResponse(basket));
```
`CachedBasketRepository.GetBasketAsync` must also be updated: if the inner `repository.GetBasketAsync` returns `null`, it must return `null` (not try to cache a null value). The cache-set logic is skipped when the basket is null.

### Rule 4 — Carter must be removed; endpoints must use native MapGroup routing

**Old approach — `ICarterModule`**: Each `*Endpoints.cs` file declares a class implementing `ICarterModule` with an `AddRoutes(IEndpointRouteBuilder app)` method. Carter scans the assembly at startup and registers all routes. `AddCarter()` and `app.MapCarter()` are the host entry points.

**New approach — `MapGroup` + static extension methods**: Each `*Endpoints.cs` file becomes a static class with a method like `MapGetBasketEndpoint(this RouteGroupBuilder group)`. A top-level `BasketEndpoints` static class aggregates them under a `/basket` group and is registered via `app.MapBasketEndpoints()` in `Program.cs`. This is explicit, AOT-friendly, and eliminates the runtime assembly scan. The pattern exactly mirrors `ProductsEndpoints.cs` in the modernized Catalog.API.

### Rule 5 — `ISender` must be replaced with `IMessageBus` in command endpoints

In the two command endpoints (`StoreBasket`, `DeleteBasket`), `ISender sender` is replaced with `IMessageBus bus` from `WolverineFx`. The dispatch call changes from `sender.Send(command)` to `bus.InvokeAsync<TResult>(command)`. `IMessageBus.InvokeAsync<T>` is Wolverine's equivalent — it locates the handler by convention, runs Wolverine middleware (including FluentValidation), and returns the result.

### Rule 6 — `Program.cs` service registrations must be updated

- **Remove**: `AddMediatR(config => ...)` with all `AddOpenBehavior` calls
- **Remove**: `AddCarter()` and `app.MapCarter()`
- **Add**: `builder.Host.UseWolverine(opts => opts.UseFluentValidation())` — registers Wolverine with the host and enables FluentValidation middleware that picks up all `IValidator<T>` implementations from DI
- **Add**: `.IntegrateWithWolverine()` chained on `AddMarten(...)` — from `WolverineFx.Marten`, this wires Marten's session lifecycle into Wolverine's per-message IoC scope, ensuring `IDocumentSession` (used by `BasketRepository`) is correctly scoped per Wolverine message invocation
- **Keep**: `AddValidatorsFromAssembly(assembly)` — Wolverine's FluentValidation middleware picks validators up from DI
- **Replace** Scrutor decoration with keyed services (see Rule 9)
- **Keep**: `builder.Services.AddExceptionHandler<CustomExceptionHandler>()` — the simplified generic 500 safety net from BuildingBlocks
- **Add**: explicit `app.MapBasketEndpoints()` call replacing `app.MapCarter()`

### Rule 7 — `BasketNotFoundException` must be deleted

With `BasketRepository.GetBasketAsync` returning `ShoppingCart?` instead of throwing, `BasketNotFoundException` has no remaining callsites. It is deleted. `CustomExceptionHandler` (in BuildingBlocks) was already simplified to a generic 500 handler during the Catalog.API modernization — no further changes to it are required.

### Rule 9 — Scrutor must be removed; decorator registration must use .NET keyed services

**Old approach — Scrutor `.Decorate<>()`**: `AddScoped<IBasketRepository, BasketRepository>()` registers the concrete repository, then `Decorate<IBasketRepository, CachedBasketRepository>()` (from `Scrutor`) wraps it transparently. Scrutor re-registers `IBasketRepository` as a factory that constructs `CachedBasketRepository` with the original `BasketRepository` resolved internally. This is convenient but adds a third-party NuGet dependency solely for one registration call.

**New approach — .NET 8+ keyed services**: The inner `BasketRepository` is registered under a named key (`"basket:inner"`); `IBasketRepository` (the public binding) is registered as a factory that resolves the keyed inner instance and wraps it with `CachedBasketRepository`. No third-party library is needed:
```csharp
builder.Services.AddKeyedScoped<IBasketRepository, BasketRepository>("basket:inner");
builder.Services.AddScoped<IBasketRepository>(provider =>
    new CachedBasketRepository(
        provider.GetRequiredKeyedService<IBasketRepository>("basket:inner"),
        provider.GetRequiredService<IDistributedCache>()));
```
The `Scrutor` NuGet reference is removed from `Basket.API.csproj`. `CachedBasketRepository`'s constructor signature (`IBasketRepository repository, IDistributedCache cache`) is unchanged.

### Rule 8 — Mapster is kept for command endpoint request-to-command mappings

`StoreBasketEndpoints` currently uses `request.Adapt<StoreBasketCommand>()`. This is kept in the modernized endpoint. `GetBasketEndpoints` currently uses `result.Adapt<GetBasketResponse>()` — since the result is inlined and the response is constructed directly (`new GetBasketResponse(basket)`), Mapster is not needed for the Get endpoint. `DeleteBasketEndpoints` constructs the response inline from the result; Mapster is kept for `StoreBasket` per the Catalog.API precedent.

---

## 4. Acceptance Criteria

The feature is complete when:

- [ ] `Carter` NuGet reference is removed from `Basket.API.csproj`
- [ ] `Scrutor` NuGet reference is removed from `Basket.API.csproj`
- [ ] No file in Basket.API references `ICarterModule`, `ICommand`, `ICommandHandler`, `IQuery`, `IQueryHandler`, `ISender`, `IPipelineBehavior`, or `Scrutor`
- [ ] `IBasketRepository` is registered via keyed services: `BasketRepository` is registered with key `"basket:inner"` and `IBasketRepository` is registered as a factory wrapping it with `CachedBasketRepository`
- [ ] `Program.cs` does not call `.Decorate<>()`
- [ ] `GetBasketHandler.cs` is deleted (`GetBasketQuery`, `GetBasketResult`, and `GetBasketQueryHandler` are gone)
- [ ] `BasketNotFoundException.cs` is deleted
- [ ] `IBasketRepository.GetBasketAsync` returns `Task<ShoppingCart?>`
- [ ] `BasketRepository.GetBasketAsync` returns `null` when the cart is not found (no exception thrown)
- [ ] `CachedBasketRepository.GetBasketAsync` returns `null` when the inner repository returns `null` (cache set is skipped)
- [ ] `StoreBasketCommand` and `DeleteBasketCommand` records no longer extend any interface
- [ ] `StoreBasketCommandHandler` and `DeleteBasketCommandHandler` classes no longer implement any interface
- [ ] `GetBasketEndpoints`, `StoreBasketEndpoints`, and `DeleteBasketEndpoints` are static classes with `MapGroup`-based extension methods (not `ICarterModule`)
- [ ] A top-level `BasketEndpoints.cs` static class aggregates all three endpoints under a `/basket` group
- [ ] `Program.cs` calls `builder.Host.UseWolverine(opts => opts.UseFluentValidation())`
- [ ] `Program.cs` chains `.IntegrateWithWolverine()` on `AddMarten(...)`
- [ ] `Program.cs` calls `app.MapBasketEndpoints()` (no `app.MapCarter()`)
- [ ] `Program.cs` does not call `AddMediatR(...)` or `AddCarter()`
- [ ] `GET /basket/{userName}` with a valid existing user name returns `200 OK` with the shopping cart
- [ ] `GET /basket/{userName}` with a non-existent user name returns `404 Not Found` (no exception thrown)
- [ ] `POST /basket` with a valid body stores the cart, calls the Discount gRPC service, and returns `201 Created`
- [ ] `POST /basket` with an invalid body (empty `UserName`) returns `400 Bad Request` via Wolverine's FluentValidation middleware
- [ ] `DELETE /basket/{userName}` deletes the cart and returns `200 OK`
- [ ] `DELETE /basket/{userName}` with an invalid body (empty `UserName`) returns `400 Bad Request` via Wolverine's FluentValidation middleware
- [ ] `GET /health` returns both the Npgsql and Redis health check results
- [ ] `StoreBasketCommandValidator` and `DeleteBasketCommandValidator` remain in their respective handler files and are picked up by Wolverine's middleware automatically

---

## 5. Out of Scope

The following are explicitly NOT part of this modernization:

- Changes to `ShoppingCart.cs` or `ShoppingCartItem.cs` model structure
- Changes to `IBasketRepository`, `BasketRepository`, or `CachedBasketRepository` beyond the nullable return type of `GetBasketAsync`
- Implementing `CheckoutBasket` — the handler and endpoint stubs exist but are empty; checkout is deferred to a future feature
- Changes to Docker, docker-compose, or deployment configuration
- Adding new endpoints or changing the API surface (URLs, verbs, request/response shapes stay identical)
- Upgrading Marten, WolverineFx, or Redis cache packages beyond their current versions
- Multi-service messaging (Wolverine's transport/broker features — only in-process command dispatch is in scope)
- Adding OpenTelemetry or Serilog beyond what Wolverine provides by default
- Authentication or authorization
- Health check UI endpoint — only the existing `/health` endpoint is in scope
- `BuildingBlocks` changes — all necessary BuildingBlocks cleanup was completed in the Catalog.API modernization

---

## 6. Decisions

All open questions resolved on 2026-05-20:

1. **GetBasket not-found strategy → Option A**: `GetBasketEndpoint` returns `TypedResults.NotFound()` inline when `repository.GetBasketAsync` returns `null`. Consistent with Catalog.API's `GetProductById` pattern. No exception thrown.

2. **DeleteBasket not-found behavior → Option A**: Keep always-success behavior. `DeleteBasketCommandHandler` returns `DeleteBasketResult(bool Success)` as a simple result. The `.ProducesProblem(StatusCodes.Status404NotFound)` annotation is removed from the endpoint since it was never accurate — Marten silently ignores deletes for non-existent documents.

3. **Scrutor → keyed services**: Replace `Scrutor` with .NET 8+ keyed services for the `CachedBasketRepository` decorator registration. `BasketRepository` is registered under the key `"basket:inner"`; `IBasketRepository` is registered as a factory that resolves the keyed inner instance and constructs `CachedBasketRepository`. `Scrutor` NuGet reference is removed.
