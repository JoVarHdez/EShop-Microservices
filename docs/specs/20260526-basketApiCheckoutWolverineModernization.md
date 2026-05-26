# Basket.API — CheckoutBasket Wolverine & Native Minimal API Modernization

## 1. Feature Summary

The CheckoutBasket operation was previously an empty stub. It has since been implemented using the same .NET 8 course patterns as the rest of Basket.API: MediatR for command dispatch (`ICommand<T>`, `ICommandHandler<T,R>`), Carter for endpoint routing (`ICarterModule`, `ISender`), and exception-driven HTTP responses. The implementation introduces two new collaborators not present in the other operations: MassTransit's `IPublishEndpoint` (for publishing a `BasketCheckoutEvent` to RabbitMQ) and a `BasketCheckoutDto` input object (carrying shipping, billing, and payment data). It also contains a latent bug — the endpoint declares `Produces<>(StatusCodes.Status201Created)` but `Results.Ok()` returns `200 OK`.

This spec modernizes CheckoutBasket to match the Wolverine/native MapGroup standard established in `20260520-basketApiWolverineModernization.md`, which is assumed complete. MassTransit's `IPublishEndpoint` is explicitly kept — it is the integration-event publishing mechanism for inter-service communication via RabbitMQ and is a different concern from Wolverine's in-process command dispatch. The `AddMessageBroker()` registration in `Program.cs` and the `BuildingBlocks.Messaging` project are also out of scope — they are already wired correctly.

---

## 2. Data Model / Entities

### BasketCheckoutDto *(unchanged)*
- `UserName`: `string`
- `CustomerId`: `Guid`
- `TotalPrice`: `decimal`
- `FirstName`, `LastName`, `EmailAddress`, `AddressLine`, `Country`, `State`, `ZipCode`: `string`
- `CardName`, `CardNumber`, `Expiration`, `CVV`: `string`
- `PaymentMethod`: `int`

### BasketCheckoutEvent *(unchanged, lives in BuildingBlocks.Messaging)*
- Same fields as `BasketCheckoutDto` plus `TotalPrice` set from the stored basket at runtime
- Extends `IntegrationEvent` (`Id`, `OccurredOn`, `EventType`)

### Command Record *(shape unchanged, interface removed)*

- `CheckoutBasketCommand(BasketCheckoutDto BasketCheckoutDto)` — drops `: ICommand<CheckoutBasketResult>`
- `CheckoutBasketResult(bool IsSuccess)` — unchanged if open question 1 resolves to Option A; becomes a discriminated union if Option B

---

## 3. Business Rules & Constraints

### Rule 1 — CQRS interfaces must be stripped from `CheckoutBasketCommand` and `CheckoutBasketCommandHandler`

**Old approach**: `CheckoutBasketCommand` extends `ICommand<CheckoutBasketResult>` and `CheckoutBasketCommandHandler` extends `ICommandHandler<CheckoutBasketCommand, CheckoutBasketResult>` — both interface chains from `BuildingBlocks.CQRS`, which no longer exists in source after the first spec's execution.

**New approach — Wolverine convention**: Remove `using BuildingBlocks.CQRS;`. Strip `: ICommand<CheckoutBasketResult>` from the command record and `: ICommandHandler<CheckoutBasketCommand, CheckoutBasketResult>` from the handler class. Wolverine discovers `CheckoutBasketCommandHandler` at compile time via the `Handle` method convention. Constructor-injected `IBasketRepository` and `IPublishEndpoint` continue to be resolved from the DI container.

### Rule 2 — `IPublishEndpoint` must remain in the handler

**Why it stays**: MassTransit's `IPublishEndpoint` is the mechanism for publishing `BasketCheckoutEvent` to RabbitMQ. Wolverine handles in-process command dispatch; MassTransit handles inter-service integration events. They serve different layers and can coexist. Removing MassTransit from the handler would require reimplementing the RabbitMQ publishing via Wolverine's transport — that is outside the scope of this modernization.

The handler constructor `CheckoutBasketCommandHandler(IBasketRepository repository, IPublishEndpoint publishEndpoint)` is unchanged beyond stripping the interface inheritance.

### Rule 3 — Carter must be removed from `CheckoutBasketEndpoints`; the endpoint must use native MapGroup routing

**Old approach**: `CheckoutBasketEndpoints` implements `ICarterModule` and registers `POST /basket/checkout` directly on `app` (not on a group). Carter routes are absolute paths.

**New approach**: `CheckoutBasketEndpoints` becomes a `static` class with a `MapCheckoutBasketEndpoint(this RouteGroupBuilder group)` extension method. The route path changes from `/basket/checkout` to `/checkout` — the `/basket` prefix is already provided by the `MapGroup("/basket")` call in `BasketEndpoints.cs`. The final resolved URL remains `/basket/checkout`.

### Rule 4 — `BasketEndpoints.cs` must be updated to include the checkout endpoint

The aggregator created in the first spec wires three endpoints. A fourth call — `group.MapCheckoutBasketEndpoint()` — must be added so the checkout route is registered under the `/basket` group.

### Rule 5 — `ISender` must be replaced with `IMessageBus` in the checkout endpoint

`ISender sender` is replaced with `IMessageBus bus`. The dispatch call changes from `sender.Send(command)` to `await bus.InvokeAsync<CheckoutBasketResult>(command)`. Wolverine's FluentValidation middleware automatically applies `CheckoutBasketCommandValidator` before the handler runs.

### Rule 6 — The HTTP status code bug must be fixed

The current endpoint declares `.Produces<CheckoutBasketResponse>(StatusCodes.Status201Created)` but the handler returns `Results.Ok()` (`200 OK`). Since checkout is an action (not a resource creation at a new URI), `200 OK` is semantically correct. The annotation is corrected to `Produces<CheckoutBasketResponse>(StatusCodes.Status200OK)` and `TypedResults.Ok(response)` is used.

### Rule 7 — Mapster is kept for request-to-command and result-to-response mappings

`request.Adapt<CheckoutBasketCommand>()` and `result.Adapt<CheckoutBasketResponse>()` are kept per the Catalog.API and first Basket.API spec precedent.

---

## 4. Acceptance Criteria

The feature is complete when:

- [ ] `CheckoutBasketCommand` record no longer extends any interface
- [ ] `CheckoutBasketCommandHandler` class no longer implements any interface
- [ ] `using BuildingBlocks.CQRS;` is removed from `CheckoutBasketHandler.cs`
- [ ] `CheckoutBasketEndpoints` is a `static` class (not `ICarterModule`)
- [ ] `CheckoutBasketEndpoints` exposes `MapCheckoutBasketEndpoint(this RouteGroupBuilder group)` returning `RouteGroupBuilder`
- [ ] Route path inside the endpoint is `/checkout` (not `/basket/checkout`)
- [ ] `BasketEndpoints.cs` calls `group.MapCheckoutBasketEndpoint()`
- [ ] `ISender sender` is replaced with `IMessageBus bus` in the checkout endpoint lambda
- [ ] `sender.Send(command)` is replaced with `await bus.InvokeAsync<CheckoutBasketResult>(command)`
- [ ] Endpoint declares `Produces<CheckoutBasketResponse>(StatusCodes.Status200OK)` (not 201)
- [ ] `TypedResults.Ok(response)` is returned on success
- [ ] No file references `ICarterModule`, `ISender`, `ICommand`, `ICommandHandler` in the `CheckoutBasket/` folder
- [ ] `POST /basket/checkout` with a valid body publishes a `BasketCheckoutEvent`, deletes the basket, and returns `200 OK`
- [ ] `POST /basket/checkout` with an empty `UserName` returns `400 Bad Request` via Wolverine's FluentValidation middleware
- [ ] `POST /basket/checkout` when the basket does not exist returns the response resolved by open question 1

---

## 5. Out of Scope

- Changes to `BasketCheckoutDto` structure
- Changes to `BasketCheckoutEvent` or `IntegrationEvent` in `BuildingBlocks.Messaging`
- Changes to `BuildingBlocks.Messaging.MassTransit.Extensions` (`AddMessageBroker`)
- Replacing MassTransit with Wolverine's messaging transport for RabbitMQ publishing
- Changes to `Program.cs` beyond what is needed for the checkout endpoint registration (which is already handled by the first spec's `app.MapBasketEndpoints()` call)
- Changes to `GetBasket`, `StoreBasket`, or `DeleteBasket` — covered by the first spec
- Adding OpenTelemetry or Serilog
- Authentication or authorization
- Health check changes

---

## 6. Decisions *(resolved 2026-05-26)*

1. **CheckoutBasket not-found response strategy → Option A**
   Keep `CheckoutBasketResult(bool IsSuccess)`. The endpoint checks `result.IsSuccess` and returns `TypedResults.NotFound()` when `false`, `TypedResults.Ok(response)` when `true`. No discriminated union needed.

2. **HTTP response status on successful checkout → Option A**
   Return `200 OK`. Checkout publishes the event synchronously and confirms completion to the caller. `TypedResults.Ok(response)` is used; the endpoint declares `Produces<CheckoutBasketResponse>(StatusCodes.Status200OK)`.
