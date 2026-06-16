# Coverage Configuration

Configure code coverage targets and tooling for 90% line coverage.

## Coverlet Configuration

### Install Coverlet

```bash
dotnet add package coverlet.collector
```

### .runsettings File

Create `src/.runsettings` for coverage settings:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage">
        <Configuration>
          <Format>cobertura</Format>
          <Exclude>[xunit*]*,[*.Tests]*,[*.Testing]*</Exclude>
          <IncludeTestAssembly>false</IncludeTestAssembly>
          <SingleHit>false</SingleHit>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
  <LoggerRunSettings>
    <Loggers>
      <Logger friendlyName="console" enabled="True">
        <Configuration>
          <Verbosity>minimal</Verbosity>
        </Configuration>
      </Logger>
    </Loggers>
  </LoggerRunSettings>
</RunSettings>
```

### Test Project .csproj Update

Add to your test project `.csproj`:

```xml
<PropertyGroup>
  <CollectCoverage>true</CollectCoverage>
  <CoverletOutputFormat>cobertura,json</CoverletOutputFormat>
  <CoverletOutput>./coverage/</CoverletOutput>
  <Threshold>90</Threshold>
  <ThresholdType>line</ThresholdType>
  <ThresholdStat>total</ThresholdStat>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="coverlet.collector" Version="6.0.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

---

## Running Tests with Coverage

### Basic Coverage Run

```bash
# Run tests and collect coverage
dotnet test --collect:"XPlat Code Coverage"
```

### With Coverage Enforcement

```bash
# Enforce 90% line coverage threshold
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Threshold=90 /p:ThresholdType=line
```

### Generate HTML Report

```bash
# Install ReportGenerator
dotnet tool install -g reportgenerator

# Generate HTML report from coverage results
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"./coverage-report" -reporttypes:Html
```

### View Report

Open `coverage-report/index.html` in a browser to inspect:
- Overall coverage percentage
- Per-file breakdown
- Uncovered lines highlighted
- Branch coverage details

---

## Coverage Exclusions

### Exclude Auto-Generated Code

In test project `.csproj`:

```xml
<PropertyGroup>
  <ExcludeFromCodeCoverage>
    [*.Grpc]*,
    [*.Migrations]*,
    [*]*.Properties.*,
    [xunit*]*
  </ExcludeFromCodeCoverage>
</PropertyGroup>
```

### Per-Method Exclusion (ExcludeFromCodeCoverage Attribute)

```csharp
// Exclude EF Core DbSet initialization from coverage
[ExcludeFromCodeCoverage]
public DbSet<Order> Orders { get; set; }
```

---

## Branch Coverage

For critical paths (event handlers, validators, complex logic):

### Measure Branch Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Include=[*]* /p:Threshold=70 /p:ThresholdType=branch
```

### Example: Branch Coverage for Event Handler

```csharp
public class BasketCheckoutEventHandlerTests
{
    [Theory]
    [InlineData(null)]  // Null basket
    [InlineData("")]    // Empty basket
    [InlineData("basket-123")]  // Valid basket
    public async Task Handle_AllBranches_Tested(string basketId)
    {
        // Test all conditional paths in the handler
    }
}
```

---

## GitHub Actions Integration

### Coverage Report in CI

Create `.github/workflows/test-coverage.yml`:

```yaml
name: Test & Coverage

on:
  pull_request:
    paths:
      - 'src/**'
      - '.github/workflows/test-coverage.yml'

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.x

      - name: Restore
        run: dotnet restore

      - name: Run Tests with Coverage
        run: dotnet test --configuration Release /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Threshold=90 /p:ThresholdType=line

      - name: Upload Coverage
        uses: codecov/codecov-action@v3
        with:
          files: ./coverage/*.cobertura.xml
          flags: unittests
          fail_ci_if_error: true
```

---

## Coverage Targets per Service

Adjust thresholds based on service complexity:

| Service | Line | Branch | Notes |
|---------|------|--------|-------|
| Catalog | 90% | 70% | API + gRPC endpoints |
| Basket | 90% | 75% | Complex checkout logic |
| Ordering | 90% | 80% | Event handlers, aggregate |
| Discount | 85% | 70% | gRPC service, calculation logic |
| ApiGateway (YARP) | 80% | 60% | Routing, minimal business logic |

---

## Troubleshooting Coverage Issues

### Coverage Not Collected

```bash
# Verify coverlet is installed
dotnet list package | grep coverlet

# Check .runsettings is loaded
dotnet test --settings:.runsettings --collect:"XPlat Code Coverage" -v detailed
```

### High Exclusions Inflating Coverage

- Only exclude auto-generated code (gRPC stubs, EF migrations)
- Avoid excluding business logic to hide low coverage
- Use `ThresholdFailBuild=true` to enforce in CI

### False Negatives in Coverage

- Use `SingleHit=false` to measure all code paths
- For branches, verify all `if/else` combinations are tested with `Theory` + `InlineData`

---

## Best Practices

1. **Target 90% line coverage** across all services
2. **Measure branch coverage 70%+** on critical paths (event handlers, validators)
3. **Exclude only auto-generated** code (gRPC, EF migrations)
4. **Run coverage locally** before pushing to CI
5. **Review uncovered lines** in HTML report each sprint
6. **Test all branches** in event handlers, validators, and error paths
