# Wolverine Integration — Common Errors

## WolverineFx.FluentValidation namespace

**Symptom**: `CS1061: 'WolverineOptions' does not contain a definition for 'UseFluentValidation'`

**Cause**: `UseFluentValidation()` lives in the `Wolverine.FluentValidation` namespace (package `WolverineFx.FluentValidation`). Forgetting the `using Wolverine.FluentValidation;` directive causes a misleading "method not found" error.

**Fix**:
```csharp
using Wolverine.FluentValidation;
// ...
builder.Host.UseWolverine(opts => opts.UseFluentValidation(RegistrationBehavior.ExplicitRegistration));
```

The `RegistrationBehavior.ExplicitRegistration` enum value is required when validators are registered via `AddValidatorsFromAssembly` (avoids double-registration).

---

## BuildingBlocks.Exceptions no longer exists

**Symptom**: `CS0234: The type or namespace name 'Exceptions' does not exist in the namespace 'BuildingBlocks'`

**Cause**: `BuildingBlocks.Exceptions.Handler.CustomExceptionHandler` was removed from BuildingBlocks during the Catalog modernization. Each service now owns its own `Exceptions/Handler/CustomExceptionHandler.cs`.

**Fix**: Copy the local `CustomExceptionHandler` from `Catalog.API/Exceptions/Handler/CustomExceptionHandler.cs` into the target service's `Exceptions/Handler/` folder and update the `using` in `Program.cs` to point to the service's own namespace (e.g., `using Basket.API.Exceptions.Handler;`).

---

## BuildingBlocks.CQRS no longer exists

**Symptom**: `CS0234: The type or namespace name 'CQRS' does not exist in the namespace 'BuildingBlocks'`

**Cause**: `ICommand<T>`, `ICommandHandler<T,R>`, `IQuery<T>`, and `IQueryHandler<T,R>` were removed from BuildingBlocks. Wolverine discovers handlers by convention (a `Handle` method whose first parameter matches the message type) — no marker interfaces needed.

**Fix**: Remove `using BuildingBlocks.CQRS;` and strip `: ICommand<T>` / `: ICommandHandler<T,R>` from record/class declarations. Rename the `Handle` method's first parameter from `request` to `command` to match Wolverine convention.
