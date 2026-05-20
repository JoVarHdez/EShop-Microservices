# Implementation Plan: Basket.API — Wolverine & Native Minimal API Modernization

> Spec: [`docs/specs/20260520-basketApiWolverineModernization.md`](../specs/20260520-basketApiWolverineModernization.md)

**TL;DR** — Work through 5 phases: delete dead code first (unblocks everything), then update the data layer (repository signatures), then strip interfaces from command handlers, then rewrite all endpoint files, then wire the host. Phases 2 and 3 are independent of each other and can run in parallel. Unlike the Catalog plan, there is no BuildingBlocks work — that was completed in the Catalog.API modernization.

---

## Phase 1 — Delete dead code & remove packages

All steps in this phase are **independent and can run in parallel**.

### 1.1 — Delete `GetBasketHandler.cs`

The query record, result record, and handler class are all in this single file. The handler body (`repository.GetBasketAsync`) moves inline into the endpoint in Phase 4.

- `src/Services/Basket/Basket.API/Basket/GetBasket/GetBasketHandler.cs`

### 1.2 — Delete `BasketNotFoundException.cs`

`BasketRepository.GetBasketAsync` currently throws this. Phase 2 changes the repository to return `null` instead, eliminating the only callsite.

- `src/Services/Basket/Basket.API/Exceptions/BasketNotFoundException.cs`

### 1.3 — Remove NuGet package references from `Basket.API.csproj`

**`src/Services/Basket/Basket.API/Basket.API.csproj`** — remove:
- `Carter` — replaced by native `MapGroup` extension methods
- `Scrutor` — replaced by .NET keyed services

---

## Phase 2 — Update the data layer *(depends on Phase 1.2)*

All three steps touch different files and are **independent of each other**.

### 2.1 — `Data/IBasketRepository.cs`

Change `GetBasketAsync` return type from `Task<ShoppingCart>` to `Task<ShoppingCart?>`. No other members change.

### 2.2 — `Data/BasketRepository.cs`

- Remove `using Basket.API.Exceptions;`
- Change `GetBasketAsync` return type to `Task<ShoppingCart?>`
- Replace the ternary throw expression with a direct return of the nullable result:
  - Old: `return basket is null ? throw new BasketNotFoundException(userName) : basket;`
  - New: `return basket;`

### 2.3 — `Data/CachedBasketRepository.cs`

- Change `GetBasketAsync` return type to `Task<ShoppingCart?>`
- Guard the cache-set call: only serialize and store when the basket is not null
  - Old: always calls `GetBasketAsync` on the inner repository then `cache.SetStringAsync`
  - New: if `basket is null`, return `null` immediately (skip `SetStringAsync`)

---

## Phase 3 — Strip interfaces from command handlers *(parallel with Phase 2)*

Both steps are **independent** and only remove interface inheritance — no logic changes. The `BuildingBlocks.CQRS` types no longer exist in BuildingBlocks source; these files simply reference them via old `using` statements against the stale compiled DLL.

### 3.1 — `Basket/StoreBasket/StoreBasketHandler.cs`

- Remove `using BuildingBlocks.CQRS;`
- Strip `: ICommand<StoreBasketResult>` from `StoreBasketCommand` record declaration
- Strip `: ICommandHandler<StoreBasketCommand, StoreBasketResult>` from `StoreBasketCommandHandler` class declaration
- Handler body (`DeductDiscount` + `repository.StoreBasketAsync`) is **unchanged**

### 3.2 — `Basket/DeleteBasket/DeleteBasketHandler.cs`

- Remove `using BuildingBlocks.CQRS;`
- Strip `: ICommand<DeleteBasketResult>` from `DeleteBasketCommand` record declaration
- Strip `: ICommandHandler<DeleteBasketCommand, DeleteBasketResult>` from `DeleteBasketCommandHandler` class declaration
- Handler body is **unchanged** — always returns `DeleteBasketResult(true)`

---

## Phase 4 — Rewrite endpoint files *(depends on Phase 2 and Phase 3)*

**Pattern for every endpoint file**: the `ICarterModule` class becomes a `static` class; `AddRoutes(IEndpointRouteBuilder app)` becomes `MapXxxEndpoint(this RouteGroupBuilder group)`; route paths drop the `/basket` prefix (moved to the `MapGroup` in Step 4.1). Mirrors the pattern in `ProductsEndpoints.cs` from the modernized Catalog.API.

### 4.1 — Create `Basket/BasketEndpoints.cs` *(new file)*

```
src/Services/Basket/Basket.API/Basket/BasketEndpoints.cs
```

Static class `BasketEndpoints` with a single extension method on `IEndpointRouteBuilder`:

```csharp
public static IEndpointRouteBuilder MapBasketEndpoints(this IEndpointRouteBuilder app)
{
    var group = app.MapGroup("/basket");

    group.MapGetBasketEndpoint();
    group.MapStoreBasketEndpoint();
    group.MapDeleteBasketEndpoint();

    return app;
}
```

### 4.2 — `Basket/GetBasket/GetBasketEndpoints.cs`

Read-side endpoint — query handler deleted in Phase 1; inline using `IBasketRepository` + `TypedResults.NotFound()`.

- `public class GetBasketEndpoints : ICarterModule` → `public static class GetBasketEndpoints`
- `public void AddRoutes(IEndpointRouteBuilder app)` → `public static RouteGroupBuilder MapGetBasketEndpoint(this RouteGroupBuilder group)`
- Route path: `/basket/{userName}` → `/{userName}`
- Remove `ISender sender` from lambda; add `IBasketRepository repository` and `CancellationToken ct`
- Remove the `sender.Send(new GetBasketQuery(userName))` dispatch
- Inline repository call and null check:
  ```csharp
  var basket = await repository.GetBasketAsync(userName, ct);
  if (basket is null) return Results.NotFound();
  return Results.Ok(new GetBasketResponse(basket));
  ```
- `GetBasketResponse` stays in this file; construct it directly (`new GetBasketResponse(basket)`) — no `Adapt<>` needed
- Remove `using Carter;`, `using MediatR;`, `using Mapster;`
- Return `group`

### 4.3 — `Basket/StoreBasket/StoreBasketEndpoints.cs`

Write-side endpoint — replace `ISender` with `IMessageBus`.

- `public class StoreBasketEndpoints : ICarterModule` → `public static class StoreBasketEndpoints`
- `public void AddRoutes(IEndpointRouteBuilder app)` → `public static RouteGroupBuilder MapStoreBasketEndpoint(this RouteGroupBuilder group)`
- Route path: `/basket` → `/`
- Replace `ISender sender` with `IMessageBus bus`
- Replace `sender.Send(command)` with `await bus.InvokeAsync<StoreBasketResult>(command)`
- `request.Adapt<StoreBasketCommand>()` and `result.Adapt<StoreBasketResponse>()` are **kept**
- Remove `using Carter;`, `using MediatR;`; add `using Wolverine;`
- Return `group`

### 4.4 — `Basket/DeleteBasket/DeleteBasketEndpoints.cs`

Write-side endpoint — replace `ISender` with `IMessageBus`; remove the inaccurate `ProducesProblem(404)`.

- `public class DeleteBasketEndpoints : ICarterModule` → `public static class DeleteBasketEndpoints`
- `public void AddRoutes(IEndpointRouteBuilder app)` → `public static RouteGroupBuilder MapDeleteBasketEndpoint(this RouteGroupBuilder group)`
- Route path: `/basket/{userName}` → `/{userName}`
- Replace `ISender sender` with `IMessageBus bus`
- Replace `sender.Send(new DeleteBasketCommand(userName))` with `await bus.InvokeAsync<DeleteBasketResult>(new DeleteBasketCommand(userName))`
- Remove `.ProducesProblem(StatusCodes.Status404NotFound)` — Marten delete always succeeds; this annotation was never accurate
- Response construction via `result.Adapt<DeleteBasketResponse>()` is **kept**
- Remove `using Carter;`, `using MediatR;`; add `using Wolverine;`
- Return `group`

---

## Phase 5 — Update `Program.cs` *(depends on Phase 1.3, Phase 4)*

**`src/Services/Basket/Basket.API/Program.cs`**

### 5.1 — Remove

- `using Carter;`
- The entire `builder.Services.AddMediatR(config => { ... });` block (all lines including `AddOpenBehavior` calls)
- `builder.Services.AddCarter();`
- `builder.Services.AddScoped<IBasketRepository, BasketRepository>();`
- `builder.Services.Decorate<IBasketRepository, CachedBasketRepository>();`
- The commented-out manual factory block (lines starting with `//builder.Services.AddScoped<IBasketRepository>...`) — replaced by keyed services below
- `app.MapCarter();`

Keep `var assembly = typeof(Program).Assembly;` — still needed for `AddValidatorsFromAssembly(assembly)`.

### 5.2 — Add Wolverine host registration

After `builder.Services.AddValidatorsFromAssembly(assembly)`:

```csharp
builder.Host.UseWolverine(opts =>
{
    opts.UseFluentValidation();
});
```

Add `using Wolverine;` and `using Wolverine.Marten;` at the top.

### 5.3 — Integrate Marten with Wolverine

Chain `.IntegrateWithWolverine()` onto the existing `AddMarten(...).UseLightweightSessions()` call:

```csharp
builder.Services.AddMarten(config =>
{
    config.Connection(builder.Configuration.GetConnectionString("Database")!);
    config.Schema.For<ShoppingCart>().Identity(x => x.UserName);
})
.UseLightweightSessions()
.IntegrateWithWolverine();
```

### 5.4 — Replace Scrutor with keyed services

After the Marten block, add:

```csharp
builder.Services.AddKeyedScoped<IBasketRepository, BasketRepository>("basket:inner");
builder.Services.AddScoped<IBasketRepository>(provider =>
    new CachedBasketRepository(
        provider.GetRequiredKeyedService<IBasketRepository>("basket:inner"),
        provider.GetRequiredService<IDistributedCache>()));
```

### 5.5 — Register endpoints

Replace `app.MapCarter()` with:

```csharp
app.MapBasketEndpoints();
```

---

## Relevant Files

| Action | File |
|--------|------|
| DELETE | `src/Services/Basket/Basket.API/Basket/GetBasket/GetBasketHandler.cs` |
| DELETE | `src/Services/Basket/Basket.API/Exceptions/BasketNotFoundException.cs` |
| MODIFY | `src/Services/Basket/Basket.API/Basket.API.csproj` |
| MODIFY | `src/Services/Basket/Basket.API/Data/IBasketRepository.cs` |
| MODIFY | `src/Services/Basket/Basket.API/Data/BasketRepository.cs` |
| MODIFY | `src/Services/Basket/Basket.API/Data/CachedBasketRepository.cs` |
| MODIFY | `src/Services/Basket/Basket.API/Basket/StoreBasket/StoreBasketHandler.cs` |
| MODIFY | `src/Services/Basket/Basket.API/Basket/DeleteBasket/DeleteBasketHandler.cs` |
| MODIFY | `src/Services/Basket/Basket.API/Basket/GetBasket/GetBasketEndpoints.cs` |
| MODIFY | `src/Services/Basket/Basket.API/Basket/StoreBasket/StoreBasketEndpoints.cs` |
| MODIFY | `src/Services/Basket/Basket.API/Basket/DeleteBasket/DeleteBasketEndpoints.cs` |
| MODIFY | `src/Services/Basket/Basket.API/Program.cs` |
| CREATE | `src/Services/Basket/Basket.API/Basket/BasketEndpoints.cs` |

---

## Verification

1. **Build**: `dotnet build src/eshop-microservies.slnx` must succeed with 0 errors.
2. **Grep check**: search across `src/Services/Basket/` for `ICarterModule`, `ISender`, `ICommand`, `ICommandHandler`, `IQuery`, `IQueryHandler`, `AddMediatR`, `AddCarter`, `MapCarter`, `Scrutor`, `Decorate`, `BasketNotFoundException` — must return no results.
3. **HTTP smoke tests** (run against `docker-compose up`):
   - `GET /basket/{existing-userName}` → `200 OK` with shopping cart
   - `GET /basket/{non-existent-userName}` → `404 Not Found` (no exception in logs)
   - `POST /basket` valid body → `201 Created` with discount deducted from item prices
   - `POST /basket` body with empty `UserName` → `400 Bad Request` with FluentValidation errors
   - `DELETE /basket/{userName}` → `200 OK`
   - `DELETE /basket/{userName}` with empty `userName` path param → `400 Bad Request`
   - `GET /health` → `200 OK` with both Npgsql and Redis checks passing
