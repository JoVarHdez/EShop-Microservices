# Shopping.Web.Razor.Tests

Test suite for `Shopping.Web.Razor` with a layered strategy:

- Fast unit tests for page models, configuration validation, and basket service behavior
- Integration tests for Refit contract routing, startup registration, and resilience retry behavior
- Optional Docker-backed provider smoke tests through YARP gateway

## Current Status

- Last validated date: 2026-06-17
- Command: `dotnet test test/Shopping.Web.Razor.Tests/Shopping.Web.Razor.Tests.csproj --collect:"XPlat Code Coverage" --settings:test/Shopping.Web.Razor.Tests/coverlet.runsettings`
- Test result: 38 passed, 0 failed
- Coverage (latest run):
  - Line: 89.41% (228/255 lines covered)
  - Branch: 100% (14/14 branches covered)
- Coverage report:
  - `test/Shopping.Web.Razor.Tests/TestResults/<guid>/coverage.cobertura.xml`

## Coverage Exclusions

Configured in `coverlet.runsettings`:

- `Program.cs`
- `Pages/Error.cshtml.cs`
- `Pages/Confirmation.cshtml.cs`
- `Pages/*.cshtml` (compiled Razor views)
- `obj/**`
- `bin/**`
- `BuildingBlocks/**`

These exclusions keep service-level coverage scoped to Shopping.Web.Razor-owned behavior, consistent with service-level coverage reporting used in this repository. Razor view files (*.cshtml) are excluded as they are primarily HTML markup.

## Coverage Targets

- Line: 90% minimum
- Branch: 70% minimum on critical page-model and checkout/category flows

## Run Commands

### Default test run

```powershell
dotnet test test/Shopping.Web.Razor.Tests/Shopping.Web.Razor.Tests.csproj
```

### Run with coverage collector

```powershell
dotnet test test/Shopping.Web.Razor.Tests/Shopping.Web.Razor.Tests.csproj --collect:"XPlat Code Coverage" --settings:test/Shopping.Web.Razor.Tests/coverlet.runsettings
```

### Docker-backed provider smoke tests (optional)

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File test/Shopping.Web.Razor.Tests/run-with-docker.ps1 -Coverage
```

### Docker-backed run without coverage

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File test/Shopping.Web.Razor.Tests/run-with-docker.ps1 -Coverage:$false
```

## Docker Requirements

The provider-backed smoke tests in `Integration/Provider/ShoppingGatewaySmokeTests.cs` are optional and require Docker.

- Docker compose starts catalogdb, basketdb, distributedcache, orderingdb, messagebroker, discount.grpc, catalog.api, basket.api, ordering.api, yarpapigateway
- Provider tests use YARP at `http://localhost:6004`
- If Docker is required by the runner, the script sets `SHOPPING_REQUIRE_DOCKER_TESTS=true`
- If Docker is unavailable in that mode, tests fail fast with a clear message

## Remaining Gapsare nearly met:
- **Line coverage: 89.41%** - 0.59% short of 90% target
- **Branch coverage: 100%** - exceeds 70% target

Remaining 27 uncovered lines:
- **DevUserContextProvider** (2 lines): Concrete implementation is mocked in all tests; would require integration tests to cover
- **Model property getters** (25 lines): Auto-implemented properties on OrderResponse, AddressModel, PaymentModel; hit during JSON deserialization but not tracked by coverage

Next quality focus areas (optional hardening):

- Add integration test exercising DevUserContextProvider to close line coverage gap
Next quality focus areas (optional hardening):

- Add deeper provider-backed assertions for checkout happy-path data transitions after stable shared fixture setup
- Add malformed form-input integration coverage once antiforgery test harness strategy is finalized
- Revisit CI gating rules when CI scope is explicitly introduced

## Folder Highlights

- `Unit/Configuration/` : ApiSettings and DevUserContext validation tests
- `Unit/Services/` : BasketService behavior tests with mock clients
- `Unit/Pages/` : Page model tests for Index, ProductList, ProductDetail, Cart, Checkout, OrderList
- `Integration/Startup/` : Service registration and DI configuration tests
- `Integration/Http/` : Refit contract and route validation tests
- `Integration/Resilience/` : Retry and resilience pipeline behavior tests
- `Integration/Provider/` : Optional Docker-backed smoke tests through YARP gateway
- `Routing/` : Razor page endpoint registration tests
- `Support/` : Shared test harness (ShoppingWebAppFactory, FakeHttpMessageHandler, TestData, DockerAvailability)
- `run-with-docker.ps1` : One-command local Docker automation for provider smoke suite
