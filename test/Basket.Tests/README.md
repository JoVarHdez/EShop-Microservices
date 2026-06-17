# Basket.Tests

Test suite for `Basket.API` with a two-layer strategy:

- Fast mock-based unit and endpoint tests
- Component integration tests for checkout event mapping and basket deletion flow

## Current Status

- Last validated date: 2026-06-17
- Command: `dotnet test test/Basket.Tests/Basket.Tests.csproj`
- Test result: 31 passed, 0 failed
- Coverage (latest run):
	- Line: 100.00% (`Basket.API` package scope)
	- Branch: 91.66% (`Basket.API` package scope)
- Coverage report:
	- `test/Basket.Tests/TestResults/*/coverage.cobertura.xml`

## Coverage Exclusions

Configured in `coverlet.runsettings`:

- `Program.cs`
- `obj/**`
- `BuildingBlocks/BuildingBlocks/**`
- `BuildingBlocks/BuildingBlocks.Messaging/**`

These exclusions keep Basket service KPI reporting scoped to Basket-owned code, consistent with service-level coverage reporting used in this repository.

## Coverage Targets

- Line: 90% minimum
- Branch: 70% minimum on critical flows

## Run Commands

### Standard run

```powershell
dotnet test test/Basket.Tests/Basket.Tests.csproj
```

### Run with coverage collector

```powershell
dotnet test test/Basket.Tests/Basket.Tests.csproj --collect:"XPlat Code Coverage"
```

### Optional: specify Cobertura output

```powershell
dotnet test test/Basket.Tests/Basket.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

## Docker or External Setup

No Docker or external infrastructure is required for the default Basket test suite.

## Remaining Gaps and Improvements

- Default tests use deterministic doubles for Discount gRPC behavior.
- If real Discount gRPC integration is introduced later, add a dedicated docker-backed script and document it as an optional suite.
