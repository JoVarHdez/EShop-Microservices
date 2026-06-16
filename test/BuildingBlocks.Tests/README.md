# BuildingBlocks.Tests

Test suite for shared `BuildingBlocks` library behavior.

## Current Status

- Last validated date: 2026-06-16
- Command: `dotnet test test/BuildingBlocks.Tests/BuildingBlocks.Tests.csproj --collect:"XPlat Code Coverage"`
- Test result: 6 passed, 0 failed
- Coverage (latest run):
	- Line: 100.00%
	- Branch: 100.00%
- Coverage report:
	- `test/BuildingBlocks.Tests/TestResults/0e6e44c4-7c1c-4923-b310-be5bcbf86e38/coverage.cobertura.xml`

## Coverage Exclusions

None configured.

## Run Commands

### Default test run

```powershell
dotnet test test/BuildingBlocks.Tests/BuildingBlocks.Tests.csproj
```

### Run with coverage

```powershell
dotnet test test/BuildingBlocks.Tests/BuildingBlocks.Tests.csproj --collect:"XPlat Code Coverage"
```

### Optional: specify Cobertura output

```powershell
dotnet test test/BuildingBlocks.Tests/BuildingBlocks.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

## Docker or External Setup

No Docker or external infrastructure is required.

## Remaining Gaps and Improvements

Coverage targets are currently exceeded.

Optional hardening:

- Add tests for additional shared utilities as they are added to `BuildingBlocks`
- Keep service-level coverage isolated to service-owned code only
