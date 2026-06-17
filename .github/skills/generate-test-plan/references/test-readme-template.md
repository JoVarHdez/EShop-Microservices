# Test README Template

Use this template when creating or updating service test project READMEs under test/*.

## Header

```markdown
# [ServiceName].Tests

Test suite for `[ServiceName].API` with [brief strategy summary].
```

## Current Status

```markdown
## Current Status

- Last validated date: [YYYY-MM-DD]
- Command: `[single canonical command used for latest validation]`
- Test result: [N passed, 0 failed]
- Coverage (latest run):
  - Line: [XX.XX%]
  - Branch: [XX.XX%]
- Coverage report:
  - `test/[ServiceName].Tests/TestResults/[run-id]/coverage.cobertura.xml`
```

## Coverage Exclusions

```markdown
## Coverage Exclusions

Configured in `coverlet.runsettings`:

- `[excluded-file-or-pattern-1]`
- `[excluded-file-or-pattern-2]`

[One short paragraph explaining why exclusions are valid and ownership boundaries for shared libraries.]
```

## Run Commands

```markdown
## Run Commands

### Default test run

```powershell
dotnet test test/[ServiceName].Tests/[ServiceName].Tests.csproj
```

### Run with coverage

```powershell
dotnet test test/[ServiceName].Tests/[ServiceName].Tests.csproj --collect:"XPlat Code Coverage"
```

### Optional: specify Cobertura output

```powershell
dotnet test test/[ServiceName].Tests/[ServiceName].Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```
```

## Docker or External Setup

```markdown
## Docker or External Setup

[Choose exactly one]

- No Docker or external infrastructure is required.

OR

- The tests in `[path-to-provider-coupled-tests]` require Docker.
- Docker-backed command:
  - `pwsh -NoProfile -ExecutionPolicy Bypass -File test/[ServiceName].Tests/run-with-docker.ps1 -Coverage`
- If Docker is unavailable, tests [fail fast / skip] as documented by project policy.
```

## Remaining Gaps and Improvements

```markdown
## Remaining Gaps and Improvements

- [Gap 1]
- [Gap 2]
```

## Folder Highlights (Optional)

```markdown
## Folder Highlights

- `Unit/`: [summary]
- `Integration/`: [summary]
- `Routing/`: [summary]
- `Support/`: [summary]
```

## Standardization Rules

- Follow the same section order used in existing test READMEs in this repository.
- Prefer dotnet test commands for baseline documentation.
- Only document custom scripts when they are Docker-backed or required for provider-coupled local automation.
- Do not document CI workflow yaml files unless they already exist in the repository.
- Keep status values factual and reproducible from a recent test run.
