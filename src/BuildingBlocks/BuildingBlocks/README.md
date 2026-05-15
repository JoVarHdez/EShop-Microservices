# BuildingBlocks

A shared class library providing cross-cutting infrastructure for the eShop microservices solution. It contains reusable CQRS abstractions, MediatR pipeline behaviors, and standardized exception handling.

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
| WolverineFx | 5.39.1 | Messaging and command bus |
| WolverineFx.Marten | 5.39.1 | Wolverine + Marten integration |

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
└── Exceptions/                  # Domain exceptions and handler
    ├── BadRequestException.cs
    ├── InternalServerException.cs
    ├── NotFoundException.cs
    └── Handler/
        └── CustomExceptionHandler.cs
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

## Exception Handling

### Exception Types

| Exception | HTTP Status | Usage |
|---|---|---|
| `BadRequestException` | 400 | Invalid input that doesn't pass business rules |
| `NotFoundException` | 404 | Entity not found by ID or criteria |
| `InternalServerException` | 500 | Unexpected server-side failures |

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

app.UseExceptionHandler();
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
