# Test Specification: Shopping.Web.Razor

**Source Spec**: [Shopping.Web.Razor - .NET 10 Real-World Modernization](./20260604-shoppingWebRazorDotnet10Modernization.md)
**Service**: `Shopping.Web.Razor`
**Last Updated**: 2026-06-17

## 1. Testing Objective

Define a test contract for the Shopping Razor Pages web app that validates UI-facing page-model behavior, outbound API client composition, options/configuration safety, and request routing reliability after the .NET 10 modernization.

This scope targets the highest-risk areas introduced or changed by the modernization work:
- Options binding and startup validation for `ApiSettings` and `DevUserContext`
- Consolidated Refit client registration and resilience middleware wiring
- Separation of HTTP contract (`IBasketApiClient`) from basket business logic (`IBasketService`/`BasketService`)
- Backend-driven product category filtering in `ProductListModel`
- Cross-page basket and checkout flow consistency

## 2. Scope

### In Scope
- Startup/service registration behavior in `Program.cs` and `ServiceExtensions.cs`.
- `BasketService` fallback behavior when basket retrieval fails.
- Page-model behavior for `Index`, `ProductList`, `ProductDetail`, `Cart`, `Checkout`, and `OrderList`.
- Outbound contract usage for Catalog, Basket, and Ordering Refit interfaces.
- Route handler behavior for page-model GET/POST handlers and expected redirect/page/not-found outcomes.

### Out of Scope
- Browser UI automation (Playwright/Selenium), visual regression, or CSS rendering fidelity.
- End-to-end tests against real backend microservices deployed in shared environments.
- Performance/load testing and front-end Lighthouse-style audits.
- Authentication/authorization implementation (still intentionally dev-user based).
- CI workflow yaml authoring.

## 3. Test Surfaces

### 3.1 Domain and Validation
- `ApiSettings.GatewayAddress` is required and must fail startup validation when missing.
- `DevUserContext.UserName` and `DevUserContext.CustomerId` are required and must fail startup validation when missing or invalid.
- `BasketService.LoadUserBasketAsync` returns API cart payload on success.
- `BasketService.LoadUserBasketAsync` returns an empty cart with current username when API read fails.
- `CheckoutModel.OnPostCheckOutAsync` must short-circuit to `Page()` when `ModelState` is invalid.

### 3.2 API / Messaging / gRPC Contracts
- `ICatalogService` calls:
  - `GET /catalog-service/products`
  - `GET /catalog-service/products/{id}`
  - `GET /catalog-service/products/category/{category}`
- `IBasketApiClient` calls:
  - `GET /basket-service/basket/{userName}`
  - `POST /basket-service/basket`
  - `DELETE /basket-service/basket/{userName}`
  - `POST /basket-service/basket/checkout`
- `IOrderingService` calls:
  - `GET /ordering-service/orders/customer/{customerId}` for order list retrieval.
- Checkout page must map user context + basket total into checkout request before dispatching basket checkout.

### 3.3 Routing and Dispatch
- Razor Pages route dispatch must execute the correct handler methods for:
  - `OnGetAsync` for `Index`, `ProductList`, `ProductDetail`, `Cart`, `Checkout`, and `OrderList`
  - `OnPostAddToCartAsync` for `Index`, `ProductList`, and `ProductDetail`
  - `OnPostRemoveToCartAsync` for `Cart`
  - `OnPostCheckOutAsync` for `Checkout`
- Success paths must return expected navigation results (`RedirectToPage("Cart")`, `RedirectToPage("Confirmation", "OrderSubmitted")`, or `Page()`).
- Not-found branch in product lookups must return `NotFound()` when product payload is null.

### 3.4 Data Access and Provider-Coupled Paths
- There is no direct datastore provider in Shopping.Web.Razor; provider dependency is external HTTP APIs behind YARP gateway.
- A minimal provider-coupled slice should run against a reachable gateway + dependent services to verify:
  - Catalog category filtering returns backend-filtered results.
  - Basket checkout flow performs end-to-end request mapping and redirect behavior.
  - Order list retrieval uses current dev user context and renders without contract mismatch.

## 4. Strategy and Environment

### 4.1 Test Layering
- Unit: page-model method branching, basket fallback behavior, configuration validation behavior, and request-object mapping assertions with mocked service dependencies.
- Integration: ASP.NET Core test host (`WebApplicationFactory`) validating DI/options registration, route execution, and page-model behavior with deterministic API doubles.
- Routing: handler-selection and result-type assertions for GET/POST Razor Page handlers.

### 4.2 Mock-Solvable vs Provider-Dependent Classification
- Mock-solvable:
  - Basket fallback semantics in `BasketService`.
  - Page-model redirect/not-found/page return branches.
  - Product add/remove cart mutation behavior.
  - Checkout mapping from basket and dev user context.
  - Service registration assertions for `IBasketService`, `IBasketApiClient`, `ICatalogService`, and `IOrderingService`.
- Provider-dependent:
  - Real gateway base-address behavior and resilience interaction under transient HTTP failures.
  - Category filter correctness against backend endpoint behavior.
  - End-to-end checkout and order retrieval contract compatibility with live microservices.

### 4.3 Provider-Dependent Execution Policy
- Real-provider mechanism: Docker Compose-backed eShop stack (YARP gateway + Basket/Catalog/Ordering services).
- Docker policy: `skip` locally when Docker/services are unavailable; `fail-fast` in CI when provider-backed Shopping.Web.Razor tests are explicitly enabled.
- Local command baseline:
  - `dotnet test test/Shopping.Web.Razor.Tests/Shopping.Web.Razor.Tests.csproj`
  - Provider-backed command to be added once a docker-backed test runner script is introduced.

## 5. Quality Gates

- Line coverage target: 90% on Shopping.Web.Razor-owned critical paths.
- Branch coverage target: 70%+ on page-model control flow and configuration/error branches.
- Reliability gate: all required unit, routing, and local integration tests pass.
- Contract gate: all API contract-shape and checkout-mapping assertions pass.

### Coverage Ownership
- Service-owned code included:
  - `src/WebApps/Shopping.Web.Razor/Program.cs`
  - `src/WebApps/Shopping.Web.Razor/ServiceExtensions.cs`
  - `src/WebApps/Shopping.Web.Razor/Services/`
  - `src/WebApps/Shopping.Web.Razor/Pages/*.cshtml.cs`
  - `src/WebApps/Shopping.Web.Razor/Models/ApiSettings.cs`
  - `src/WebApps/Shopping.Web.Razor/Models/DevUserContext.cs`
- Shared library code excluded from service KPI:
  - `src/BuildingBlocks/**`
- Shared library test project/report:
  - `test/BuildingBlocks.Tests/`

## 6. Required Edge Cases

- [x] Pagination defaults and nullable query behavior
  - Catalog page-model flows should preserve service defaults when pagination parameters are omitted.
- [x] Empty result semantics for filters/categories
  - Category-selected product list should return `Page()` with empty `ProductList` when backend returns no products.
- [x] Malformed route/query combinations
  - Invalid or missing `productId`/form payloads must not mutate cart state and should return framework-consistent results.
- [x] Not-found branches
  - Product detail and add-to-cart retrieval paths should return `NotFound()` when product payload is null.
- [x] Validation failure behavior and no-dispatch checks
  - Invalid checkout model state must return `Page()` and must not invoke checkout dispatch.

## 7. Gaps and Assumptions

- Gap: `test/Shopping.Web.Razor.Tests/` does not exist yet.
  - Impact: no executable regression suite currently protects page-model behavior and startup wiring.
  - Resolution: scaffold a dedicated Shopping.Web.Razor test project with `Unit`, `Routing`, and `Integration` folders.

- Gap: No docker-backed test runner script exists for Shopping.Web.Razor provider-coupled scenarios.
  - Impact: local execution of real-gateway tests may be inconsistent across developers.
  - Resolution: add `test/Shopping.Web.Razor.Tests/run-with-docker.ps1` aligned with existing service test scripts.

- Assumption: deterministic API doubles are acceptable for the default integration slice to keep feedback fast.
  - Impact: contract drift risk remains until provider-backed slice is also executed.
  - Resolution: include a minimal provider-backed smoke slice for category filtering + checkout + order retrieval.

- Assumption: dev-user context remains intentional and stable for current testing scope.
  - Impact: auth transition later will require updating test harness identity setup.
  - Resolution: isolate user-context setup behind `IDevUserContextProvider` test doubles to reduce churn.

## 8. Open Questions

1. Question: What should be mandatory on pull requests for Shopping.Web.Razor tests?
  - Why this matters: controls feedback speed versus confidence for UI-facing workflows.
  - Status: Deferred; CI is not part of this process yet.
  - Option A: Unit + routing mandatory on pull requests; provider-backed integration nightly.
    - Implication: fastest pull request cycle, but gateway contract regressions can be detected later.
  - Option B: Unit + routing + minimal provider-backed smoke tests mandatory on pull requests.
    - Implication: higher confidence on critical flows with moderate runtime increase.
  - Option C: Full provider-backed suite mandatory on every pull request.
    - Implication: strongest confidence with highest CI cost and flakiness risk.
  - Decision: Deferred until CI work starts.

2. Question: How should outbound API calls be verified in integration tests?
  - Why this matters: affects brittleness and test maintenance as contracts evolve.
  - Option A: Assert concrete URL/path/query and HTTP verb from recorded handlers.
    - Implication: precise contract guarantees; higher maintenance if endpoint paths change.
  - Option B: Assert only semantic behavior via stubbed service abstractions.
    - Implication: easier maintenance; reduced confidence on actual HTTP contract shape.
  - Option C: Hybrid - exact contract assertions for critical checkout/cart calls, semantic assertions elsewhere.
    - Implication: balanced confidence and maintainability.
  - Decision: Option C.

3. Question: Should resilience behavior be directly validated in the first test phase?
  - Why this matters: resilience wiring is new and can silently regress.
  - Option A: Validate registration only (presence of standard resilience handler).
    - Implication: low effort; runtime retry/circuit behavior not empirically proven.
  - Option B: Validate registration plus one transient-failure retry scenario per client.
    - Implication: better confidence with moderate harness complexity.
  - Option C: Defer all resilience behavior checks.
    - Implication: fastest delivery; risk of unnoticed runtime regressions.
  - Decision: Option B.

## 9. Learning Notes

- Decision: Keep the largest test surface at page-model/service level, not UI browser automation, for first-phase coverage.
  - Why: modernization changed application composition and business flow orchestration more than HTML/CSS rendering.
  - What we learned: unit+routing coverage can quickly protect key regressions in Razor Pages apps.
  - Watch next: add focused browser smoke checks only after backend contract coverage stabilizes.

- Decision: Treat gateway-backed checks as a minimal, intentional provider-coupled slice.
  - Why: this app is mostly an orchestrator over external services; pure mocks alone under-test integration risk.
  - What we learned: a thin provider slice captures high-value regressions without making all tests expensive.
  - Watch next: monitor CI duration and flakiness before expanding provider-backed coverage.

- Decision: Explicitly separate coverage ownership for Shopping.Web.Razor from shared libraries.
  - Why: coverage KPIs should reflect code owned by the web-app team.
  - What we learned: ownership boundaries keep quality gates actionable and reduce metric noise.
  - Watch next: update exclusions if shared abstractions move between web app and BuildingBlocks.

## 10. Handoff to Test Plan

- Planned test project: `test/Shopping.Web.Razor.Tests/`
- README update required: yes
- Next command/skill: `/generate-test-plan` with `docs/specs/20260617-shoppingWebRazorTestSpec.md`