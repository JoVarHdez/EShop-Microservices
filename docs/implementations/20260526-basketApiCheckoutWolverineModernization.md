# Implementation Plan: Basket.API — CheckoutBasket Wolverine & Native Minimal API Modernization

> Spec: [`docs/specs/20260526-basketApiCheckoutWolverineModernization.md`](../specs/20260526-basketApiCheckoutWolverineModernization.md)

**TL;DR** — Phase 1 strips the CQRS interfaces from the handler; Phase 2 rewrites the endpoint as a static extension method (parallel with Phase 1); Phase 3 wires the new endpoint into the `BasketEndpoints.cs` aggregator.
Phases 1 and 2 are independent and can run in parallel. Phase 3 depends on Phase 2. `BasketEndpoints.cs` is assumed to exist as created by the `20260520-basketApiWolverineModernization` plan.

---

## Relevant Files

| Action | File |
|--------|------|
| MODIFY | `src/Services/Basket/Basket.API/Basket/CheckoutBasket/CheckoutBasketHandler.cs` |
| MODIFY | `src/Services/Basket/Basket.API/Basket/CheckoutBasket/CheckoutBasketEndpoints.cs` |
| MODIFY | `src/Services/Basket/Basket.API/Basket/BasketEndpoints.cs` |

---

## Phase 1 — Strip CQRS interfaces from the handler *(parallel with Phase 2)*

Wolverine discovers `CheckoutBasketCommandHandler` at compile time by convention (a `Handle` method whose parameter matches the command type). The `ICommand<T>` and `ICommandHandler<T, R>` interfaces from `BuildingBlocks.CQRS` are not needed and no longer exist in source after the first spec's execution.

### 1.1 — Remove CQRS interface inheritance and the BuildingBlocks.CQRS using

**File**: `src/Services/Basket/Basket.API/Basket/CheckoutBasket/CheckoutBasketHandler.cs`

- Remove `using BuildingBlocks.CQRS;`
- Strip `: ICommand<CheckoutBasketResult>` from the `CheckoutBasketCommand` record declaration
- Strip `: ICommandHandler<CheckoutBasketCommand, CheckoutBasketResult>` from the `CheckoutBasketCommandHandler` class declaration

```csharp
// BEFORE
using BuildingBlocks.CQRS;
...
public record CheckoutBasketCommand(BasketCheckoutDto BasketCheckoutDto) : ICommand<CheckoutBasketResult>;
...
public class CheckoutBasketCommandHandler(...) : ICommandHandler<CheckoutBasketCommand, CheckoutBasketResult>
{
    public async Task<CheckoutBasketResult> Handle(CheckoutBasketCommand request, CancellationToken cancellationToken)

// AFTER
// (no BuildingBlocks.CQRS using)
public record CheckoutBasketCommand(BasketCheckoutDto BasketCheckoutDto);
...
public class CheckoutBasketCommandHandler(...)
{
    public async Task<CheckoutBasketResult> Handle(CheckoutBasketCommand command, CancellationToken cancellationToken)
```

> **Learning note**: Wolverine uses source generation to find handlers at compile time via the `Handle` method name convention — no marker interface required.

- **Unchanged**: `CheckoutBasketResult`, `CheckoutBasketCommandValidator`, all business logic inside `Handle` (`GetBasketAsync`, `Adapt<BasketCheckoutEvent>()`, `publishEndpoint.Publish()`, `DeleteBasketAsync`), `IPublishEndpoint` constructor parameter

---

## Phase 2 — Rewrite checkout endpoint as a static extension method *(parallel with Phase 1)*

Carter's `ICarterModule` is removed in the first spec's execution. All endpoint modules are replaced with `static` classes that expose `Map*Endpoint(this RouteGroupBuilder group)` extension methods, consistent with `GetBasketEndpoints`, `StoreBasketEndpoints`, and `DeleteBasketEndpoints`. This phase also fixes two bugs in the current implementation: the absolute path (`/basket/checkout` → `/checkout`), the status code mismatch (`201` → `200`), and the missing not-found handling.

### 2.1 — Replace ICarterModule with a static extension method

**File**: `src/Services/Basket/Basket.API/Basket/CheckoutBasket/CheckoutBasketEndpoints.cs`

- Remove `using Carter;`
- Remove `using MediatR;`
- Add `using Wolverine;`
- Change `public class CheckoutBasketEndpoints : ICarterModule` to `public static class CheckoutBasketEndpoints`
- Remove the `public void AddRoutes(IEndpointRouteBuilder app)` method entirely
- Add `public static RouteGroupBuilder MapCheckoutBasketEndpoint(this RouteGroupBuilder group)` with the full new implementation:

```csharp
using Basket.API.DTOs;
using Mapster;
using Wolverine;

namespace Basket.API.Basket.CheckoutBasket
{
    public record CheckoutBasketRequest(BasketCheckoutDto BasketCheckoutDto);
    public record CheckoutBasketResponse(bool IsSuccess);

    public static class CheckoutBasketEndpoints
    {
        public static RouteGroupBuilder MapCheckoutBasketEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("/checkout", async (CheckoutBasketRequest request, IMessageBus bus) =>
            {
                var command = request.Adapt<CheckoutBasketCommand>();

                var result = await bus.InvokeAsync<CheckoutBasketResult>(command);

                if (!result.IsSuccess)
                    return Results.NotFound();

                var response = result.Adapt<CheckoutBasketResponse>();

                return Results.Ok(response);
            })
                .WithName("CheckoutBasket")
                .Produces<CheckoutBasketResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithSummary("Checkout a basket")
                .WithDescription("Checkout a basket with the provided details");

            return group;
        }
    }
}
```

> **Learning note**: The route is `/checkout` — not `/basket/checkout`. The `/basket` prefix is already provided by `MapGroup("/basket")` in `BasketEndpoints.cs`; combining both would produce `/basket/basket/checkout`.

> **Learning note**: `IMessageBus.InvokeAsync<T>()` is Wolverine's in-process dispatch. `IPublishEndpoint` (MassTransit) remains in the *handler* for inter-service event publishing — the two work at different layers and do not replace each other.

- **Unchanged**: `CheckoutBasketRequest`, `CheckoutBasketResponse` record definitions

---

## Phase 3 — Wire checkout into the BasketEndpoints aggregator *(depends on Phase 2)*

`BasketEndpoints.cs` was created by the first implementation plan with three endpoint calls. A fourth call is needed so the checkout route is registered under the `/basket` group when `app.MapBasketEndpoints()` is called from `Program.cs`.

### 3.1 — Add MapCheckoutBasketEndpoint to the aggregator

**File**: `src/Services/Basket/Basket.API/Basket/BasketEndpoints.cs`

- Add `group.MapCheckoutBasketEndpoint();` as the fourth call inside `MapBasketEndpoints`:

```csharp
// BEFORE
var group = app.MapGroup("/basket");
group.MapGetBasketEndpoint();
group.MapStoreBasketEndpoint();
group.MapDeleteBasketEndpoint();

// AFTER
var group = app.MapGroup("/basket");
group.MapGetBasketEndpoint();
group.MapStoreBasketEndpoint();
group.MapDeleteBasketEndpoint();
group.MapCheckoutBasketEndpoint();
```

- **Unchanged**: `MapGroup("/basket")` path, `MapBasketEndpoints` method signature, all other endpoint calls

---

## Verification

1. **Build**: `dotnet build src/eshop-microservies.slnx` must succeed with 0 errors.

2. **Grep check**: Search `src/Services/Basket/Basket.API/Basket/CheckoutBasket/` for the following — must return no results:
   - `ICarterModule`
   - `ISender`
   - `ICommand`
   - `ICommandHandler`
   - `BuildingBlocks.CQRS`
   - `/basket/checkout` (absolute path — replaced by `/checkout` relative path)
   - `Status201Created` (replaced by `Status200OK`)

3. **HTTP smoke tests** (run against `docker-compose up`):
   - `POST /basket/checkout` with a valid `BasketCheckoutRequest` body for an existing user → `200 OK` with `{ "isSuccess": true }`, basket is deleted, `BasketCheckoutEvent` published to RabbitMQ
   - `POST /basket/checkout` with a valid body for a non-existent user → `404 Not Found`
   - `POST /basket/checkout` with `BasketCheckoutDto: null` → `400 Bad Request` (Wolverine FluentValidation middleware)
   - `POST /basket/checkout` with empty `UserName` → `400 Bad Request` (Wolverine FluentValidation middleware)
   - `GET /health` → `200 OK` with NpgSql and Redis checks passing
