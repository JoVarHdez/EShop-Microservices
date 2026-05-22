# BuildingBlocks

A shared class library providing cross-cutting infrastructure for the eShop microservices solution. It contains reusable CQRS abstractions, MediatR pipeline behaviors, standardized exception handling, and pagination utilities.

## Target Framework

- .NET 10.0

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| MediatR | 14.1.0 | CQRS mediator pattern |
| FluentValidation | 12.1.1 | Request validation |
| FluentValidation.AspNetCore | 11.3.1 | ASP.NET Core integration |
| FluentValidation.DependencyInjectionExtensions | 12.1.1 | DI registration helpers |
| Mapster | 10.0.7 | Object mapping |
| `WolverineFx` | 5.39.1 | Wolverine messaging and command bus (used by services migrating away from MediatR) |
| `WolverineFx.Marten` | 5.39.1 | Wolverine + Marten integration for event-sourced handlers |

---

## Project Structure

```
BuildingBlocks/
├── CQRS/                        # Command/Query interfaces
│   ├── ICommand.cs
│   ├── ICommandHandler.cs
│   ├── IQuery.cs
│   └── IQueryHandler.cs
├── Behaviors/                   # MediatR pipeline behaviors
│   ├── LoggingBehavior.cs
│   └── ValidationBehavior.cs
├── Exceptions/                  # Domain exceptions and handler
│   ├── BadRequestException.cs
│   ├── InternalServerException.cs
│   ├── NotFoundException.cs
│   └── Handler/
│       └── CustomExceptionHandler.cs
└── Pagination/                  # Pagination helpers
    ├── PaginationRequest.cs
    └── PaginatedResult.cs
```

---

## CQRS

Thin wrappers around MediatR that enforce a consistent command/query separation pattern across all services.

### Commands

```csharp
// A command that returns no value
public record CreateProductCommand(string Name, decimal Price) : ICommand;

// A command that returns a typed result
public record CreateProductCommand(string Name) : ICommand<CreateProductResponse>;
```

### Command Handlers

```csharp
// Handler for a void command
public class CreateProductHandler : ICommandHandler<CreateProductCommand>
{
    public async Task<Unit> Handle(CreateProductCommand command, CancellationToken ct) { ... }
}

// Handler for a command with a response
public class CreateProductHandler : ICommandHandler<CreateProductCommand, CreateProductResponse>
{
    public async Task<CreateProductResponse> Handle(CreateProductCommand command, CancellationToken ct) { ... }
}
```

### Queries

```csharp
public record GetProductByIdQuery(Guid Id) : IQuery<GetProductByIdResult>;

public class GetProductByIdHandler : IQueryHandler<GetProductByIdQuery, GetProductByIdResult>
{
    public async Task<GetProductByIdResult> Handle(GetProductByIdQuery query, CancellationToken ct) { ... }
}
```

---

## Pipeline Behaviors

MediatR pipeline behaviors that run automatically for every request processed through the mediator.

### LoggingBehavior

Logs the start and end of every request, including request name, response type, and input data. If a request takes longer than **3 seconds**, a performance warning is emitted.

```
[START] Handle Request=CreateProductCommand - Response=Unit - RequestData=...
[PERFORMANCE] The request CreateProductCommand took 5 seconds to process
[END] Handle CreateProductCommand with Unit
```

### ValidationBehavior

Runs all registered `IValidator<TRequest>` instances before the handler is invoked. Only applies to requests that implement `ICommand<TResponse>`. Throws a `FluentValidation.ValidationException` if any validators report failures, which is then caught by `CustomExceptionHandler` and returned as an HTTP 400 response.

---

## Pagination

Two lightweight types for applying consistent paging across all query endpoints.

### PaginationRequest

A record passed in as part of a query to specify the desired page.

```csharp
public record PaginationRequest(int PageIndex = 0, int PageSize = 10);
```

| Property | Default | Description |
|---|---|---|
| `PageIndex` | `0` | Zero-based page index |
| `PageSize` | `10` | Number of items per page |

### PaginatedResult\<TEntity\>

The standard envelope returned by paginated query handlers.

```csharp
public class PaginatedResult<TEntity>(int pageIndex, int pageSize, long count, IEnumerable<TEntity> data)
    where TEntity : class
```

| Property | Type | Description |
|---|---|---|
| `PageIndex` | `int` | Current page index |
| `PageSize` | `int` | Page size used for this result |
| `Count` | `long` | Total number of matching records |
| `Data` | `IEnumerable<TEntity>` | Items for the current page |

**Usage example:**

```csharp
public record GetProductsQuery(PaginationRequest Pagination) : IQuery<PaginatedResult<ProductDto>>;

public class GetProductsHandler : IQueryHandler<GetProductsQuery, PaginatedResult<ProductDto>>
{
    public async Task<PaginatedResult<ProductDto>> Handle(GetProductsQuery query, CancellationToken ct)
    {
        var (pageIndex, pageSize) = query.Pagination;
        var total = await _repo.CountAsync(ct);
        var items = await _repo.GetPageAsync(pageIndex, pageSize, ct);
        return new PaginatedResult<ProductDto>(pageIndex, pageSize, total, items);
    }
}
```

---

## Exception Handling

### Exception Types

| Exception | HTTP Status | Usage |
|---|---|---|
| `BadRequestException` | 400 | Invalid input that doesn't pass business rules |
| `FluentValidation.ValidationException` | 400 | Validation failures raised by `ValidationBehavior` |
| `NotFoundException` | 404 | Entity not found by ID or criteria |
| `InternalServerException` | 500 | Unexpected server-side failures |
| *(any other)* | 500 | Unhandled exceptions — caught as last resort |

```csharp
// NotFoundException
throw new NotFoundException("Product", productId);
// message: "Product (ID: <guid>)"

// BadRequestException with details
throw new BadRequestException("Validation failed", "Price must be greater than zero");
```

### CustomExceptionHandler

Implements `IExceptionHandler` (ASP.NET Core 8+ problem details middleware). Maps known exceptions to [RFC 7807 `ProblemDetails`](https://datatracker.ietf.org/doc/html/rfc7807) responses and attaches a `traceId` for correlation. Validation errors are included in the `ValidationErrors` extension field.

**Register in `Program.cs`:**

```csharp
builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();

app.UseExceptionHandler(options => { });
```

**Example error response:**

```json
{
  "title": "NotFoundException",
  "status": 404,
  "detail": "Product (ID: 3fa85f64-5717-4562-b3fc-2c963f66afa6)",
  "instance": "/api/products/3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "traceId": "00-abc123..."
}
```
