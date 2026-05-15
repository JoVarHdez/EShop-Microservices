# Catalog.API — Wolverine & Native Minimal API Modernization

## 1. Feature Summary

The Catalog.API microservice was built following a .NET 8 course that taught CQRS using MediatR, endpoint organization using Carter, and cross-cutting concerns via MediatR pipeline behaviors. While functional, these patterns introduce unnecessary abstraction layers, third-party routing dependencies, and exception-driven HTTP control flow — all of which conflict with .NET 10 best practices and Native AOT compatibility.

This modernization replaces MediatR with Wolverine (from the same JasperFx ecosystem as Marten), removes Carter in favor of native `MapGroup` routing, eliminates the CQRS interface wrappers and pipeline behaviors that were MediatR-specific, inlines query handlers directly into endpoints using `IQuerySession`, replaces exception-throwing patterns with `TypedResults` and discriminated command results, and removes all typed exception classes from `BuildingBlocks.Exceptions` in favor of Wolverine's built-in problem-details handling. The external API contract (all 6 endpoints, their URLs, verbs, and response shapes) remains identical — only the internal wiring changes.

---

## 2. Data Model / Entities

### Product *(unchanged)*
- `Id`: `Guid` — Marten document identity
- `Name`: `string` — display name
- `Categories`: `List<string>` — tag-style category strings
- `Description`: `string`
- `ImageUrl`: `string` — must be a valid absolute URI
- `Price`: `decimal` — must be greater than 0

### Command Records *(shape unchanged, interface removed)*

The three write-side command records keep their current properties but drop the `: ICommand<TResult>` inheritance:

- `CreateProductCommand(Name, Categories, Description, ImageUrl, Price)` → result: `CreateProductResult(Guid Id)`
- `UpdateProductCommand(Id, Name, Categories, Description, ImageUrl, Price)` → result: discriminated union of `UpdateProductResult(bool IsSuccess)` or `NotFound`
- `DeleteProductCommand(Guid ProductId)` → result: discriminated union of `DeleteProductResult(bool IsSuccess)` or `NotFound`

### Query Records *(deleted)*

The three query records (`GetProductByIdQuery`, `GetProductsQuery`, `GetProductByCategoryQuery`) are deleted entirely because their logic moves inline into the endpoint lambdas.

---

## 3. Business Rules & Constraints

> Each rule includes an explanation of how the old approach worked vs. the new approach, to support learning.

### Rule 1 — CQRS interfaces must be deleted from BuildingBlocks

**Old approach — MediatR interfaces**: MediatR requires every message to implement `IRequest<TResponse>` and every handler to implement `IRequestHandler<TRequest, TResponse>`. The project added a second layer of wrappers (`ICommand<TResponse>`, `ICommandHandler<TCommand, TResult>`, etc.) for semantic clarity. These compile down to the same thing — they're pure MediatR marker interfaces with no runtime behavior of their own.

**New approach — Wolverine convention**: Wolverine discovers handlers through **source-generation at compile time**. Any class with a `Handle()` or `HandleAsync()` method whose first parameter is the message type is recognized as a handler — no interface needed. Wolverine even allows injecting `IDocumentSession` directly as a method parameter (not just constructor), which Wolverine resolves per-invocation from its IoC container. The four files in `BuildingBlocks/CQRS/` (`ICommand.cs`, `ICommandHandler.cs`, `IQuery.cs`, `IQueryHandler.cs`) have no use after MediatR is removed, and the `: ICommand<TResult>` inheritance on each command record must be stripped.

### Rule 2 — MediatR pipeline behaviors must be deleted from BuildingBlocks

**Old approach — `IPipelineBehavior`**: MediatR implements cross-cutting concerns as a linked chain of `IPipelineBehavior<TRequest, TResponse>` decorators. Each behavior wraps the next via a `RequestHandlerDelegate`. They are registered globally in `AddMediatR()` using `AddOpenBehavior()`. Two behaviors exist: `ValidationBehavior` (runs all `IValidator<TRequest>` implementations, throws `ValidationException` on failure) and `LoggingBehavior` (logs start/end/elapsed time).

**New approach — Wolverine middleware**: Wolverine uses source-generated middleware that wraps handlers at **compile time**, not runtime reflection chains. For validation, the `WolverineFx` package includes first-class FluentValidation integration: calling `opts.UseFluentValidation()` during host setup causes Wolverine to automatically discover all registered `IValidator<TCommand>` implementations and apply them as middleware before each matching handler runs. On validation failure, Wolverine returns a `400 Bad Request` with a validation problem details body — no exception is thrown. For logging, Wolverine has built-in OpenTelemetry activity tracking, and `ILogger<T>` can be injected directly as a method parameter on the handler's `Handle` method. The two behavior files in `BuildingBlocks/Behaviors/` have no use after MediatR is removed.

### Rule 3 — Carter must be removed; endpoints must use native MapGroup routing

**Old approach — `ICarterModule`**: Before .NET 7, organizing minimal API routes required third-party solutions. Carter scanned the assembly at startup for all classes implementing `ICarterModule`, called `AddRoutes(IEndpointRouteBuilder app)` on each, and registered the routes. This was a valuable pattern in 2021. `AddCarter()` and `app.MapCarter()` are the entry points.

**New approach — `MapGroup` + static extension methods**: .NET 8+ provides `app.MapGroup("/products")` which prefixes routes, plus the convention of using a static class with an extension method on `IEndpointRouteBuilder` as the "module". The class implementing `ICarterModule` in each `*Endpoint.cs` file is replaced by a static class with a method like `MapProductsEndpoints(this IEndpointRouteBuilder app)`. `Program.cs` calls `app.MapProductsEndpoints()` directly, which is explicit, AOT-friendly, and eliminates a runtime assembly scan. Carter has zero value in .NET 10.

### Rule 4 — Query handlers must be deleted; queries must be inlined in endpoints using IQuerySession

**Old approach — Query handler via MediatR**: A read operation (e.g., GetProductById) goes through 3 layers: (1) endpoint constructs a query record and calls `sender.Send(query)`, (2) MediatR routes to the query handler, (3) the handler calls `session.LoadAsync<Product>(id)` and returns a result record. For a read with zero business logic beyond a null check, layers 1 and 2 add boilerplate with no benefit.

**New approach — Direct `IQuerySession` injection**: Marten exposes `IQuerySession` as a read-only, lightweight session that can be injected directly into a minimal API endpoint lambda. The endpoint calls Marten directly and returns an HTTP result. This is the "thin read model" side of CQRS — queries are simple data fetches, not orchestrations. `IQuerySession` (read-only) is preferred over `IDocumentSession` (read-write) for query endpoints because it signals intent and avoids accidental writes. The three query handler files (`GetProductByIdHandler.cs`, `GetProductsHandler.cs`, `GetProductByCategoryHandler.cs`) are deleted.

### Rule 5 — MediatR `ISender` must be replaced with Wolverine `IMessageBus` in command endpoints

In the three command endpoints (Create, Update, Delete), `ISender sender` is replaced with `IMessageBus bus` from `WolverineFx`. The dispatch call changes from `sender.Send(command)` to `bus.InvokeAsync<TResult>(command)`. `IMessageBus.InvokeAsync<T>` is Wolverine's equivalent of MediatR's `ISender.Send<T>` — it locates the handler, runs Wolverine middleware (including validation), and returns the result.

### Rule 6 — `Program.cs` service registrations must be updated

- Remove: `AddMediatR(...)` with all `AddOpenBehavior` calls
- Remove: `AddCarter()` and `app.MapCarter()`
- Add: `builder.Host.UseWolverine(opts => opts.UseFluentValidation())` — registers Wolverine with the host, enabling convention-based handler discovery and FluentValidation middleware
- Add: `.IntegrateWithWolverine()` chained on `AddMarten(...)` — from `WolverineFx.Marten`, this wires Marten's `IDocumentSession` into Wolverine's IoC so handlers can receive it as a method parameter and participates in Wolverine's durable outbox if needed
- Keep: `AddValidatorsFromAssembly(assembly)` — Wolverine's FluentValidation middleware picks them up from DI
- Keep: `AddExceptionHandler<CustomExceptionHandler>()` — simplified to handle only untyped 500-level exceptions; all typed exception pattern-matching is removed from it since those paths are eliminated
- Add: explicit `app.MapProductsEndpoints()` call replacing `app.MapCarter()`

### Rule 7 — Exception-driven HTTP responses must be replaced with TypedResults and discriminated command results

**Old approach — throw exception, catch in middleware**: When a handler finds `session.LoadAsync` returns `null`, it throws `ProductNotFoundException` (extends `NotFoundException` from BuildingBlocks). This unwinds the call stack, triggers ASP.NET Core's exception handler middleware, which calls `CustomExceptionHandler.TryHandleAsync`, which pattern-matches the exception type, builds a `ProblemDetails` object, and writes it to the response. This is a full exception lifecycle for a predictable outcome.

**New approach for query endpoints — inline `TypedResults.NotFound()`**: Since query handlers are inlined into endpoints (Rule 4), the null check lives directly in the endpoint lambda. `TypedResults` (vs `Results`) is the strongly-typed variant introduced in .NET 7 that enables compile-time type checking and OpenAPI response inference without `Produces<T>()` attributes. `if (product is null) return TypedResults.NotFound()` replaces the throw entirely.

**New approach for command handlers (Update, Delete) — discriminated result**: Command handlers must not return HTTP types (`IResult`) because that couples domain logic to HTTP concerns. Instead, `UpdateProductCommandHandler` and `DeleteProductCommandHandler` return a discriminated union — a sealed type hierarchy (e.g., `UpdateProductResult` as the success case, a `NotFound` marker as the failure case). The endpoint receives this union, pattern-matches on it, and returns the appropriate `TypedResults` response. This keeps HTTP concerns in the endpoint layer where they belong while eliminating the exception throw from the handler.

**Typed exceptions deleted from BuildingBlocks**: With all callsites eliminated, `NotFoundException`, `BadRequestException`, `InternalServerException`, and `ProductNotFoundException` are all deleted. `CustomExceptionHandler` is kept but stripped of its typed pattern-matching arms, leaving only the generic 500 fallback — it remains a safety net for truly unexpected failures (e.g., database unreachable, unhandled Wolverine errors).

---

## 4. Acceptance Criteria

The feature is complete when:

- [ ] `BuildingBlocks/CQRS/` folder is deleted (all 4 files removed)
- [ ] `BuildingBlocks/Behaviors/` folder is deleted (both behavior files removed)
- [ ] `Carter` NuGet reference is removed from `Catalog.API.csproj`
- [ ] `MediatR` NuGet reference is removed from `BuildingBlocks.csproj`
- [ ] `FluentValidation.AspNetCore` NuGet reference is removed from `BuildingBlocks.csproj`
- [ ] No file in the solution references `ICarterModule`, `ICommand`, `ICommandHandler`, `IQuery`, `IQueryHandler`, `ISender`, or `IPipelineBehavior`
- [ ] `WolverineFx` and `WolverineFx.Marten` are registered in `Program.cs` with `UseFluentValidation()` and `IntegrateWithWolverine()`
- [ ] All 6 endpoint files no longer extend `ICarterModule`; they are static extension methods registered via `app.MapGroup`
- [ ] `GET /products` returns a paged list of products
- [ ] `GET /products/{id}` with a valid existing ID returns `200 OK` with product data
- [ ] `GET /products/{id}` with a non-existent ID returns `404 Not Found`
- [ ] `GET /products/category/{categoryId}` returns products matching the category
- [ ] `POST /products` with a valid body creates a product and returns `201 Created`
- [ ] `POST /products` with an invalid body (e.g., empty name) returns `400 Bad Request` via Wolverine's FluentValidation middleware
- [ ] `PUT /products/{id}` with a valid body updates the product and returns `200 OK`
- [ ] `DELETE /products/{id}` deletes the product and returns `200 OK`
- [ ] `GET /health` returns the Npgsql health check result
- [ ] The three query handler files (`GetProductByIdHandler.cs`, `GetProductsHandler.cs`, `GetProductByCategoryHandler.cs`) are deleted
- [ ] `GET /products/{id}` returns `TypedResults.NotFound()` directly from the endpoint when the product does not exist (no exception thrown)
- [ ] `PUT /products/{id}` with a non-existent ID returns `404 Not Found`
- [ ] `DELETE /products/{id}` with a non-existent ID returns `404 Not Found`
- [ ] `UpdateProductCommandHandler` and `DeleteProductCommandHandler` return a discriminated result type; the endpoint pattern-matches it to produce the HTTP response
- [ ] `ProductNotFoundException`, `NotFoundException`, `BadRequestException`, and `InternalServerException` are all deleted
- [ ] `CustomExceptionHandler` retains only the generic 500 fallback arm; all typed exception pattern-matching arms are removed
- [ ] Mapster (`request.Adapt<TCommand>()`) is kept for all endpoint request-to-command mappings

---

## 5. Out of Scope

The following are explicitly NOT part of this modernization:

- Changes to `Product.cs` model structure
- Changes to `CatalogInitialData.cs` seed data
- Changes to Docker, docker-compose, or deployment configuration
- Adding new endpoints or changing the API surface (URLs, verbs, request/response shapes stay identical)
- Upgrading Marten beyond its current version
- Switching from `IDocumentSession` to Wolverine's durable outbox for commands (Marten's transactional session is sufficient for now)
- Adding OpenTelemetry or Serilog structured logging beyond what Wolverine provides by default
- Health check UI endpoint (`/health-ui`) — only the existing `/health` check endpoint is in scope
- Authentication or authorization
- Multi-service messaging (Wolverine's messaging/transport features — only in-process command dispatch is in scope here)

---

## 6. Decisions

All open questions resolved on 2026-05-15:

1. **Exception handler strategy → Option B**: Keep `CustomExceptionHandler` as a generic 500 safety net, but remove all typed exception classes (`NotFoundException`, `BadRequestException`, `InternalServerException`, `ProductNotFoundException`). Typed arms in `CustomExceptionHandler` are deleted alongside the exception types they matched. Wolverine's FluentValidation middleware owns `400` responses; endpoints own `404` responses via `TypedResults`.

2. **Command handler not-found strategy → Option B**: `UpdateProductCommandHandler` and `DeleteProductCommandHandler` return a discriminated result union instead of throwing. The endpoint receives the union and pattern-matches it to either `TypedResults.Ok(result)` or `TypedResults.NotFound()`. HTTP concerns remain in the endpoint layer; domain logic remains in the handler.

3. **Mapster → Option A**: Mapster is kept for all endpoint request-to-command mappings. The `request.Adapt<TCommand>()` pattern is preserved as-is across all command endpoints.
