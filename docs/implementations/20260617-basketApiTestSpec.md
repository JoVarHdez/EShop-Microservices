# Implementation Plan: Basket API - Testing Implementation

> Spec: [docs/specs/20260617-basketApiTestSpec.md](../specs/20260617-basketApiTestSpec.md)

TL;DR - Create a dedicated Basket test project first, then add test host/support scaffolding, then implement unit tests for handlers/validators/repositories, then add routing and integration tests, and finish by aligning documentation and local execution with repository standards.

---

## Relevant Files

| Action | File |
|--------|------|
| CREATE | test/Basket.Tests/Basket.Tests.csproj |
| CREATE | test/Basket.Tests/coverlet.runsettings |
| CREATE | test/Basket.Tests/README.md |
| CREATE | test/Basket.Tests/Support/BasketApiTestHost.cs |
| CREATE | test/Basket.Tests/Support/Fakes/FakeDiscountProtoServiceClient.cs |
| CREATE | test/Basket.Tests/Unit/Validators/StoreBasketCommandValidatorTests.cs |
| CREATE | test/Basket.Tests/Unit/Validators/DeleteBasketCommandValidatorTests.cs |
| CREATE | test/Basket.Tests/Unit/Validators/CheckoutBasketCommandValidatorTests.cs |
| CREATE | test/Basket.Tests/Unit/Handlers/StoreBasketCommandHandlerTests.cs |
| CREATE | test/Basket.Tests/Unit/Handlers/DeleteBasketCommandHandlerTests.cs |
| CREATE | test/Basket.Tests/Unit/Handlers/CheckoutBasketCommandHandlerTests.cs |
| CREATE | test/Basket.Tests/Unit/Data/BasketRepositoryTests.cs |
| CREATE | test/Basket.Tests/Unit/Data/CachedBasketRepositoryTests.cs |
| CREATE | test/Basket.Tests/Integration/Endpoints/BasketEndpointsTests.cs |
| CREATE | test/Basket.Tests/Integration/Checkout/CheckoutBasketIntegrationFlowTests.cs |
| CREATE | test/Basket.Tests/Routing/BasketRoutingTests.cs |
| MODIFY | src/eshop-microservices.slnx |
| MODIFY | src/Services/Basket/Basket.API/Basket/StoreBasket/StoreBasketHandler.cs |
| MODIFY | src/Services/Basket/Basket.API/Basket/CheckoutBasket/CheckoutBasketHandler.cs |

---

## Phase 1 - Test Project Scaffolding (independent)

This phase creates the new Basket test project and coverage configuration so all later test files compile and run immediately.

All steps in this phase are independent and can run in parallel.

### 1.1 - Create Basket test project file

File: test/Basket.Tests/Basket.Tests.csproj

- Add .NET test project metadata aligned with Catalog tests:
  - TargetFramework net10.0
  - Nullable enable
  - IsPackable false
  - RunSettingsFilePath coverlet.runsettings
- Add package references:
  - Microsoft.NET.Test.Sdk
  - xunit
  - xunit.runner.visualstudio
  - Moq
  - Microsoft.AspNetCore.Mvc.Testing
  - coverlet.collector
  - coverlet.msbuild (for threshold gating)
- Add project reference to src/Services/Basket/Basket.API/Basket.API.csproj

Snippet:

```xml
<ItemGroup>
  <PackageReference Include="coverlet.collector" Version="6.0.4" />
  <PackageReference Include="coverlet.msbuild" Version="6.0.4" />
  <PackageReference Include="Moq" Version="4.20.72" />
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
  <PackageReference Include="xunit" Version="2.9.3" />
  <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
</ItemGroup>
```

- Unchanged: Basket.API production project references and runtime wiring.

### 1.2 - Add coverage settings

File: test/Basket.Tests/coverlet.runsettings

- Configure XPlat collector exclusions for bootstrap and generated artifacts:
  - Program.cs
  - Generated grpc client outputs under obj
  - Shared BuildingBlocks tree

- Unchanged: Basket API behavior and endpoint implementation.

### 1.3 - Add Basket test runbook

File: test/Basket.Tests/README.md

- Document standard `dotnet test` commands and coverage command using built-in collector.
- Document expected thresholds and where to find coverage output.
- Align section order and wording with existing `test/*/README.md` standards.

- Unchanged: Existing service READMEs.

### 1.4 - Register test project in solution

File: src/eshop-microservices.slnx

- Add project entry for ../test/Basket.Tests/Basket.Tests.csproj.

Learning note: Including the test project in the solution ensures IDE discovery, command consistency, and easier CI targeting.

---

## Phase 2 - Support Infrastructure for Tests (depends on Phase 1)

This phase adds reusable in-memory host and test doubles so endpoint and flow tests remain fast and deterministic.

### 2.1 - Create Basket test host

File: test/Basket.Tests/Support/BasketApiTestHost.cs

- Build a test host similar to Catalog support:
  - Use TestServer
  - Map /basket group and register all Basket endpoints using MapBasketEndpoints()
  - Map /health
- Provide helper overloads:
  - StartAsync with IMessageBus + IBasketRepository
  - BuildAppForRouteInspection for route tests

Snippet:

```csharp
var group = app.MapGroup("/basket");
group.MapGetBasketEndpoint();
group.MapStoreBasketEndpoint();
group.MapDeleteBasketEndpoint();
group.MapCheckoutBasketEndpoint();
app.MapHealthChecks("/health");
```

- Unchanged: Production Program.cs hosting pipeline.

### 2.2 - Create Discount gRPC test double

File: test/Basket.Tests/Support/Fakes/FakeDiscountProtoServiceClient.cs

- Add deterministic fake client that returns configured discount amounts by product name/id.
- Provide defaults for missing products (zero discount) to avoid flaky tests.

Learning note: Using a fake client satisfies decision 3 (default doubles) while still validating discount application logic deeply, without requiring script-managed docker orchestration.

- Unchanged: Basket handler signatures and runtime gRPC registration.

---

## Phase 3 - Unit Tests for Validators, Handlers, and Data Layer (depends on Phase 2)

This phase covers command correctness and repository/cache behavior, which are the highest value deterministic checks.

### 3.1 - Validator tests

Files:
- test/Basket.Tests/Unit/Validators/StoreBasketCommandValidatorTests.cs
- test/Basket.Tests/Unit/Validators/DeleteBasketCommandValidatorTests.cs
- test/Basket.Tests/Unit/Validators/CheckoutBasketCommandValidatorTests.cs

- Verify success and invalid cases:
  - Store: null cart, empty username
  - Delete: empty username
  - Checkout: null dto, empty username

- Unchanged: Validation rule definitions in Basket.API.

### 3.2 - Handler tests: StoreBasket

File: test/Basket.Tests/Unit/Handlers/StoreBasketCommandHandlerTests.cs

- Verify repository store invocation and result username.
- Verify discount deduction per item before persistence using fake gRPC client.

Snippet:

```csharp
Assert.Equal(expectedDiscountedPrice, capturedBasket.Items[0].Price);
repository.Verify(x => x.StoreBasketAsync(It.IsAny<ShoppingCart>(), It.IsAny<CancellationToken>()), Times.Once);
```

- Unchanged: StoreBasket endpoint response contract.

### 3.3 - Handler tests: DeleteBasket

File: test/Basket.Tests/Unit/Handlers/DeleteBasketCommandHandlerTests.cs

- Verify repository delete called once and Success true returned.

- Unchanged: Delete endpoint route and response model.

### 3.4 - Handler tests: CheckoutBasket

File: test/Basket.Tests/Unit/Handlers/CheckoutBasketCommandHandlerTests.cs

- Success path:
  - Existing basket returns IsSuccess true
  - Publishes BasketCheckoutEvent once
  - Event TotalPrice equals basket total
  - Event Items contains ProductId/ProductName/Quantity/UnitPrice mapped from basket items
  - Basket deleted after publish
- Not found path:
  - IsSuccess false
  - Publish is not called
  - Delete is not called

Snippet:

```csharp
publishEndpoint.Verify(x => x.Publish(
    It.Is<BasketCheckoutEvent>(e =>
        e.UserName == requestUser &&
        e.Items.Count == 2 &&
        e.Items.All(i => i.Quantity > 0)),
    It.IsAny<CancellationToken>()),
Times.Once);
```

- Unchanged: BasketCheckoutEvent schema.

### 3.5 - Data layer tests

Files:
- test/Basket.Tests/Unit/Data/BasketRepositoryTests.cs
- test/Basket.Tests/Unit/Data/CachedBasketRepositoryTests.cs

- BasketRepository tests:
  - GetBasketAsync returns loaded value or null
  - StoreBasketAsync calls Store + SaveChangesAsync
  - DeleteBasketAsync calls Delete + SaveChangesAsync and returns true
- CachedBasketRepository tests:
  - Cache hit returns cached basket and bypasses inner repository
  - Cache miss loads from inner repository and caches non-null result
  - Store writes to inner repository and updates cache
  - Delete removes from cache after inner delete

Learning note: Repository tests should assert interaction boundaries (calls and sequencing), not Marten internals.

- Unchanged: Marten query translation behavior.

---

## Phase 4 - Routing and Integration Tests (depends on Phase 2 and Phase 3)

This phase validates HTTP contracts and route registration for the Basket surface.

### 4.1 - Routing tests

File: test/Basket.Tests/Routing/BasketRoutingTests.cs

- Verify routes are registered:
  - /basket/{userName} for GET and DELETE
  - /basket/
  - /basket/checkout
  - /health
- Verify endpoint names include GetBasket, StoreBasket, DeleteBasket, CheckoutBasket.

- Unchanged: Endpoint names and route templates in Basket.API.

### 4.2 - Endpoint integration tests (host-level)

File: test/Basket.Tests/Integration/Endpoints/BasketEndpointsTests.cs

- Use BasketApiTestHost with mocked IMessageBus and IBasketRepository.
- Cover success and failure status contracts:
  - GET existing and missing basket
  - POST /basket valid and malformed body
  - DELETE /basket/{userName} success
  - POST /basket/checkout success and not found result mapping
  - GET /health returns 200

### 4.3 - Checkout integration flow tests (component integration)

File: test/Basket.Tests/Integration/Checkout/CheckoutBasketIntegrationFlowTests.cs

- Exercise CheckoutBasketCommandHandler with real command + in-memory basket data and mocked publish endpoint.
- Assert publish payload completeness and post-publish delete side effect.
- Assert not-found branch yields no publish and no delete.

Learning note: This component-level integration test satisfies checkout business flow requirements without requiring external broker/gRPC/container dependencies on every PR run.

- Unchanged: Real-service verification is deferred until CI workflow onboarding is approved.

---

## Phase 5 - Runtime Validation Fixes and Documentation Alignment (depends on Phase 1-4)

This phase applies validation hardening discovered by tests and aligns repository documentation with current constraints (no new workflow yaml and no non-docker scripts).

### 5.1 - Harden null-safe validator behavior uncovered by tests

Files:
- src/Services/Basket/Basket.API/Basket/StoreBasket/StoreBasketHandler.cs
- src/Services/Basket/Basket.API/Basket/CheckoutBasket/CheckoutBasketHandler.cs

- Add null guards to dependent FluentValidation rules:
  - `Cart.UserName` validation only when `Cart` is not null
  - `BasketCheckoutDto.UserName` validation only when `BasketCheckoutDto` is not null

### 5.2 - Keep local tooling unchanged unless docker-backed scripts are introduced

Files:
- .vscode/tasks.json
- .vscode/settings.json

- Do not add Basket tasks pointing to non-existent scripts.
- Keep Catalog docker task entries unchanged.

### 5.3 - Preserve existing behavior explicitly

- Unchanged: Basket.API runtime architecture, endpoint contracts, and service dependencies.
- Unchanged: Out-of-scope items (performance/load testing, DAST, service re-architecture).
- Deferred: `.github/workflows/*.yml` onboarding for Basket tests until explicitly requested.

---

## Verification

1. Build
- Command: dotnet build src/eshop-microservices.slnx
- Expected: 0 errors.

2. Grep checks (must return no matches in Basket tests)
- Legacy symbols not expected:
  - MediatR ISender usage in Basket test files.
  - Placeholder checkout line-item assertions that omit ProductId/ProductName.
- Commands (PowerShell):
  - Get-ChildItem test/Basket.Tests -Recurse -Filter *.cs | Select-String "ISender"
  - Get-ChildItem test/Basket.Tests -Recurse -Filter *.cs | Select-String "Items\s*=\s*\[\s*\]"

3. Test and coverage gate
- Commands:
  - dotnet test test/Basket.Tests/Basket.Tests.csproj
  - dotnet test test/Basket.Tests/Basket.Tests.csproj --collect:"XPlat Code Coverage"
- Expected:
  - All tests pass
  - Basket.API line coverage >= 90
  - Basket.API branch coverage >= 70

4. Functional smoke checks (host-level API behavior)
- GET /basket/{existingUser} -> 200 with cart payload.
- GET /basket/{missingUser} -> 404.
- POST /basket with valid payload -> 201 and location /basket/{userName}.
- POST /basket with malformed JSON -> 400.
- DELETE /basket/{userName} -> 200 with success true.
- POST /basket/checkout for missing basket -> 404.
- POST /basket/checkout for existing basket flow -> 200 with isSuccess true and event publish verified in tests.
- GET /health -> 200.

5. CI policy checks
- Deferred by decision: no Basket-specific workflow yaml is added at this stage.
