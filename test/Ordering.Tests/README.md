# Ordering.Tests

Test suite for `Ordering.API` with a two-layer strategy:

- Fast host-level and unit coverage for validators, handlers, routes, and message mapping
- Real-provider SQL Server coverage for persistence, health checks, and domain event dispatch

## Current Status

- Last validated date: 2026-06-17
- Command: `dotnet test test/Ordering.Tests/Ordering.Tests.csproj`
- Test result: 60 passed, 0 failed
- Coverage (latest run):
  - Line: 90.16% (706/783)
  - Branch: 74.07% (40/54)
- Coverage report:
  - `test/Ordering.Tests/TestResults/0d669601-b21c-49f2-bc88-47d971a3b24e/coverage.cobertura.xml`

## Coverage Exclusions

Configured in `coverlet.runsettings`:

- `Program.cs`
- `obj/**`
- `Ordering.Infrastructure/Data/Migrations/**`
- `Ordering.Infrastructure/Extensions/InitialData.cs`
- `BuildingBlocks/BuildingBlocks/**`
- `BuildingBlocks/BuildingBlocks.Messaging/**`

These exclusions keep Ordering service coverage scoped to Ordering-owned code and exclude bootstrap, generated, seed, and shared-library files that should not count toward the Ordering service KPI.

## Run Commands

### Default test run

```powershell
dotnet test test/Ordering.Tests/Ordering.Tests.csproj
```

### Docker-backed run with coverage

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File test/Ordering.Tests/run-with-docker.ps1 -Coverage
```

### Docker-backed run without coverage

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File test/Ordering.Tests/run-with-docker.ps1 -Coverage:$false
```

## Docker or External Setup

- The provider-backed tests in `Integration/` require Docker.
- Docker-backed command:
  - `pwsh -NoProfile -ExecutionPolicy Bypass -File test/Ordering.Tests/run-with-docker.ps1 -Coverage`
- If Docker is unavailable, the default local `dotnet test` run may skip provider-backed tests once those tests are implemented.

## Remaining Gaps and Improvements

- Add explicit failure-path provider tests for SQL constraint and transaction rollback scenarios.
