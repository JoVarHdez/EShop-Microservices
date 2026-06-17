# Basket API - Test Specification

## 1. Feature Summary

Basket API test specification defines the required verification scope for the Basket microservice so backend engineers and QA can validate basket CRUD, checkout publishing, discount application, caching behavior, and error handling before release. This specification ensures service behavior is correct and stable across unit, routing, and integration layers for the Basket domain in the eShop platform.

## 2. Data Model / Entities

### BasketEndpointContract
- Route: API route under /basket or /health.
- HttpMethod: expected HTTP method for the route.
- ExpectedStatusCodes: allowed success and failure codes.
- RequestShape: required request payload fields.
- ResponseShape: required response payload fields.

### BasketCommandContract
- CommandName: command type invoked through Wolverine.
- ValidatorRules: required command-level validation rules.
- SuccessResult: expected command result payload.
- FailureResult: expected error behavior when validation or preconditions fail.

### BasketRepositoryInteraction
- Operation: Get, Store, or Delete basket operation.
- PrimaryStoreBehavior: expected Marten persistence behavior.
- CacheBehavior: expected Redis cache read/write/remove behavior.
- ConsistencyExpectation: expected consistency between cache and primary store.

### BasketCheckoutEventContract
- EventName: BasketCheckoutEvent.
- RequiredFields: checkout identity, customer, payment, address, and total price fields.
- LineItems: Items list with ProductId, ProductName, Quantity, and UnitPrice.
- PublishCondition: basket must exist for the requested user.
- PostPublishEffect: basket is deleted after successful publish.

### TestExecutionProfile
- TestLayer: Unit, Routing, or Integration.
- DependencyMode: mocked dependency or infrastructure-backed execution.
- RequiredCoverage: minimum target for line and branch coverage.
- PassGate: conditions that must pass in CI to accept changes.

## 3. Business Rules & Constraints

The system MUST enforce the following non-negotiable rules:

1. GET /basket/{userName} MUST return 200 with cart payload when basket exists and 404 when it does not.
2. POST /basket MUST dispatch StoreBasketCommand, apply discount deduction per cart line through Discount gRPC, persist basket, and return 201 with UserName.
3. DELETE /basket/{userName} MUST dispatch DeleteBasketCommand and return 200 with Success true after repository delete call.
4. POST /basket/checkout MUST return 404 when the user basket does not exist.
5. POST /basket/checkout MUST publish one BasketCheckoutEvent when basket exists, including line items copied from the basket and TotalPrice recalculated from current basket state.
6. Successful checkout MUST delete the user basket after publishing the integration event.
7. Cached basket repository MUST return cached cart when present, populate cache on cache miss with non-null repository result, and remove cache entry on delete.
8. Command validators MUST reject null/empty command payload requirements, specifically cart presence and username for store and checkout flows, and username for delete flow.
9. Health endpoint MUST remain available at /health and report dependency health for PostgreSQL and Redis.
10. Test suite MUST gate merges with at least 90 percent line coverage and 70 percent branch coverage on Basket API critical paths.

## 4. Acceptance Criteria

The feature is complete when:

- [ ] A Basket API test project or test suite section contains unit tests for StoreBasket, DeleteBasket, and CheckoutBasket command handlers and validators.
- [ ] Routing tests verify all Basket API routes and methods: GET /basket/{userName}, POST /basket, DELETE /basket/{userName}, POST /basket/checkout, and GET /health.
- [ ] Integration tests verify checkout success path publishes BasketCheckoutEvent with populated Items and deletes basket.
- [ ] Integration tests verify checkout not-found path returns 404 and does not publish an event.
- [ ] Repository tests verify CachedBasketRepository cache-hit, cache-miss, store, and delete behavior.
- [ ] Tests verify discount deduction is applied to each basket line before store persistence.
- [ ] CI test execution fails when Basket API tests fail or coverage thresholds are not met.
- [ ] A documented command exists to execute Basket API tests in local and CI environments.

## 5. Out of Scope

The following are explicitly NOT part of this feature:

- End-to-end UI workflow testing for Shopping.Web.Razor.
- Performance, load, or soak benchmarking for Basket API.
- Contract testing for external services beyond Basket-owned request and event payload assertions.
- Security penetration testing, DAST, or infrastructure vulnerability scanning.
- Re-architecting Basket service runtime dependencies, storage model, or transport infrastructure.

## 6. Open Questions (Resolved)

The following decisions were provided and are now locked for implementation and test planning:

1. **Should Basket API test gates block all pull requests or only changes touching Basket and shared event contracts?**
   - Context: Global blocking increases confidence but may slow unrelated deliveries.
   - Options:
     - A) Block all pull requests.
     - B) Block only pull requests that touch Basket API or BasketCheckoutEvent contract files.
     - C) Warn only (non-blocking) until test suite maturity reaches target stability.
  - Decision: B) Block only pull requests that touch Basket API or BasketCheckoutEvent contract files.

2. **What test execution depth should be required in CI for external dependencies?**
   - Context: Full infrastructure-backed execution improves confidence but increases build duration and flakiness risk.
   - Options:
     - A) Unit and routing only in CI, full integration nightly.
     - B) Full unit, routing, and integration on each pull request.
     - C) Risk-based split: critical path integration on pull request, full suite on main branch.
  - Decision: B) Full unit, routing, and integration on each pull request.

3. **Should discount gRPC behavior be validated with real service containers or deterministic test doubles by default?**
   - Context: Real service tests detect integration drift, while doubles provide faster and more deterministic feedback.
   - Options:
     - A) Real Discount gRPC container by default.
     - B) Test doubles by default, real integration in scheduled pipeline.
     - C) Hybrid by environment flag with mandatory weekly real-service run.
  - Decision: B) Test doubles by default, real integration in scheduled pipeline.
