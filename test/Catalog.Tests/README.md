# Catalog.Tests

Test suite for `Catalog.API` with a two-layer strategy:

- Fast mock-based unit and endpoint tests
- Real-provider read endpoint tests using PostgreSQL Testcontainers

## Current Status

- Last validated date: 2026-06-16
- Command: `pwsh -NoProfile -ExecutionPolicy Bypass -File test/Catalog.Tests/run-with-docker.ps1 -Coverage`
- Test result: 40 passed, 0 failed
- Coverage (latest Docker-backed run):
  - Line: 98.93%
  - Branch: 85.71%
- Coverage report:
  - `test/Catalog.Tests/TestResults/0ba6ad65-dbdd-4911-8d86-89d5a92fd4c1/coverage.cobertura.xml`

## Coverage Exclusions

Configured in `coverlet.runsettings`:

- `Program.cs`
- `ProductsEndpoints.cs`
- `CatalogInitialData.cs`
- `BuildingBlocks/BuildingBlocks/**`

These are excluded as bootstrap/seed aggregation code and cross-service shared library scope not owned by Catalog service tests.

`BuildingBlocks` coverage must be validated in its own dedicated test project and reported independently from `Catalog.Tests` service KPIs.

## Run Commands

### Default test run

```powershell
dotnet test test/Catalog.Tests/Catalog.Tests.csproj
```

### Docker-backed run with coverage (recommended)

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File test/Catalog.Tests/run-with-docker.ps1 -Coverage
```

### Docker-backed run without coverage

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File test/Catalog.Tests/run-with-docker.ps1 -Coverage:$false
```

## Docker Requirements

The container-backed tests in `Integration/Endpoints/ProductReadEndpointsWithPostgresTests.cs` require Docker.

- If Docker is required by the runner, the script sets `CATALOG_REQUIRE_DOCKER_TESTS=true`.
- If Docker is unavailable in that mode, tests fail fast with a clear message.

## Remaining Gaps To Reach 90%/70%

Coverage targets are currently met for `Catalog.API` service scope.

Next quality focus areas (optional hardening):

- Additional provider-backed tests for read-path variations and pagination branches
- More branch-oriented endpoint tests for alternate result paths and malformed combinations

## Folder Highlights

- `Unit/` : Validators, handlers, contract/default-value tests
- `Integration/Endpoints/` : HTTP endpoint tests (mock-based and provider-backed)
- `Routing/` : Route registration/shape tests
- `Support/` : Shared in-memory and Marten-backed test host bootstrap
- `run-with-docker.ps1` : One-command local Docker automation
