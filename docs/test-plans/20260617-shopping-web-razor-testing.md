# Test Plan: Shopping.Web.Razor Testing

**Spec**: [Shopping.Web.Razor Test Specification](../specs/20260617-shoppingWebRazorTestSpec.md)
**Service**: Shopping.Web.Razor
**Framework**: xUnit + Moq
**Coverage Target**: 90% line coverage, 70%+ branch coverage on critical paths
**Last Updated**: 2026-06-17

## 1. Overview

This plan implements the first dedicated automated test suite for Shopping.Web.Razor across unit, routing, and host-level integration, plus a minimal provider-backed smoke slice for gateway-compatible behavior.

The scope focuses on modernization-sensitive behavior:
- typed options validation at startup (`ApiSettings`, `DevUserContext`)
- centralized Refit registration and resilience wiring
- page-model branching and basket orchestration behavior
- outbound contract usage for Catalog/Basket/Ordering endpoints

This plan intentionally excludes CI workflow work and keeps execution guidance local.

### Decisions Applied

1. CI policy is deferred and not part of this plan.
2. Outbound API verification uses Option C: hybrid contract assertions (strict checks for critical basket/checkout calls, semantic checks elsewhere).
3. Resilience verification uses Option B: registration assertions plus one transient-failure retry scenario per client.

## 2. Relevant Files

| Action | File | Notes |
|---|---|---|
| CREATE | `test/Shopping.Web.Razor.Tests/Shopping.Web.Razor.Tests.csproj` | New test project with xUnit, Moq, ASP.NET Core host testing, coverage collector |
| CREATE | `test/Shopping.Web.Razor.Tests/coverlet.runsettings` | Service-scoped coverage configuration |
| CREATE | `test/Shopping.Web.Razor.Tests/README.md` | Repository-standard runbook and current status |
| CREATE | `test/Shopping.Web.Razor.Tests/run-with-docker.ps1` | Docker-backed provider smoke execution |
| CREATE | `test/Shopping.Web.Razor.Tests/Support/ShoppingWebAppFactory.cs` | Shared WebApplicationFactory bootstrap |
| CREATE | `test/Shopping.Web.Razor.Tests/Support/FakeHttpMessageHandler.cs` | Deterministic outbound HTTP inspection double |
| CREATE | `test/Shopping.Web.Razor.Tests/Support/DockerAvailability.cs` | Docker detection + skip/fail-fast policy utility |
| CREATE | `test/Shopping.Web.Razor.Tests/Integration/ShoppingProviderCollection.cs` | Serialized provider-backed collection |
| CREATE | `test/Shopping.Web.Razor.Tests/Unit/Services/BasketServiceTests.cs` | Basket load fallback and delegation coverage |
| CREATE | `test/Shopping.Web.Razor.Tests/Unit/Configuration/ApiSettingsValidationTests.cs` | Gateway address validation behavior |
| CREATE | `test/Shopping.Web.Razor.Tests/Unit/Configuration/DevUserContextValidationTests.cs` | Dev user context validation behavior |
| CREATE | `test/Shopping.Web.Razor.Tests/Unit/Pages/IndexModelTests.cs` | Product retrieval and add-to-cart behavior |
| CREATE | `test/Shopping.Web.Razor.Tests/Unit/Pages/ProductListModelTests.cs` | Category branch and add-to-cart flow |
| CREATE | `test/Shopping.Web.Razor.Tests/Unit/Pages/ProductDetailModelTests.cs` | Not-found branch and add-to-cart path |
| CREATE | `test/Shopping.Web.Razor.Tests/Unit/Pages/CartModelTests.cs` | Remove-from-cart behavior |
| CREATE | `test/Shopping.Web.Razor.Tests/Unit/Pages/CheckoutModelTests.cs` | ModelState guard and checkout mapping |
| CREATE | `test/Shopping.Web.Razor.Tests/Unit/Pages/OrderListModelTests.cs` | Customer-context ordering query behavior |
| CREATE | `test/Shopping.Web.Razor.Tests/Integration/Startup/ServiceRegistrationTests.cs` | DI registrations + typed options + resilience registration checks |
| CREATE | `test/Shopping.Web.Razor.Tests/Integration/Http/RefitContractTests.cs` | Hybrid outbound API contract verification |
| CREATE | `test/Shopping.Web.Razor.Tests/Integration/Resilience/ResiliencePipelineTests.cs` | Transient failure retry scenario per client |
| CREATE | `test/Shopping.Web.Razor.Tests/Routing/RazorPageHandlerRoutingTests.cs` | Handler route/verb/response behavior |
| CREATE | `test/Shopping.Web.Razor.Tests/Integration/Provider/ShoppingGatewaySmokeTests.cs` | Docker-backed provider-coupled smoke checks |
| MODIFY | `src/eshop-microservices.slnx` | Add test project to solution |

## 3. Phase Plan

### Phase 1: Scaffold the Shopping.Web.Razor test project

Create the test project and register it in solution so all subsequent files have a stable target.

**Files**
- `test/Shopping.Web.Razor.Tests/Shopping.Web.Razor.Tests.csproj`
- `test/Shopping.Web.Razor.Tests/coverlet.runsettings`
- `src/eshop-microservices.slnx`

**Implementation details**
- Add standard packages used by existing service tests:
  - `Microsoft.NET.Test.Sdk`
  - `xunit`
  - `xunit.runner.visualstudio`
  - `Moq`
  - `FluentAssertions`
  - `Microsoft.AspNetCore.Mvc.Testing`
  - `coverlet.collector`
- Add a project reference to `src/WebApps/Shopping.Web.Razor/Shopping.Web.Razor.csproj`.
- Ensure nullable and implicit usings align with other test projects in repo.

**Coverage exclusions**
- `**/Program.cs` (bootstrap)
- `**/obj/**`
- `**/bin/**`
- `**/Pages/Error.cshtml.cs` (framework boilerplate-only page model)
- `**/BuildingBlocks/**`

### Phase 2: Build shared test harness and runbook

Introduce reusable infrastructure before writing tests.

**Files**
- `test/Shopping.Web.Razor.Tests/Support/ShoppingWebAppFactory.cs`
- `test/Shopping.Web.Razor.Tests/Support/FakeHttpMessageHandler.cs`
- `test/Shopping.Web.Razor.Tests/Support/DockerAvailability.cs`
- `test/Shopping.Web.Razor.Tests/Integration/ShoppingProviderCollection.cs`
- `test/Shopping.Web.Razor.Tests/run-with-docker.ps1`
- `test/Shopping.Web.Razor.Tests/README.md`

**Implementation details**
- `ShoppingWebAppFactory.cs`
  - derive from `WebApplicationFactory<Program>`.
  - support replacing outbound `HttpMessageHandler` for Refit clients.
  - support overriding dev user context options for deterministic tests.
- `FakeHttpMessageHandler.cs`
  - capture request method/path/query/body for assertions.
  - return programmable `HttpResponseMessage` queue.
- `DockerAvailability.cs`
  - detect docker availability through local docker command probe.
  - expose helper for skip/fail-fast mode.
- `run-with-docker.ps1`
  - start/verify docker-backed dependencies.
  - run provider tests only with category filter.
  - optional `-Coverage` switch matching existing test scripts pattern.

**Docker policy**
- default local `dotnet test`: provider-backed tests are skipped when Docker is unavailable.
- docker script: fail fast when Docker cannot be reached.

### Phase 3: Implement mock-solvable unit coverage

Cover deterministic service and page-model behavior with Moq and direct class testing.

**Files**
- `test/Shopping.Web.Razor.Tests/Unit/Services/BasketServiceTests.cs`
- `test/Shopping.Web.Razor.Tests/Unit/Configuration/ApiSettingsValidationTests.cs`
- `test/Shopping.Web.Razor.Tests/Unit/Configuration/DevUserContextValidationTests.cs`
- `test/Shopping.Web.Razor.Tests/Unit/Pages/IndexModelTests.cs`
- `test/Shopping.Web.Razor.Tests/Unit/Pages/ProductListModelTests.cs`
- `test/Shopping.Web.Razor.Tests/Unit/Pages/ProductDetailModelTests.cs`
- `test/Shopping.Web.Razor.Tests/Unit/Pages/CartModelTests.cs`
- `test/Shopping.Web.Razor.Tests/Unit/Pages/CheckoutModelTests.cs`
- `test/Shopping.Web.Razor.Tests/Unit/Pages/OrderListModelTests.cs`

**Priority cases**

#### Services
- `BasketService.LoadUserBasketAsync` returns cart on successful API call.
- `BasketService.LoadUserBasketAsync` returns empty cart with current username on API failure.
- `BasketService.StoreBasketAsync/DeleteBasketAsync/CheckoutBasketAsync` delegate exactly once to client.

#### Configuration
- `ApiSettings` missing `GatewayAddress` fails data annotation validation.
- `DevUserContext` missing `UserName` fails validation.
- `DevUserContext` default `CustomerId` fails validation when bound from invalid/empty config.

#### Page models
- `IndexModel.OnGetAsync` loads products and returns page.
- `IndexModel.OnPostAddToCartAsync` returns not found when product is missing.
- `IndexModel.OnPostAddToCartAsync` stores updated basket and redirects to Cart.
- `ProductListModel.OnGetAsync(categoryName)` calls category endpoint branch when category exists.
- `ProductListModel.OnGetAsync` no-category branch loads full list and categories.
- `ProductListModel.OnPostAddToCartAsync` stores basket then redirects.
- `ProductDetailModel.OnGetAsync` returns not found when product absent.
- `CartModel.OnPostRemoveToCartAsync` removes item and stores basket.
- `CheckoutModel.OnPostCheckOutAsync` invalid model state returns page and does not call checkout.
- `CheckoutModel.OnPostCheckOutAsync` valid state maps `UserName`, `CustomerId`, and `TotalPrice` then redirects.
- `OrderListModel.OnGetAsync` uses current user customer id when requesting orders.

### Phase 4: Add routing and host-level integration tests

Validate startup wiring, route handler execution, and outbound call composition with deterministic doubles.

**Files**
- `test/Shopping.Web.Razor.Tests/Integration/Startup/ServiceRegistrationTests.cs`
- `test/Shopping.Web.Razor.Tests/Integration/Http/RefitContractTests.cs`
- `test/Shopping.Web.Razor.Tests/Routing/RazorPageHandlerRoutingTests.cs`

**Startup and DI cases**
- `IBasketService` resolves to `BasketService`.
- Refit clients for Catalog/Basket/Ordering are registered.
- typed options binding for `ApiSettings` and `DevUserContext` is active.

**Hybrid outbound contract cases (Decision 2: Option C)**
- Strict assertions for critical calls:
  - basket checkout `POST /basket-service/basket/checkout`
  - basket store `POST /basket-service/basket`
  - basket read `GET /basket-service/basket/{userName}`
- Semantic assertions for non-critical calls:
  - product list and category flows return expected page-model state
  - order list flow returns expected orders for current user

**Routing cases**
- GET handlers render successfully for `Index`, `ProductList`, `ProductDetail`, `Cart`, `Checkout`, and `OrderList`.
- POST handlers execute and return expected redirects for add/remove/checkout actions.
- product detail missing product branch returns not found result.

### Phase 5: Validate resilience behavior and provider-backed smoke slice

Add minimal tests for resilience behavior and real gateway-compatible execution.

**Files**
- `test/Shopping.Web.Razor.Tests/Integration/Resilience/ResiliencePipelineTests.cs`
- `test/Shopping.Web.Razor.Tests/Integration/Provider/ShoppingGatewaySmokeTests.cs`

**Resilience cases (Decision 3: Option B)**
- registration assertion: each Refit client has standard resilience handler configured.
- transient-failure assertion per client:
  - first response transient failure (e.g., 503 or timeout)
  - subsequent response success
  - final page-model/service action succeeds and proves retry behavior.

**Provider-backed smoke cases**
- category query from product list returns data from running stack.
- checkout flow submits mapped payload and reaches confirmation redirect.
- order list fetch resolves for configured dev user context.

### Phase 6: Final verification and documentation

Finalize README status and run both default and docker-backed command paths.

**Files**
- `test/Shopping.Web.Razor.Tests/README.md`

**Required verification commands**

```powershell
dotnet test test/Shopping.Web.Razor.Tests/Shopping.Web.Razor.Tests.csproj
```

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File test/Shopping.Web.Razor.Tests/run-with-docker.ps1 -Coverage
```

## 4. Test Class Layout

```text
test/Shopping.Web.Razor.Tests/
  Shopping.Web.Razor.Tests.csproj
  coverlet.runsettings
  README.md
  run-with-docker.ps1
  Support/
    ShoppingWebAppFactory.cs
    FakeHttpMessageHandler.cs
    DockerAvailability.cs
  Integration/
    ShoppingProviderCollection.cs
    Startup/
      ServiceRegistrationTests.cs
    Http/
      RefitContractTests.cs
    Resilience/
      ResiliencePipelineTests.cs
    Provider/
      ShoppingGatewaySmokeTests.cs
  Routing/
    RazorPageHandlerRoutingTests.cs
  Unit/
    Configuration/
      ApiSettingsValidationTests.cs
      DevUserContextValidationTests.cs
    Services/
      BasketServiceTests.cs
    Pages/
      IndexModelTests.cs
      ProductListModelTests.cs
      ProductDetailModelTests.cs
      CartModelTests.cs
      CheckoutModelTests.cs
      OrderListModelTests.cs
```

## 5. Dependencies and Mocking Strategy

| Dependency | Test Layer | Strategy | Example |
|---|---|---|---|
| `ICatalogService` | Unit | Moq strict setup | product lookup/list branches |
| `IBasketApiClient` | Unit | Moq strict setup | fallback and delegation behavior |
| `IBasketService` | Unit | Moq strict setup | page-model add/remove/checkout |
| `IOrderingService` | Unit | Moq strict setup | order list retrieval by user context |
| `IDevUserContextProvider` | Unit | Moq deterministic user context | checkout mapping and order list calls |
| Refit `HttpClient` pipeline | Integration | programmable `HttpMessageHandler` | method/path/query contract assertions |
| Gateway + services | Provider smoke | Docker-backed stack | minimal real contract compatibility |

## 6. Coverage Gap Checklist

- [ ] List excluded files and justification (bootstrap/seed/generated only)
- [ ] Identify non-service assemblies in coverage output and define ownership (`service` vs `shared library`)
- [ ] If shared library code appears in service coverage, require separate test project/report
- [ ] Separate mock-only vs provider-dependent coverage candidates
- [ ] Include malformed input and route edge cases (bad query params, malformed route values, missing segments)
- [ ] Include default-value behavior tests (optional parameters, default pagination behavior)
- [ ] Define Docker local run command for provider slice
- [ ] Define expected behavior when Docker is unavailable (skip for default local, fail-fast in docker script)

## 7. Execution and Verification

For local execution details (including Docker-backed smoke runs and troubleshooting), see:
- [Shopping.Web.Razor.Tests local runbook](../../test/Shopping.Web.Razor.Tests/README.md)

### Exit Criteria

- Unit, routing, and integration tests pass locally.
- Provider-backed smoke tests pass through docker runner.
- Line coverage meets or exceeds 90% for Shopping.Web.Razor-owned scope.
- Branch coverage reaches 70%+ on checkout/cart/category branches.
- Critical outbound contract checks (basket read/store/checkout) are asserted exactly.
- No CI workflow changes are introduced as part of this plan.

## 8. Gaps and Assumptions

- Gap: There is currently no Shopping.Web.Razor test project in the repository.
  - Why this matters: there is no regression safety net for modernized page-model and startup wiring behavior.
  - Recommended resolution: create `test/Shopping.Web.Razor.Tests/` and implement Phases 1-3 first.

- Gap: Existing local tooling scripts are service-centric and do not yet include a web-app docker-backed runner.
  - Why this matters: provider smoke execution can become inconsistent between developers.
  - Recommended resolution: add and document `run-with-docker.ps1` for this test project.

- Assumption: minimal provider-backed smoke tests are sufficient for first-phase confidence.
  - Why this matters: full end-to-end UI automation is intentionally out of scope.
  - Recommended resolution: revisit browser-level automation only after API contract and resilience coverage stabilize.

- Assumption: CI governance remains deferred.
  - Why this matters: mandatory PR policy cannot be encoded or enforced yet.
  - Recommended resolution: capture CI gating decision in a follow-up planning update when CI scope begins.