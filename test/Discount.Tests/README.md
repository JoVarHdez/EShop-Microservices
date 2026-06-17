# Discount.Tests

Test suite for `Discount.Grpc` with a layered strategy:

- Fast unit tests for validators, service branching, and repository logic
- Integration tests for gRPC endpoints and validation interceptor behavior
- Routing tests for health and gRPC endpoint registration

## Current Status

- Last validated date: 2026-06-17
- Command: `dotnet test test/Discount.Tests/Discount.Tests.csproj`
- Test result: 26 passed, 0 failed
- Coverage report:
  - `test/Discount.Tests/TestResults/ca10c82b-e77c-4654-acef-0adb78b560b5/coverage.cobertura.xml`

## Coverage Exclusions

Configured in `coverlet.runsettings`:

- `Program.cs`
- `obj/**`
- `Migrations/**`
- `BuildingBlocks/BuildingBlocks/**`
- `BuildingBlocks/BuildingBlocks.Messaging/**`

These exclusions keep service-level coverage focused on Discount-owned code paths.

## Coverage Targets

- Line: 90% minimum
- Branch: 70% minimum on critical flows

## Run Commands

### Standard run

```powershell
dotnet test test/Discount.Tests/Discount.Tests.csproj
```

### Run with coverage collector

```powershell
dotnet test test/Discount.Tests/Discount.Tests.csproj --collect:"XPlat Code Coverage"
```

## Docker or External Setup

Default suite does not require Docker.

A docker-backed provider-coupled CI slice should be added to align with the selected policy decision in the testing spec.

## Remaining Gaps and Improvements

- Add CI-enforced docker-backed relational integration slice for provider-coupled behavior.
- Add script automation parity with other service test projects when Docker slice is introduced.
