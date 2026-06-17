# Test Specification: Ordering

**Source Spec**: [Ordering Module — Wolverine & Native Minimal API Modernization](./20260525-orderingModuleWolverineModernization.md)
**Service**: `Ordering`
**Last Updated**: 2026-06-17

## 1. Testing Objective

Define a test contract for the Ordering microservice that validates HTTP endpoint behavior, Wolverine command dispatch, EF Core-backed query and persistence flows, domain event dispatch, and Basket checkout message consumption before detailed test planning begins.

This scope targets the highest-risk areas for Ordering as it currently exists:
- Minimal API contract correctness for the six `/orders` routes plus `/health`
- FluentValidation short-circuit behavior on create, update, and delete commands
- SQL Server-backed persistence behavior for create, update, delete, pagination, and filtering queries
- Domain event dispatch through `DispatchDomainEventsInterceptor`
- Integration-message mapping from `BasketCheckoutEvent` into `CreateOrderCommand`

## 2. Scope

### In Scope
- HTTP behavior for `GET /orders`, `GET /orders/{orderName}`, `GET /orders/customer/{customerId}`, `POST /orders`, `PUT /orders`, `DELETE /orders/{id}`, and `GET /health`.
- Validator behavior for `CreateOrderCommand`, `UpdateOrderCommand`, and `DeleteOrderCommand`.
- Command handler behavior for order creation, update, delete, and not-found branches.
- Query handler behavior for pagination, customer filtering, name filtering, ordering, and empty-result semantics.
- Domain event publication from EF Core save interception and downstream domain handler behavior.
- Basket checkout consumer mapping and dispatch behavior in `BasketCheckoutEventHandler`.
- Coverage ownership boundaries for Ordering-owned code.

### Out of Scope
- End-to-end UI workflow testing for `Shopping.Web.Razor`.
- Performance, load, soak, or concurrency benchmarking.
- Security penetration testing, DAST, or infrastructure vulnerability scanning.
- MassTransit broker infrastructure provisioning or transport reliability benchmarking.
- Schema redesign, migration authoring, or changes to Ordering domain contracts.
- CI workflow yaml authoring.

## 3. Test Surfaces

### 3.1 Domain and Validation
- `CreateOrderCommandValidator` rejects empty `OrderName`, missing `CustomerId`, and empty `OrderItems`.
- `UpdateOrderCommandValidator` rejects empty `Id`, missing `CustomerId`, and empty `OrderName`.
- `DeleteOrderCommandValidator` rejects empty `OrderId`.
- `Order.Create` raises `OrderCreatedEvent` and computes `TotalAmount` from added items.
- `Order.Update` raises `OrderUpdatedEvent` and replaces mutable order details.
- `Order.Add` rejects non-positive quantity and non-positive price.

### 3.2 API / Messaging / gRPC Contracts
- `POST /orders` returns `201 Created` with a new order identifier when the request is valid.
- `PUT /orders` returns `200 OK` with `IsSuccess = true` when the order exists and `404 Not Found` when it does not.
- `DELETE /orders/{id}` returns `200 OK` with `IsSuccess = true` when the order exists and `404 Not Found` when it does not.
- `GET /orders` returns a paginated payload containing `PageIndex`, `PageSize`, `Count`, and ordered order DTO data.
- `GET /orders/{orderName}` returns all matching orders ordered by `OrderName` and returns `200 OK` with an empty collection when none match.
- `GET /orders/customer/{customerId}` returns all matching customer orders ordered by `OrderName` and returns `200 OK` with an empty collection when none match.
- `GET /health` remains reachable and reflects SQL Server dependency health.
- `BasketCheckoutEventHandler` maps checkout payload fields into a new `CreateOrderCommand` and invokes Wolverine exactly once per consumed message.

### 3.3 Routing and Dispatch
- `MapOrdersEndpoints` registers all six order routes under `/orders` with the expected verbs and path templates.
- Route precedence remains unambiguous between `/{orderName}` and `/customer/{customerId}`.
- Command endpoints dispatch through `IMessageBus.InvokeAsync` and return typed HTTP responses for success, validation failure, and not-found branches.
- Query endpoints resolve injected query-handler services directly without message-bus dispatch.
- `DispatchDomainEventsInterceptor` collects domain events from tracked aggregates, clears them, and publishes each through `IMessageBus.PublishAsync` during save operations.
- `OrderCreatedEventHandler` publishes an integration event only when feature flag `OrderFullfilment` is enabled.
- `OrderUpdatedEventHandler` completes successfully without mutating persistence state.

### 3.4 Data Access and Provider-Coupled Paths
- `CreateOrderHandler` persists a fully mapped order aggregate, including addresses, payment, order items, and computed total amount.
- `UpdateOrderHandler` locates by strongly typed `OrderId`, updates mutable values, and persists changes.
- `DeleteOrderHandler` locates by strongly typed `OrderId` and removes the entity.
- `GetOrdersHandler` applies pagination (`PageIndex`, `PageSize`), total count lookup, item inclusion, and `OrderName` ordering.
- `GetOrderByCustomerHandler` filters by `CustomerId`, includes order items, uses no-tracking query semantics, and returns ordered DTO results.
- `GetOrdersByNameHandler` performs substring filtering on `OrderName`, includes order items, uses no-tracking query semantics, and returns ordered DTO results.
- Seeded data path in `InitialData` provides a stable provider-backed slice for integration assertions where explicit fixture seeding is not preferred.

## 4. Strategy and Environment

### 4.1 Test Layering
- Unit: validators, aggregate invariants, command-handler decision branches, query-handler shaping with controlled EF test contexts, domain-event handler branching, and Basket checkout message-to-command mapping.
- Integration: HTTP endpoint execution over a test host with real DI wiring plus SQL Server-backed persistence assertions for create, update, delete, pagination, filtering, health, and domain-event dispatch.
- Routing: startup and endpoint registration tests verifying verb/path mapping, route disambiguation, and response codes for representative success and failure paths.

### 4.2 Mock-Solvable vs Provider-Dependent Classification
- Mock-solvable:
  - Validator rule coverage for create, update, and delete commands.
  - `UpdateOrderHandler` and `DeleteOrderHandler` not-found branching.
  - `BasketCheckoutEventHandler` field mapping and single dispatch call.
  - `OrderCreatedEventHandler` feature-flag branch behavior.
  - `DispatchDomainEventsInterceptor` publish-count behavior against tracked aggregates.
- Provider-dependent:
  - EF Core SQL Server persistence for create, update, and delete flows.
  - Pagination/count correctness and ordering behavior in `GetOrdersHandler`.
  - Query translation for customer filtering and name substring filtering.
  - Health-check behavior when SQL Server connectivity is available or unavailable.
  - End-to-end HTTP + DI + Wolverine middleware interaction for validation and command execution.

### 4.3 Provider-Dependent Execution Policy
- Real-provider mechanism: SQL Server in Docker or Testcontainers-backed integration slice.
- Docker policy: `skip` locally when Docker is unavailable; `fail-fast` in CI when provider-backed Ordering tests are enabled.
- Local command baseline:
  - `dotnet test test/Ordering.Tests/Ordering.Tests.csproj`
  - Docker-backed command to be added when `test/Ordering.Tests` and its run script are scaffolded.

## 5. Quality Gates

- Line coverage target: 90% on Ordering-owned critical paths.
- Branch coverage target: 70%+ on contract, validation, and not-found branches.
- Reliability gate: all required unit, routing, and provider-backed integration tests pass.
- Contract gate: all route, response-code, and message-mapping assertions pass.

### Coverage Ownership
- Service-owned code included:
  - `src/Services/Ordering/Ordering.API/Endpoints/`
  - `src/Services/Ordering/Ordering.Application/Orders/`
  - `src/Services/Ordering/Ordering.Application/Extensions/`
  - `src/Services/Ordering/Ordering.Infrastructure/Data/`
  - `src/Services/Ordering/Ordering.Infrastructure/Extensions/`
  - `src/Services/Ordering/Ordering.Core/`
- Shared library code excluded from service KPI:
  - `src/BuildingBlocks/**`
- Shared library test project/report:
  - `test/BuildingBlocks.Tests/`

## 6. Required Edge Cases

- [x] Pagination defaults and nullable query behavior
  - `GET /orders` must honor default `PaginationRequest(PageIndex = 0, PageSize = 10)` when query parameters are absent and must preserve caller-provided pagination values when present.
- [x] Empty result semantics for filters/categories
  - `GET /orders/{orderName}` and `GET /orders/customer/{customerId}` must return `200 OK` with an empty collection when no matches exist.
- [x] Malformed route/query combinations
  - Invalid `Guid` input for `/orders/customer/{customerId}` or `/orders/{id}` delete route must fail model binding with `400 Bad Request`; `/orders/customer/{customerId}` must not be captured by the `/{orderName}` route.
- [x] Not-found branches
  - `PUT /orders` and `DELETE /orders/{id}` must return `404 Not Found` when the target order is absent.
- [x] Validation failure behavior and no-dispatch checks
  - Invalid create, update, and delete requests must return `400 Bad Request` via FluentValidation/Wolverine middleware and must not persist or mutate orders.

## 7. Gaps and Assumptions

- Gap: `test/Ordering.Tests/` does not exist.
  - Impact: Ordering-specific quality gates, coverage reporting, and executable regression protection cannot be enforced yet.
  - Resolution: scaffold `test/Ordering.Tests` with unit, routing, and integration folders aligned to other service test projects.

- Gap: No docker-backed Ordering test runner script exists.
  - Impact: provider-coupled SQL Server test execution remains undocumented and may vary across developers.
  - Resolution: add a `run-with-docker.ps1` script once the Ordering test project is created.

- Assumption: SQL Server remains the authoritative integration-test provider because production wiring uses `UseSqlServer` directly.
  - Impact: substituting an in-memory provider alone would under-test query translation and health-check behavior.
  - Resolution: retain a minimal SQL Server-backed slice even if some unit tests use cheaper EF test doubles.

- Assumption: Basket checkout message handling is part of Ordering's owned regression surface even though transport configuration is shared through BuildingBlocks messaging.
  - Impact: excluding it would leave a meaningful order-creation entry point unguarded.
  - Resolution: include consumer mapping and dispatch in the first test plan phase.

## 8. Open Questions (Resolved)

- Question: What CI execution depth should be mandatory for Ordering once the test project exists?
  - Why this matters: determines feedback speed, infrastructure cost, and how quickly contract regressions are caught.
  - Option A: Run unit and routing tests on pull requests; run provider-backed integration tests on main/nightly.
    - Implication: fastest PR feedback, but SQL Server regressions may be detected later.
  - Option B: Run unit, routing, and a minimal provider-backed integration slice on every pull request.
    - Implication: better confidence on critical data-access paths with moderate runtime cost.
  - Option C: Run the full Ordering suite, including all provider-backed scenarios, on every pull request.
    - Implication: strongest immediate confidence, but highest flakiness and duration risk.
  - Decision: Deferred. CI execution policy is out of scope until CI work begins.

- Question: Which provider strategy should be the baseline for Ordering integration tests?
  - Why this matters: affects fidelity, test speed, and maintenance effort for persistence-heavy paths.
  - Option A: SQL Server Docker/Testcontainers baseline.
    - Implication: highest production fidelity for queries, health checks, and mappings; requires container runtime.
  - Option B: SQLite baseline with a smaller SQL Server smoke slice.
    - Implication: cheaper execution, but some SQL Server-specific behavior may drift.
  - Option C: EF Core in-memory provider for most integration tests.
    - Implication: simplest setup, but insufficient confidence for query translation and relational behavior.
  - Decision: Option A.

- Question: Should domain event publication be asserted as part of endpoint integration tests or isolated to handler/interceptor tests?
  - Why this matters: changes the balance between end-to-end confidence and test isolation.
  - Option A: Assert event publication only in focused interceptor and domain-handler tests.
    - Implication: simpler endpoint tests, but less cross-layer coverage.
  - Option B: Assert at least one end-to-end create/update integration path also publishes the expected domain event.
    - Implication: stronger slice confidence with modest added complexity.
  - Option C: Exclude domain event publication from first-phase automated coverage.
    - Implication: fastest initial delivery, but leaves a regression-prone behavior unverified.
  - Decision: Option B.

## 9. Learning Notes

- Decision: Treat Ordering as both an HTTP service and a message-driven service.
  - Why: `BasketCheckoutEventHandler` is a first-class order-creation entry point, not incidental infrastructure.
  - What we learned: API-only coverage would miss a meaningful production path.
  - Watch next: whether shared messaging abstractions introduce extra harness setup in the eventual test plan.

- Decision: Keep provider-backed coverage focused on SQL Server-sensitive slices.
  - Why: most branch logic is cheap to isolate, while query translation and health behavior need real-provider verification.
  - What we learned: provider-coupled testing can stay minimal without weakening the contract.
  - Watch next: whether route and middleware coverage already exercise enough of the same persistence paths to avoid redundancy.

- Decision: Bound coverage ownership to Ordering folders and exclude BuildingBlocks from the Ordering KPI.
  - Why: service coverage should reflect the team's direct ownership and avoid shared-library noise.
  - What we learned: explicit ownership boundaries keep coverage targets actionable.
  - Watch next: whether future refactors move shared logic into BuildingBlocks and require exclusion updates.

## 10. Handoff to Test Plan

- Planned test project: `test/Ordering.Tests/`
- README update required: yes
- Next command/skill: `/generate-test-plan` with `docs/specs/20260617-orderingApiTestSpec.md`