# Implementation Plan: Discount.Grpc — .NET 10 Real-World Practices Modernization

> Spec: [`docs/specs/20260520-discountGrpcModernization.md`](../specs/20260520-discountGrpcModernization.md)

**TL;DR** — Six phases: fix the async migration bug first (zero-risk, standalone), then apply the `required` keyword to the domain model, then build the repository layer (interface + implementation), then add the `ValidationInterceptor` and FluentValidation validators, then rewrite `DiscountService` to delegate through the new abstractions, and finally wire all new registrations into `Program.cs`. Phases 2, 3, and 4 are independent of each other and can run in parallel. Phase 5 depends on Phase 3 (repository interface). Phase 6 depends on everything.

---

## Relevant Files

| Action | File |
|--------|------|
| MODIFY | `src/Services/Discount/Discount.Grpc/Data/Extensions.cs` |
| MODIFY | `src/Services/Discount/Discount.Grpc/Models/Coupon.cs` |
| CREATE | `src/Services/Discount/Discount.Grpc/Data/IDiscountRepository.cs` |
| CREATE | `src/Services/Discount/Discount.Grpc/Data/DiscountRepository.cs` |
| CREATE | `src/Services/Discount/Discount.Grpc/Interceptors/ValidationInterceptor.cs` |
| CREATE | `src/Services/Discount/Discount.Grpc/Validators/GetDiscountRequestValidator.cs` |
| CREATE | `src/Services/Discount/Discount.Grpc/Validators/CreateDiscountRequestValidator.cs` |
| CREATE | `src/Services/Discount/Discount.Grpc/Validators/UpdateDiscountRequestValidator.cs` |
| MODIFY | `src/Services/Discount/Discount.Grpc/Services/DiscountService.cs` |
| MODIFY | `src/Services/Discount/Discount.Grpc/Program.cs` |
| MODIFY | `src/Services/Discount/Discount.Grpc/Discount.Grpc.csproj` |

---

## Phase 1 — Fix the async migration bug *(no dependencies — safe to start immediately)*

A single-line fix that eliminates the fire-and-forget async call that allows the service to start against an un-migrated schema. This phase is entirely isolated from every other change.

### 1.1 — Replace `MigrateAsync()` with `Migrate()` in `Data/Extensions.cs`

`MigrateAsync()` returns a `Task` that is never awaited, silently swallowing migration exceptions and creating a race condition between migrations and the first gRPC request. The synchronous overload blocks startup until migrations fully apply, which is the correct behavior for this startup-time extension.

**File**: `src/Services/Discount/Discount.Grpc/Data/Extensions.cs`

- Replace `dbContext.Database.MigrateAsync();` with `dbContext.Database.Migrate();`
- Remove the `async` context if `MigrateAsync` was the only async call (the method `UseMigration` is synchronous and returns `IApplicationBuilder` — no other changes needed)
- **Unchanged**: the `using var scope` / `using var dbContext` acquisition pattern, the return statement

---

## Phase 2 — Apply `required` keyword to the domain model *(parallel with Phases 3 and 4)*

A purely additive, two-property change to `Coupon.cs` that strengthens compile-time null safety. Independent of every other phase.

### 2.1 — Add `required` to `Coupon.ProductName` and `Coupon.Description`

The `= default!` null-forgiving operator suppresses the compiler warning but provides zero runtime enforcement. The C# 11 `required` keyword makes the compiler enforce initialization at every object-construction call site with `CS9035`.

**File**: `src/Services/Discount/Discount.Grpc/Models/Coupon.cs`

- Replace `public string ProductName { get; set; } = default!;` with `public required string ProductName { get; set; }`
- Replace `public string Description { get; set; } = default!;` with `public required string Description { get; set; }`
- **Unchanged**: `Id` (`int`), `Amount` (`int`), namespace, class name

> **Learning note**: `required` does not conflict with EF Core. EF Core uses reflection to populate navigation/scalar properties from database rows, bypassing object initializers, so it does not trigger the `CS9035` requirement.

---

## Phase 3 — Create the repository layer *(parallel with Phases 2 and 4)*

Extract all EF Core data access out of `DiscountService` into a dedicated repository. This phase produces two new files; the existing `DiscountService.cs` is not touched until Phase 5.

### 3.1 — Create `Data/IDiscountRepository.cs`

Define the four-method contract. All read/write operations are nullable-aware to allow callers to handle not-found without throwing.

**File**: `src/Services/Discount/Discount.Grpc/Data/IDiscountRepository.cs`

```csharp
namespace Discount.Grpc.Data;

public interface IDiscountRepository
{
    Task<Coupon?> GetDiscountAsync(string productName, CancellationToken cancellationToken = default);
    Task<Coupon> CreateDiscountAsync(Coupon coupon, CancellationToken cancellationToken = default);
    Task<Coupon?> UpdateDiscountAsync(Coupon coupon, CancellationToken cancellationToken = default);
    Task<bool> DeleteDiscountAsync(string productName, CancellationToken cancellationToken = default);
}
```

### 3.2 — Create `Data/DiscountRepository.cs`

Implement `IDiscountRepository`. All four methods migrate directly from the matching bodies in `DiscountService` with two key changes: `UpdateDiscountAsync` uses find-then-update, and `CreateDiscountAsync` checks for duplicates.

**File**: `src/Services/Discount/Discount.Grpc/Data/DiscountRepository.cs`

```csharp
using Discount.Grpc.Models;
using Microsoft.EntityFrameworkCore;

namespace Discount.Grpc.Data;

public class DiscountRepository(DiscountContext dbContext) : IDiscountRepository
{
    public async Task<Coupon?> GetDiscountAsync(string productName, CancellationToken cancellationToken = default)
    {
        return await dbContext.Coupons
            .FirstOrDefaultAsync(c => c.ProductName == productName, cancellationToken);
    }

    public async Task<Coupon> CreateDiscountAsync(Coupon coupon, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Coupons
            .FirstOrDefaultAsync(c => c.ProductName == coupon.ProductName, cancellationToken);

        if (existing is not null)
            throw new InvalidOperationException($"A coupon for '{coupon.ProductName}' already exists.");

        dbContext.Coupons.Add(coupon);
        await dbContext.SaveChangesAsync(cancellationToken);
        return coupon;
    }

    public async Task<Coupon?> UpdateDiscountAsync(Coupon coupon, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Coupons
            .FirstOrDefaultAsync(c => c.ProductName == coupon.ProductName, cancellationToken);

        if (existing is null)
            return null;

        existing.Description = coupon.Description;
        existing.Amount = coupon.Amount;

        await dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteDiscountAsync(string productName, CancellationToken cancellationToken = default)
    {
        var coupon = await dbContext.Coupons
            .FirstOrDefaultAsync(c => c.ProductName == productName, cancellationToken);

        if (coupon is null)
            return false;

        dbContext.Coupons.Remove(coupon);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
```

> **Learning note**: `UpdateDiscountAsync` updates only `Description` and `Amount` on the *tracked* entity. EF Core change tracking detects the in-memory mutation and generates an `UPDATE` targeting only those two columns — not a full-row replace.

> **Learning note**: `CreateDiscountAsync` throws `InvalidOperationException` for the duplicate case — this is a domain-layer signal. `DiscountService.CreateDiscount` catches it and converts it to `RpcException(StatusCode.AlreadyExists)`, keeping gRPC concerns (status codes) out of the repository.

---

## Phase 4 — Add the `ValidationInterceptor` and validators *(parallel with Phases 2 and 3)*

Introduce the cross-cutting validation layer. This phase also requires two new NuGet packages. All files in this phase are independent of each other.

### 4.1 — Add NuGet packages to `Discount.Grpc.csproj`

**File**: `src/Services/Discount/Discount.Grpc/Discount.Grpc.csproj`

Add inside `<ItemGroup>`:

```xml
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.11.0" />
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore" Version="10.0.0" />
```

> `FluentValidation.DependencyInjectionExtensions` provides `AddValidatorsFromAssembly`. `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` provides `AddDbContextCheck<T>` (used in Phase 6).

### 4.2 — Create `Interceptors/ValidationInterceptor.cs`

The interceptor resolves the FluentValidation `IValidator<TRequest>` for each incoming call. If a validator is registered, it runs before the service method. On failure it maps validation errors to `StatusCode.InvalidArgument`. If no validator exists for the request type, the call passes through untouched.

**File**: `src/Services/Discount/Discount.Grpc/Interceptors/ValidationInterceptor.cs`

```csharp
using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Discount.Grpc.Interceptors;

public class ValidationInterceptor(IServiceProvider serviceProvider) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var validator = serviceProvider.GetService<IValidator<TRequest>>();

        if (validator is not null)
        {
            var result = await validator.ValidateAsync(request, context.CancellationToken);

            if (!result.IsValid)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
                throw new RpcException(new Status(StatusCode.InvalidArgument, errors));
            }
        }

        return await continuation(request, context);
    }
}
```

> **Learning note**: The interceptor uses `IServiceProvider.GetService<T>()` (returns `null` when not registered) rather than `GetRequiredService<T>()` (throws). This makes the interceptor a no-op for request types that have no validator — the gRPC service degrades gracefully rather than crashing.

### 4.3 — Create `Validators/GetDiscountRequestValidator.cs`

**File**: `src/Services/Discount/Discount.Grpc/Validators/GetDiscountRequestValidator.cs`

```csharp
using FluentValidation;

namespace Discount.Grpc.Validators;

public class GetDiscountRequestValidator : AbstractValidator<GetDiscountRequest>
{
    public GetDiscountRequestValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("ProductName is required.");
    }
}
```

### 4.4 — Create `Validators/CreateDiscountRequestValidator.cs`

**File**: `src/Services/Discount/Discount.Grpc/Validators/CreateDiscountRequestValidator.cs`

```csharp
using FluentValidation;

namespace Discount.Grpc.Validators;

public class CreateDiscountRequestValidator : AbstractValidator<CreateDiscountRequest>
{
    public CreateDiscountRequestValidator()
    {
        RuleFor(x => x.Coupon).NotNull().WithMessage("Coupon is required.");
        RuleFor(x => x.Coupon.ProductName)
            .NotEmpty().WithMessage("ProductName is required.");
        RuleFor(x => x.Coupon.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.");
    }
}
```

### 4.5 — Create `Validators/UpdateDiscountRequestValidator.cs`

**File**: `src/Services/Discount/Discount.Grpc/Validators/UpdateDiscountRequestValidator.cs`

```csharp
using FluentValidation;

namespace Discount.Grpc.Validators;

public class UpdateDiscountRequestValidator : AbstractValidator<UpdateDiscountRequest>
{
    public UpdateDiscountRequestValidator()
    {
        RuleFor(x => x.Coupon).NotNull().WithMessage("Coupon is required.");
        RuleFor(x => x.Coupon.ProductName)
            .NotEmpty().WithMessage("ProductName is required.");
        RuleFor(x => x.Coupon.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Amount must be 0 or greater.");
    }
}
```

---

## Phase 5 — Rewrite `DiscountService.cs` *(depends on Phase 3)*

Replace `DiscountContext` with `IDiscountRepository` in the constructor and delegate all data access through the repository. The gRPC contract (method signatures, return types) is unchanged.

### 5.1 — Rewrite `Services/DiscountService.cs`

**File**: `src/Services/Discount/Discount.Grpc/Services/DiscountService.cs`

- **Remove**: `using Microsoft.EntityFrameworkCore;`
- **Remove**: `using Discount.Grpc.Data;` — re-add it to resolve `IDiscountRepository` (not `DiscountContext`)
- **Change constructor**: replace `DiscountContext dbContext` with `IDiscountRepository repository`
- **Rewrite `GetDiscount`**: replace `dbContext.Coupons.FirstOrDefaultAsync(...)` with `await repository.GetDiscountAsync(request.ProductName, context.CancellationToken)`; zero-discount fallback (`?? new Coupon { ... }`) **kept** — business logic decision
- **Rewrite `CreateDiscount`**: remove dead `if (coupon == null)` block; replace `dbContext.Coupons.Add` + `SaveChangesAsync` with `await repository.CreateDiscountAsync(coupon, context.CancellationToken)`; wrap in `try/catch (InvalidOperationException)` → rethrow as `RpcException(StatusCode.AlreadyExists)`
- **Rewrite `UpdateDiscount`**: remove dead `if (coupon == null)` block; replace blind `dbContext.Coupons.Update` + `SaveChangesAsync` with `var updated = await repository.UpdateDiscountAsync(coupon, context.CancellationToken)`; add `if (updated is null) throw new RpcException(StatusCode.NotFound, ...)`; return `updated.Adapt<CouponModel>()`
- **Rewrite `DeleteDiscount`**: replace `FirstOrDefaultAsync` + null-throw + `Remove` + `SaveChangesAsync` with `var deleted = await repository.DeleteDiscountAsync(request.ProductName, context.CancellationToken)`; `if (!deleted) throw RpcException(StatusCode.NotFound, ...)`

Full rewritten file:

```csharp
using Discount.Grpc.Data;
using Discount.Grpc.Models;
using Grpc.Core;
using Mapster;

namespace Discount.Grpc.Services;

public class DiscountService(IDiscountRepository repository, ILogger<DiscountService> logger)
    : DiscountProtoService.DiscountProtoServiceBase
{
    public override async Task<CouponModel> GetDiscount(GetDiscountRequest request, ServerCallContext context)
    {
        var coupon = await repository.GetDiscountAsync(request.ProductName, context.CancellationToken)
            ?? new Coupon
            {
                ProductName = request.ProductName,
                Amount = 0,
                Description = "No discount available"
            };

        logger.LogInformation("Discount retrieved for ProductName: {ProductName}, Amount: {Amount}",
            coupon.ProductName, coupon.Amount);

        return coupon.Adapt<CouponModel>();
    }

    public override async Task<CouponModel> CreateDiscount(CreateDiscountRequest request, ServerCallContext context)
    {
        var coupon = request.Coupon.Adapt<Coupon>();

        try
        {
            await repository.CreateDiscountAsync(coupon, context.CancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
        }

        logger.LogInformation("Discount created for ProductName: {ProductName}, Amount: {Amount}",
            coupon.ProductName, coupon.Amount);

        return coupon.Adapt<CouponModel>();
    }

    public override async Task<CouponModel> UpdateDiscount(UpdateDiscountRequest request, ServerCallContext context)
    {
        var coupon = request.Coupon.Adapt<Coupon>();

        var updated = await repository.UpdateDiscountAsync(coupon, context.CancellationToken);

        if (updated is null)
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Discount for '{request.Coupon.ProductName}' not found."));

        logger.LogInformation("Discount updated for ProductName: {ProductName}, Amount: {Amount}",
            updated.ProductName, updated.Amount);

        return updated.Adapt<CouponModel>();
    }

    public override async Task<DeleteDiscountResponse> DeleteDiscount(DeleteDiscountRequest request, ServerCallContext context)
    {
        var deleted = await repository.DeleteDiscountAsync(request.ProductName, context.CancellationToken);

        if (!deleted)
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Discount for ProductName: {request.ProductName} not found."));

        logger.LogInformation("Discount deleted for ProductName: {ProductName}", request.ProductName);

        return new DeleteDiscountResponse { Success = true };
    }
}
```

---

## Phase 6 — Update `Program.cs` *(depends on all prior phases)*

Wire all new registrations into the host. This is the last phase because it references every type introduced in previous phases.

### 6.1 — Update `Program.cs`

**File**: `src/Services/Discount/Discount.Grpc/Program.cs`

**Modify `AddGrpc()` call** — chain the interceptor registration:
```csharp
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<ValidationInterceptor>();
});
```

**Add after `AddGrpcReflection()`**:
```csharp
var assembly = typeof(Program).Assembly;
builder.Services.AddValidatorsFromAssembly(assembly);
builder.Services.AddScoped<IDiscountRepository, DiscountRepository>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DiscountContext>();
```

**Add after `app.MapGrpcService<DiscountService>()`**:
```csharp
app.MapHealthChecks("/health");
```

Full updated `Program.cs`:

```csharp
using Discount.Grpc.Data;
using Discount.Grpc.Interceptors;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<ValidationInterceptor>();
});
builder.Services.AddGrpcReflection();
builder.Services.AddDbContext<DiscountContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Database")));

var assembly = typeof(Program).Assembly;
builder.Services.AddValidatorsFromAssembly(assembly);
builder.Services.AddScoped<IDiscountRepository, DiscountRepository>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DiscountContext>();

var app = builder.Build();

app.UseMigration();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGrpcService<DiscountService>();
app.MapHealthChecks("/health");
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

await app.RunAsync();
```

---

## Verification

1. **Build**: `dotnet build src/eshop-microservies.slnx` must succeed with **0 errors and 0 warnings** (CS4014 "unawaited task" warning is eliminated by Phase 1).

2. **Grep check** — the following symbols must not appear anywhere under `src/Services/Discount/Discount.Grpc/` after the changes:
   - `MigrateAsync` — replaced by `Migrate()`
   - `= default!` — replaced by `required`
   - `if (coupon == null)` — dead null guards removed
   - `dbContext` inside `DiscountService.cs` — all EF Core calls moved to `DiscountRepository`
   - `.Update(coupon)` — blind update replaced by find-then-update

3. **Functional smoke tests** (run `docker-compose up discount.grpc` or `dotnet run` and use [grpcurl](https://github.com/fullstorydev/grpcurl) against `localhost:5002`):

   | # | Test | Expected |
   |---|------|----------|
   | a | `GetDiscount` with `productName = "IPhone X"` | `200`, `amount = 150` |
   | b | `GetDiscount` with `productName = "Unknown"` | `200`, `amount = 0`, `description = "No discount available"` |
   | c | `GetDiscount` with empty `productName` | `INVALID_ARGUMENT` status from `ValidationInterceptor` |
   | d | `CreateDiscount` with new product, `amount = 50` | `200`, coupon returned |
   | e | `CreateDiscount` with `productName = "IPhone X"` (duplicate) | `ALREADY_EXISTS` status |
   | f | `CreateDiscount` with `amount = 0` | `INVALID_ARGUMENT` from validator |
   | g | `UpdateDiscount` with `productName = "IPhone X"`, `amount = 200` | `200`, `amount = 200` |
   | h | `UpdateDiscount` with `productName = "Unknown"` | `NOT_FOUND` status |
   | i | `DeleteDiscount` with `productName = "Samsung 10"` | `200`, `success = true` |
   | j | `DeleteDiscount` with `productName = "Unknown"` | `NOT_FOUND` status |
   | k | `GET /health` via HTTP | `200 Healthy` |

4. **Basket.API integration**: Start both `discount.grpc` and `basket.api` via `docker-compose up`. `StoreBasket` for a product with a known discount must return a cart with the price correctly reduced.
