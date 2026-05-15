# Implementation Plan: Catalog.API — Wolverine & Native Minimal API Modernization

> Spec: [`docs/specs/20260515-catalogApiWolverineModernization.md`](../specs/20260515-catalogApiWolverineModernization.md)

**TL;DR** — Work top-down through 5 phases: delete dead code first (zero risk, unblocks everything), then simplify the exception handler, then rewrite the domain handler files, then rewrite all 6 endpoint files, then wire the host. Each phase only touches files it owns; no phase depends on a later one.

---

## Phase 1 — Delete dead code & remove packages

All steps in this phase are **independent and can run in parallel**.

### 1.1 — Delete `BuildingBlocks/CQRS/` folder (4 files)

These are pure MediatR marker interface wrappers with no runtime behavior. Wolverine requires no interfaces.

- `src/BuildingBlocks/BuildingBlocks/CQRS/ICommand.cs`
- `src/BuildingBlocks/BuildingBlocks/CQRS/ICommandHandler.cs`
- `src/BuildingBlocks/BuildingBlocks/CQRS/IQuery.cs`
- `src/BuildingBlocks/BuildingBlocks/CQRS/IQueryHandler.cs`

### 1.2 — Delete `BuildingBlocks/Behaviors/` folder (2 files)

`IPipelineBehavior` is a MediatR-specific abstraction. Validation is replaced by Wolverine's `UseFluentValidation()` middleware; logging is replaced by injecting `ILogger<T>` as a handler method parameter.

- `src/BuildingBlocks/BuildingBlocks/Behaviors/ValidationBehavior.cs`
- `src/BuildingBlocks/BuildingBlocks/Behaviors/LoggingBehavior.cs`

### 1.3 — Delete query handler files (3 files)

Simple reads with no business logic are inlined directly into the endpoint lambdas using `IQuerySession`. The query record *types* inside these files (`GetProductByIdQuery`, etc.) are also gone; the response record types (`GetProductByIdResponse`, etc.) stay in the corresponding `*Endpoint.cs` files.

- `src/Services/Catalog/Catalog.API/Products/GetProductById/GetProductByIdHandler.cs`
- `src/Services/Catalog/Catalog.API/Products/GetProducts/GetProductsHandler.cs`
- `src/Services/Catalog/Catalog.API/Products/GetProductByCategory/GetProductByCategoryHandler.cs`

### 1.4 — Delete typed exception files (4 files)

All callsites are eliminated in later phases. `CustomExceptionHandler` loses its typed pattern-matching arms in Phase 2.

- `src/BuildingBlocks/BuildingBlocks/Exceptions/NotFoundException.cs`
- `src/BuildingBlocks/BuildingBlocks/Exceptions/BadRequestException.cs`
- `src/BuildingBlocks/BuildingBlocks/Exceptions/InternalServerException.cs`
- `src/Services/Catalog/Catalog.API/Exceptions/ProductNotFoundException.cs`

### 1.5 — Remove NuGet package references

**`src/BuildingBlocks/BuildingBlocks/BuildingBlocks.csproj`** — remove both references:
- `MediatR`
- `FluentValidation.AspNetCore` (deprecated since v12; DI extensions package already present)

**`src/Services/Catalog/Catalog.API/Catalog.API.csproj`** — remove:
- `Carter`

---

## Phase 2 — Simplify `CustomExceptionHandler` *(depends on Phase 1.4)*

**`src/BuildingBlocks/BuildingBlocks/Exceptions/Handler/CustomExceptionHandler.cs`**

Remove the typed switch expression entirely. Replace it with a single direct `ProblemDetails` construction at `Status 500`. The handler becomes a generic last-resort safety net for truly unexpected errors (e.g., database unreachable, unhandled Wolverine runtime errors). No `FluentValidation` using needed; remove it.

The simplified `TryHandleAsync` body:
1. Build `ProblemDetails` with `Status 500`, `exception.Message` as `Detail`, `exception.GetType().Name` as `Title`, `httpContext.Request.Path` as `Instance`
2. Add `traceId` extension
3. Write as JSON and return `true`

---

## Phase 3 — Rewrite command handler files *(depends on Phase 1.1)*

All three handler files are in `src/Services/Catalog/Catalog.API/Products/`. Changes per file:

### 3.1 — `CreateProduct/CreateProductHandler.cs`

Minimal change — only interfaces are stripped. Handler body and constructor injection are untouched; Wolverine discovers handlers by the `Handle` method name convention, and constructor-injected `IDocumentSession` continues to work through DI.

- Remove `using BuildingBlocks.CQRS;`
- Strip `: ICommand<CreateProductResult>` from the `CreateProductCommand` record declaration
- Strip `: ICommandHandler<CreateProductCommand, CreateProductResult>` from `CreateProductCommandHandler`

### 3.2 — `UpdateProduct/UpdateProductHandler.cs`

Strip interfaces + introduce discriminated union + replace the exception throw with a union return.

- Remove `using BuildingBlocks.CQRS;` and `using Catalog.API.Exceptions;`
- Strip `: ICommand<UpdateProductResult>` from `UpdateProductCommand`
- Strip `: ICommandHandler<UpdateProductCommand, UpdateProductResult>` from `UpdateProductCommandHandler`
- **Add** above the handler class:
  ```csharp
  public abstract record UpdateProductCommandResult;
  ```
- **Modify** `UpdateProductResult` to extend it:
  ```csharp
  public record UpdateProductResult(bool IsSuccess) : UpdateProductCommandResult;
  ```
- **Add** a not-found marker:
  ```csharp
  public record UpdateProductNotFound : UpdateProductCommandResult;
  ```
- Change handler return type from `Task<UpdateProductResult>` → `Task<UpdateProductCommandResult>`
- Replace `throw new ProductNotFoundException(request.Id)` with `return new UpdateProductNotFound()`

### 3.3 — `DeleteProduct/DeleteProductHandler.cs`

Strip interfaces + introduce discriminated union + **add existence check** (the original handler blindly deleted without checking).

- Remove `using BuildingBlocks.CQRS;`
- Strip interfaces from `DeleteProductCommand` record and `DeleteProductCommandHandler` class
- **Add** discriminated union records (same pattern as Update):
  ```csharp
  public abstract record DeleteProductCommandResult;
  public record DeleteProductResult(bool IsSuccess) : DeleteProductCommandResult;
  public record DeleteProductNotFound : DeleteProductCommandResult;
  ```
- Change handler return type to `Task<DeleteProductCommandResult>`
- **Before** `session.Delete<Product>(...)`, add:
  ```csharp
  var product = await session.LoadAsync<Product>(request.ProductId, cancellationToken);
  if (product is null) return new DeleteProductNotFound();
  ```

---

## Phase 4 — Rewrite endpoint files *(depends on Phase 3)*

**Pattern for every endpoint file**: the `ICarterModule` class becomes a `static` class; `AddRoutes(IEndpointRouteBuilder app)` becomes `MapXxxEndpoint(this RouteGroupBuilder group)`; route paths drop the `/products` prefix (moved to the `MapGroup` in Step 4.1).

### 4.1 — Create `Products/ProductsEndpoints.cs` *(new file)*

```
src/Services/Catalog/Catalog.API/Products/ProductsEndpoints.cs
```

Static class `ProductsEndpoints` with a single extension method:
```csharp
public static IEndpointRouteBuilder MapProductsEndpoints(this IEndpointRouteBuilder app)
```
Inside: `var group = app.MapGroup("/products")`, then call each endpoint's extension method on the group:
- `group.MapCreateProductEndpoint()`
- `group.MapGetProductsEndpoint()`
- `group.MapGetProductByIdEndpoint()`
- `group.MapGetProductByCategoryEndpoint()`
- `group.MapUpdateProductEndpoint()`
- `group.MapDeleteProductEndpoint()`

Return `app`.

### 4.2 — `CreateProduct/CreateProductEndpoint.cs`

Write-side endpoint — uses `IMessageBus`.

- `public class CreateProductEndpoint : ICarterModule` → `public static class CreateProductEndpoint`
- `public void AddRoutes(IEndpointRouteBuilder app)` → `public static RouteGroupBuilder MapCreateProductEndpoint(this RouteGroupBuilder group)`
- Route path: `/products` → `/`
- Lambda parameters: replace `ISender sender` with `IMessageBus bus`
- Dispatch: `sender.Send(command)` → `bus.InvokeAsync<CreateProductResult>(command)`
- Response construction and Mapster call unchanged
- Remove `using Carter;`, `using MediatR;`; add `using Wolverine;`
- Return `group`

### 4.3 — `GetProducts/GetProductsEndpoint.cs`

Read-side endpoint — query handler deleted; inline with `IQuerySession`.

- Same class/method transformation
- Route path: `/products` → `/`
- Remove `ISender sender` and the query record dispatch entirely (query types are deleted)
- Lambda parameters: add `IQuerySession session` (alongside `[AsParameters] GetProductsRequest request`)
- Inline the Marten call: `session.Query<Product>().ToPagedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, ct)`
- Return `TypedResults.Ok(new GetProductResponse(products))`
- Remove `using MediatR;`; add `using Marten;` and `using Marten.Pagination;`

### 4.4 — `GetProductById/GetProductByIdEndpoint.cs`

Read-side endpoint — inline with `IQuerySession` + `TypedResults.NotFound()`.

- Same class/method transformation
- Route path: `/products/{id}` → `/{id}`
- Lambda parameters: `(Guid id, IQuerySession session)`
- Inline: `await session.LoadAsync<Product>(id, ct)`
- Return `product is null ? TypedResults.NotFound() : TypedResults.Ok(new GetProductByIdResponse(product))`
- Remove `using MediatR;`; add `using Marten;`

### 4.5 — `GetProductByCategory/GetProductByCategoryEndpoint.cs`

Read-side endpoint — inline with `IQuerySession`.

- Same class/method transformation
- Route path: `/products/category/{categoryId}` → `/category/{categoryId}`
- Lambda parameters: `(string categoryId, IQuerySession session)`
- Inline: `session.Query<Product>().Where(p => p.Categories.Contains(categoryId)).ToListAsync(ct)`
- Return `TypedResults.Ok(new GetProductByCategoryResponse(products))`
- Remove `using MediatR;`; add `using Marten;`

### 4.6 — `UpdateProduct/UpdateProductEndpoint.cs`

Write-side endpoint — `IMessageBus` + pattern match on discriminated result.

- Same class/method transformation
- Route path: `/products/{id}` → `/{id}`
- Lambda parameters: replace `ISender sender` with `IMessageBus bus`
- Dispatch: `await bus.InvokeAsync<UpdateProductCommandResult>(command)`
- **Add pattern match** on result:
  ```csharp
  return result switch {
      UpdateProductResult r => TypedResults.Ok(r.Adapt<UpdateProductResponse>()),
      UpdateProductNotFound => TypedResults.NotFound(),
      _                     => TypedResults.Problem("Unexpected result", statusCode: 500)
  };
  ```
- `.ProducesProblem(StatusCodes.Status404NotFound)` is already present; keep it
- Remove `using Carter;`, `using MediatR;`; add `using Wolverine;`

### 4.7 — `DeleteProduct/DeleteProductEndpoint.cs`

Write-side endpoint — `IMessageBus` + pattern match on discriminated result.

- Same class/method transformation
- Route path: `/products/{id}` → `/{id}`
- Lambda parameters: replace `ISender sender` with `IMessageBus bus`
- Dispatch: `await bus.InvokeAsync<DeleteProductCommandResult>(command)`
- **Add pattern match** on result:
  ```csharp
  return result switch {
      DeleteProductResult r => TypedResults.Ok(r.Adapt<DeleteProductResponse>()),
      DeleteProductNotFound => TypedResults.NotFound(),
      _                     => TypedResults.Problem("Unexpected result", statusCode: 500)
  };
  ```
- Remove `using Carter;`, `using MediatR;`; add `using Wolverine;`

---

## Phase 5 — Update `Program.cs` *(depends on Phase 4)*

**`src/Services/Catalog/Catalog.API/Program.cs`**

### 5.1 — Remove

- `using Carter;`
- The entire `builder.Services.AddMediatR(config => { ... });` block (4 lines)
- `builder.Services.AddCarter();`
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
})
.UseLightweightSessions()
.IntegrateWithWolverine();
```

### 5.4 — Register endpoints

Replace `app.MapCarter()` with:
```csharp
app.MapProductsEndpoints();
```

---

## Relevant Files

| Action | File |
|--------|------|
| DELETE | `src/BuildingBlocks/BuildingBlocks/CQRS/ICommand.cs` |
| DELETE | `src/BuildingBlocks/BuildingBlocks/CQRS/ICommandHandler.cs` |
| DELETE | `src/BuildingBlocks/BuildingBlocks/CQRS/IQuery.cs` |
| DELETE | `src/BuildingBlocks/BuildingBlocks/CQRS/IQueryHandler.cs` |
| DELETE | `src/BuildingBlocks/BuildingBlocks/Behaviors/ValidationBehavior.cs` |
| DELETE | `src/BuildingBlocks/BuildingBlocks/Behaviors/LoggingBehavior.cs` |
| DELETE | `src/Services/Catalog/Catalog.API/Products/GetProductById/GetProductByIdHandler.cs` |
| DELETE | `src/Services/Catalog/Catalog.API/Products/GetProducts/GetProductsHandler.cs` |
| DELETE | `src/Services/Catalog/Catalog.API/Products/GetProductByCategory/GetProductByCategoryHandler.cs` |
| DELETE | `src/BuildingBlocks/BuildingBlocks/Exceptions/NotFoundException.cs` |
| DELETE | `src/BuildingBlocks/BuildingBlocks/Exceptions/BadRequestException.cs` |
| DELETE | `src/BuildingBlocks/BuildingBlocks/Exceptions/InternalServerException.cs` |
| DELETE | `src/Services/Catalog/Catalog.API/Exceptions/ProductNotFoundException.cs` |
| MODIFY | `src/BuildingBlocks/BuildingBlocks/BuildingBlocks.csproj` |
| MODIFY | `src/Services/Catalog/Catalog.API/Catalog.API.csproj` |
| MODIFY | `src/BuildingBlocks/BuildingBlocks/Exceptions/Handler/CustomExceptionHandler.cs` |
| MODIFY | `src/Services/Catalog/Catalog.API/Products/CreateProduct/CreateProductHandler.cs` |
| MODIFY | `src/Services/Catalog/Catalog.API/Products/UpdateProduct/UpdateProductHandler.cs` |
| MODIFY | `src/Services/Catalog/Catalog.API/Products/DeleteProduct/DeleteProductHandler.cs` |
| MODIFY | `src/Services/Catalog/Catalog.API/Products/CreateProduct/CreateProductEndpoint.cs` |
| MODIFY | `src/Services/Catalog/Catalog.API/Products/GetProductById/GetProductByIdEndpoint.cs` |
| MODIFY | `src/Services/Catalog/Catalog.API/Products/GetProducts/GetProductsEndpoint.cs` |
| MODIFY | `src/Services/Catalog/Catalog.API/Products/GetProductByCategory/GetProductByCategoryEndpoint.cs` |
| MODIFY | `src/Services/Catalog/Catalog.API/Products/UpdateProduct/UpdateProductEndpoint.cs` |
| MODIFY | `src/Services/Catalog/Catalog.API/Products/DeleteProduct/DeleteProductEndpoint.cs` |
| MODIFY | `src/Services/Catalog/Catalog.API/Program.cs` |
| CREATE | `src/Services/Catalog/Catalog.API/Products/ProductsEndpoints.cs` |

---

## Verification

1. **Build**: `dotnet build src/eshop-microservies.slnx` must succeed with 0 errors.
2. **Grep check**: search for `ICarterModule`, `ISender`, `ICommand`, `IPipelineBehavior`, `AddMediatR`, `AddCarter`, `NotFoundException`, `BadRequestException`, `ProductNotFoundException` across `src/` — must return no results.
3. **HTTP smoke tests** (run against `docker-compose up`):
   - `POST /products` valid body → `201 Created`
   - `POST /products` empty `Name` field → `400 Bad Request` with FluentValidation errors in body
   - `GET /products` → `200 OK` paged list
   - `GET /products/{existing-id}` → `200 OK`
   - `GET /products/{non-existent-id}` → `404 Not Found`
   - `GET /products/category/Category1` → `200 OK`
   - `PUT /products/{existing-id}` valid body → `200 OK`
   - `PUT /products/{non-existent-id}` → `404 Not Found`
   - `DELETE /products/{existing-id}` → `200 OK`
   - `DELETE /products/{non-existent-id}` → `404 Not Found`
   - `GET /health` → `200 OK` with Npgsql status

---

## Decisions (resolved in spec)

- **Exceptions** — `CustomExceptionHandler` kept as generic 500 fallback; all typed exception classes deleted.
- **Command not-found** — `UpdateProductCommandHandler` and `DeleteProductCommandHandler` return sealed discriminated union results; endpoint owns the HTTP mapping via pattern match.
- **Mapster** — kept for all `request.Adapt<TCommand>()` and `result.Adapt<TResponse>()` calls in endpoints.
