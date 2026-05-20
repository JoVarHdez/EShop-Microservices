# Discount.Grpc — .NET 10 Real-World Practices Modernization

## 1. Feature Summary

The Discount.Grpc microservice was built following the same .NET 8 course that produced Catalog.API and Basket.API. Unlike those two services — which required removing MediatR, Carter, and `BuildingBlocks.CQRS` marker interfaces — Discount.Grpc has a simpler structure: it is already a gRPC service with no MediatR or Carter dependencies. Its modernization targets the internal patterns specific to gRPC services in .NET 10.

Four concrete defects exist in the current code. First, `DiscountService` directly injects `DiscountContext` (an EF Core `DbContext`), coupling the gRPC service to the persistence layer and making it impossible to unit test without a real database. Second, `UpdateDiscount` performs a blind `dbContext.Coupons.Update(coupon)` call using a Mapster-mapped entity, bypassing existence verification and EF Core change tracking. Third, `UseMigration` calls `dbContext.Database.MigrateAsync()` without `await`, silently discarding the returned `Task` — a fire-and-forget async bug that allows gRPC requests to arrive before migrations complete. Fourth, `CreateDiscount` and `UpdateDiscount` both contain `if (coupon == null)` guards after a `Adapt<Coupon>()` call; Mapster never returns `null` for a class target, making these guards permanently dead code.

Beyond defects, three missing capabilities align this service with .NET 10 real-world practices: a `ValidationInterceptor` for request validation (the gRPC equivalent of Wolverine's FluentValidation middleware), C# 11 `required` properties on the `Coupon` domain model, and an EF Core health check for container-readiness probing. The external gRPC contract (all four RPC methods, the `.proto` file, and the proto-generated message shapes) remains identical — only the internal wiring changes.

---

## 2. Data Model / Entities

### Coupon *(property declaration change)*
- `Id`: `int` — EF Core primary key *(unchanged)*
- `ProductName`: `required string` — (was `string = default!`)
- `Description`: `required string` — (was `string = default!`)
- `Amount`: `int` — coupon discount amount *(unchanged)*

### IDiscountRepository *(new interface)*
- `GetDiscountAsync(string productName, CancellationToken) → Task<Coupon?>`
- `CreateDiscountAsync(Coupon coupon, CancellationToken) → Task<Coupon>`
- `UpdateDiscountAsync(Coupon coupon, CancellationToken) → Task<Coupon?>`
- `DeleteDiscountAsync(string productName, CancellationToken) → Task<bool>`

### DiscountRepository *(new class)*
Implements `IDiscountRepository`. Receives `DiscountContext` via primary constructor injection. Owns all EF Core calls currently in `DiscountService`.

### DiscountService *(dependency change only)*
- **Current constructor**: `DiscountContext dbContext, ILogger<DiscountService> logger`
- **New constructor**: `IDiscountRepository repository, ILogger<DiscountService> logger`
- Service methods delegate all data access to `IDiscountRepository`; no EF Core calls remain in the service class

---

## 3. Business Rules & Constraints

> Each rule explains the old approach vs. the new approach to support learning.

### Rule 1 — `IDiscountRepository` must abstract data access from `DiscountService`

**Old approach — direct `DbContext` injection**: `DiscountService` directly receives `DiscountContext` via primary constructor injection and calls EF Core methods (`FirstOrDefaultAsync`, `Add`, `Update`, `Remove`, `SaveChangesAsync`) inside each gRPC service method. This couples the gRPC transport layer to the persistence technology. To test `DiscountService`, a test must provision a real (or in-memory) SQLite database — there is no seam to substitute a fake.

**New approach — `IDiscountRepository` abstraction**: Extract an `IDiscountRepository` interface with four methods mirroring the four gRPC operations. A `DiscountRepository` class implements the interface and owns all EF Core calls (receiving `DiscountContext` via its own primary constructor). `DiscountService` receives `IDiscountRepository` and delegates data access through the interface. This is the same `IBasketRepository` pattern used in Basket.API: the service becomes testable by substituting a mock repository, and the persistence technology can change without touching the gRPC service class. Both are registered in `Program.cs` with `builder.Services.AddScoped<IDiscountRepository, DiscountRepository>()`.

### Rule 2 — `UpdateDiscount` must use a find-then-update pattern instead of blind `Update`

**Old approach — `dbContext.Coupons.Update(entity)`**: `UpdateDiscount` calls `request.Coupon.Adapt<Coupon>()` to map the incoming proto `CouponModel` to a `Coupon` domain object, then calls `dbContext.Coupons.Update(coupon)`. EF Core's `Update` method attaches the entity with `EntityState.Modified` on every property and attempts to execute an `UPDATE` for all columns. This approach:
- Does not verify the coupon exists before attempting to update it (a non-existent `Id` silently produces zero rows changed or throws `DbUpdateConcurrencyException` depending on database provider)
- Marks all columns as modified, even those that did not change
- Returns `StatusCode.OK` even when the coupon was not found — inconsistent with `DeleteDiscount`, which correctly returns `StatusCode.NotFound`

**New approach — find-then-update in `DiscountRepository.UpdateDiscountAsync`**: Load the existing entity with `FirstOrDefaultAsync(c => c.ProductName == coupon.ProductName)`. If null, return `null`. Otherwise, update only the mutable fields (`Description`, `Amount`) on the tracked entity, call `SaveChangesAsync()`, and return the updated entity. EF Core change tracking will generate an `UPDATE` statement for only the modified columns. `DiscountService.UpdateDiscount` receives the nullable result and returns `RpcException(StatusCode.NotFound)` when `null` is returned, making not-found behavior consistent across all four gRPC methods.

### Rule 3 — Dead null checks after `Adapt<Coupon>()` must be removed

**Old approach — unreachable guard blocks**: `CreateDiscount` and `UpdateDiscount` both call `request.Coupon.Adapt<Coupon>()` and immediately check `if (coupon == null) throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid coupon data"))`. Mapster's `Adapt<T>()` extension method **never returns `null`** when the target type is a reference class — it always constructs and populates a new instance. These null checks are permanently dead code: the `if` branch is unreachable and the `RpcException` is never thrown.

**New approach — remove the guard blocks**: Delete both `if (coupon == null)` blocks and their associated `throw new RpcException(...)` lines. Request-level validation (null/empty `productName`, zero or negative `amount`) is the responsibility of the `ValidationInterceptor` (Rule 5), not a post-mapping null guard inside the service method.

### Rule 4 — `UseMigration` must not fire-and-forget `MigrateAsync`

**Old approach — discarded `Task`**: `Extensions.UseMigration` calls `dbContext.Database.MigrateAsync()` without `await`. The returned `Task` is never awaited or observed. The consequences are:
- EF Core migrations may not have completed when the first gRPC request arrives, causing the service to execute queries against an un-migrated schema
- Any exception thrown during migration is silently swallowed — a migration failure leaves the database in an inconsistent state while the service continues to start as if nothing happened
- The C# compiler emits a warning (CS4014: "Because this call is not awaited...") that is currently being ignored

**New approach — synchronous `Migrate()`**: Replace `dbContext.Database.MigrateAsync()` with `dbContext.Database.Migrate()` (the synchronous overload). `UseMigration` is called during application startup, in the same synchronous `IApplicationBuilder` pipeline that configures middleware — before `app.Run()`. Using the synchronous overload ensures migrations fully apply (and any migration exception propagates and halts startup) before the server begins accepting gRPC connections. There is no benefit to async migration in this startup context.

### Rule 5 — Add a gRPC `Interceptor` for request validation (FluentValidation)

**Old approach — no validation**: Request validation is entirely absent from `DiscountService`. The only input checks were the permanently-dead null guards removed in Rule 3. An empty `productName` in a `GetDiscountRequest`, a negative `Amount` in a `CreateDiscountRequest`, or any other malformed input passes through to EF Core with no rejection.

**New approach — `ValidationInterceptor`**: Add a `ValidationInterceptor : Interceptor` class that overrides `UnaryServerHandler<TRequest, TResponse>`. On each incoming call it resolves `IValidator<TRequest>` from the DI container (via `IServiceProvider`). If a validator is found, it runs the validation; on failure it throws `RpcException(StatusCode.InvalidArgument, ...)` with the validation failure messages before the service method executes. If no validator is registered for the request type, the interceptor passes through. This is the gRPC equivalent of the Wolverine FluentValidation middleware added to Catalog.API and Basket.API: cross-cutting validation lives in one place rather than being scattered across service methods. The interceptor is registered via `.AddGrpc(opts => opts.Interceptors.Add<ValidationInterceptor>())` and `FluentValidation.DependencyInjectionExtensions.AddValidatorsFromAssembly(assembly)` in `Program.cs`.

### Rule 6 — `Coupon` model must use the `required` keyword

**Old approach — `= default!` null-forgiving**: `Coupon.ProductName` and `Coupon.Description` are declared as `string = default!`. The null-forgiving operator tells the compiler to suppress nullable warnings for that property but provides no enforcement — the property can still be left unset at runtime and will be `null` (despite the `!` suppression), causing silent data integrity issues.

**New approach — `required string`**: Replace `= default!` with the C# 11 `required` modifier. A `required` property must be initialized in every object initializer or primary constructor call at the call site. The compiler enforces this at compile time with `CS9035` — there is no runtime surprise. `DiscountRepository` uses EF Core entity initialization patterns that are compatible with `required` properties.

### Rule 7 — Add EF Core health checks for container-readiness probing

**Old approach — no health checks**: No health check endpoint is registered. Container orchestrators (Docker Compose healthcheck, Kubernetes `readinessProbe`) have no way to determine whether Discount.Grpc is ready to accept traffic — they can only check if the process is running, not if the database connection is healthy.

**New approach — `AddDbContextCheck<DiscountContext>()`**: Register `builder.Services.AddHealthChecks().AddDbContextCheck<DiscountContext>()` in `Program.cs`. Map the health endpoint with `app.MapHealthChecks("/health")`. The EF Core health check executes a lightweight `CanConnectAsync()` probe against the database. When the SQLite file is accessible, the endpoint returns `200 Healthy`; when the connection fails (file locked, file missing, corrupted database), it returns `503 Unhealthy`. This follows the same health check pattern established for Catalog.API and Basket.API.

---

## 4. Acceptance Criteria

1. `DiscountService` primary constructor takes `IDiscountRepository repository` and `ILogger<DiscountService> logger`; the class has no `using Microsoft.EntityFrameworkCore` import and no direct `DiscountContext` reference.
2. `DiscountRepository` implements `IDiscountRepository` with all four methods using `DiscountContext` via primary constructor injection; it is registered in `Program.cs` as `AddScoped<IDiscountRepository, DiscountRepository>()`.
3. `DiscountRepository.UpdateDiscountAsync` loads the existing coupon by `ProductName` before updating; returns `null` when not found; updates only `Description` and `Amount` on the tracked entity; calls `SaveChangesAsync` once.
4. `DiscountService.UpdateDiscount` returns `RpcException(StatusCode.NotFound)` when `IDiscountRepository.UpdateDiscountAsync` returns `null`.
5. No `if (coupon == null)` check follows any `Adapt<Coupon>()` call anywhere in the codebase.
6. `Extensions.UseMigration` calls `dbContext.Database.Migrate()` (synchronous); no unawaited `MigrateAsync()` call exists.
7. `ValidationInterceptor` is registered; a `GetDiscountRequest` with an empty `productName` returns `StatusCode.InvalidArgument` without reaching `DiscountService.GetDiscount`.
8. `Coupon.ProductName` and `Coupon.Description` are declared as `required string` with no `= default!` assignment.
9. `GET /health` returns `200 OK` when the SQLite database file is accessible.
10. The `.proto` file is unchanged; all four gRPC endpoints retain their current request/response message shapes and method names.

---

## 5. Out of Scope

The following are explicitly NOT part of this modernization:

- Changes to the `.proto` file, `CouponModel`, or any gRPC message contract
- Migrating from SQLite to another database engine
- Adding streaming gRPC methods (server-streaming, client-streaming, or bidirectional)
- Adding gRPC authentication or TLS configuration beyond current settings
- Replacing Mapster with another object-mapping library
- Changes to how `Basket.API` calls `DiscountProtoServiceClient` — the client contract is unchanged
- Replacing EF Core with Dapper, raw ADO.NET, or any other data access approach
- Changes to Docker, docker-compose, or deployment configuration
- Adding OpenTelemetry exporters or Serilog structured logging sinks
- Health check UI endpoint (`/health-ui`) — only `GET /health` is in scope

---

## 6. Decisions

All open questions resolved on 2026-05-20:

1. **`GetDiscount` zero-discount fallback → keep as-is (business logic)**: Returning a synthetic coupon (`Amount = 0`, `Description = "No discount available"`) when no row is found is intentional business behavior. Basket.API calls `GetDiscount` unconditionally for every cart item and expects a model back, not an error. Changing this to `StatusCode.NotFound` would break that caller. The zero-discount path is treated as the legitimate "no discount applies" response, not a missing-resource error.

2. **`CreateDiscount` duplicate detection → add `StatusCode.AlreadyExists`**: `CreateDiscountAsync` must check for an existing coupon with the same `ProductName` before inserting. If a row already exists, the repository returns `null` and the service method throws `RpcException(StatusCode.AlreadyExists, ...)`. This adds a unique-by-`ProductName` contract at the application level (the `.proto` contract is unchanged). A database-level unique index on `ProductName` should accompany this rule to enforce the constraint at the persistence layer.

3. **SQLite file path → keep current path; document the persistence caveat in README**: The `Data Source=discountdb` connection string stays in `appsettings.json` for now. The README must include a clearly visible warning that the database file is written to the container working directory and will be lost on container restart. The README must also include a "Database Alternatives" section recommending production-suitable replacements (PostgreSQL as primary recommendation, SQL Server and MySQL as alternatives), with the EF Core provider package name for each.
