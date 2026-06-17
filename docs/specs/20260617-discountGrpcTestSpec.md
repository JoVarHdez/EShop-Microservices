# Test Specification: Discount.Grpc

**Source Spec**: [Discount.Grpc — .NET 10 Real-World Practices Modernization](./20260520-discountGrpcModernization.md)
**Service**: `Discount.Grpc`
**Last Updated**: 2026-06-17

## 1. Testing Objective

Define a test contract for the Discount gRPC microservice that validates coupon retrieval and mutation behavior, request validation, persistence interactions, and transport-level error semantics before implementation planning.

This scope targets the highest risk areas for this service:
- gRPC contract and status-code correctness (`OK`, `NotFound`, `InvalidArgument`, `AlreadyExists`)
- Validation interceptor short-circuit behavior
- Repository-backed data consistency for create, update, and delete flows
- Provider-coupled behavior with EF Core + SQLite (or test-equivalent provider)

## 2. Scope

### In Scope
- Unary gRPC method behavior for `GetDiscount`, `CreateDiscount`, `UpdateDiscount`, and `DeleteDiscount`.
- FluentValidation + gRPC interceptor integration for request-level input validation.
- Repository behavior for coupon lookup by `ProductId` and `ProductName`.
- Not-found, duplicate, and fallback discount paths.
- Health endpoint behavior at `GET /health` for EF Core connectivity.
- Coverage ownership boundaries for Discount service code.

### Out of Scope
- `.proto` schema redesign or new RPC methods.
- Basket API client-side resilience testing against Discount gRPC failures.
- Performance/load, soak, and security penetration testing.
- Database engine migration testing (for example SQLite to PostgreSQL).
- Deployment pipeline/workflow yaml authoring.

## 3. Test Surfaces

### 3.1 Domain and Validation
- `CreateDiscountRequestValidator` enforces non-null coupon, non-empty `ProductName`, and `Amount > 0`.
- `UpdateDiscountRequestValidator` enforces non-null coupon, non-empty `ProductName`, and `Amount >= 0`.
- `GetDiscountRequestValidator` enforces non-empty `ProductName`.
- `ValidationInterceptor` returns `InvalidArgument` without calling service continuation when validation fails.

### 3.2 API / Messaging / gRPC Contracts
- `GetDiscount` returns an existing coupon when found by valid `ProductId`.
- `GetDiscount` falls back to `ProductName` when `ProductId` is missing/invalid.
- `GetDiscount` returns synthetic zero-discount coupon when no persisted coupon exists.
- `CreateDiscount` returns created coupon and maps duplicate repository exceptions to `AlreadyExists`.
- `UpdateDiscount` returns updated coupon when found; returns `NotFound` when target coupon does not exist.
- `DeleteDiscount` returns success on delete and `NotFound` when target does not exist.

### 3.3 Routing and Dispatch
- gRPC service is mapped and callable via HTTP/2 at runtime.
- Reflection endpoint is available only in Development.
- Validation interceptor is globally wired for gRPC unary calls.
- HTTP health route `/health` is reachable and returns environment-appropriate health status.

### 3.4 Data Access and Provider-Coupled Paths
- `DiscountRepository.GetDiscountByProductIdAsync` and `GetDiscountAsync` query correctness.
- `CreateDiscountAsync` duplicate detection by `ProductName` and persistence behavior.
- `UpdateDiscountAsync` find-then-update semantics (update mutable fields only).
- `DeleteDiscountAsync` removes existing entities and returns `false` when absent.
- Seed data visibility for first-run lookup behavior.

## 4. Strategy and Environment

### 4.1 Test Layering
- Unit: validators, interceptor behavior, `DiscountService` method decisions with mocked `IDiscountRepository`.
- Integration: gRPC endpoint execution over in-memory test host plus EF-backed repository behavior.
- Routing: startup/mapping tests for gRPC service registration and `/health` endpoint reachability.

### 4.2 Mock-Solvable vs Provider-Dependent Classification
- Mock-solvable:
  - Validator rule coverage.
  - Interceptor short-circuit and pass-through behavior.
  - `DiscountService` status-code branching (`NotFound`, `AlreadyExists`) with repository mocks.
  - ProductId parsing fallback logic in `GetDiscount`.
- Provider-dependent:
  - EF Core query translation and persistence behavior in `DiscountRepository`.
  - Duplicate detection and update/delete behavior against a real relational provider.
  - Health check behavior when database connectivity is available/unavailable.

### 4.3 Provider-Dependent Execution Policy
- Real-provider mechanism: Docker-backed integration slice (preferred) or Testcontainers for deterministic local/CI runs.
- Docker policy: `skip` locally when Docker is unavailable; `fail-fast` in CI where provider-backed suite is required.
- Local command baseline:
  - `dotnet test test/Discount.Tests/Discount.Tests.csproj`
  - Docker-backed suite command to be added once `test/Discount.Tests` scaffolding exists.

## 5. Quality Gates

- Line coverage target: 90% on service-owned Discount code paths.
- Branch coverage target: 70%+ on critical contract and error branches.
- Reliability gate: all required unit, routing, and provider-backed integration tests pass.
- Contract gate: all gRPC method and status-code assertions pass.

### Coverage Ownership
- Service-owned code included:
  - `src/Services/Discount/Discount.Grpc/Services/`
  - `src/Services/Discount/Discount.Grpc/Repository/`
  - `src/Services/Discount/Discount.Grpc/Interceptors/`
  - `src/Services/Discount/Discount.Grpc/Validators/`
- Shared library code excluded from service KPI:
  - `src/BuildingBlocks/**`
- Shared library test project/report:
  - `test/BuildingBlocks.Tests/` (separate ownership and report)

## 6. Required Edge Cases

- [ ] Pagination defaults and nullable query behavior
  - Not applicable for Discount gRPC surface (no pagination contract).
- [x] Empty result semantics for filters/categories
  - No persisted coupon returns synthetic `CouponModel` with `Amount = 0` and default description.
- [x] Malformed route/query combinations
  - Invalid `ProductId` string in `GetDiscountRequest` must not fail request; flow falls back to `ProductName` lookup.
- [x] Not-found branches
  - `UpdateDiscount` and `DeleteDiscount` return `StatusCode.NotFound` for absent coupon.
- [x] Validation failure behavior and no-dispatch checks
  - Empty `ProductName`, null coupon, and invalid amount are rejected with `InvalidArgument` before service method executes.

## 7. Gaps and Assumptions

- Gap: Formal test project for Discount service is not present in `test/`.
  - Impact: quality gates cannot be enforced until test project scaffolding exists.
  - Resolution: create `test/Discount.Tests` and align structure with Catalog/Basket test projects.

- Gap: Provider-backed execution script for Discount tests is not defined.
  - Impact: CI reproducibility for provider-coupled tests remains ambiguous.
  - Resolution: add docker-backed run script similar to other service test projects once tests are scaffolded.

- Assumption: Existing business rule for zero-discount fallback remains valid and intentional.
  - Impact: changing this behavior would require contract updates across callers.
  - Resolution: confirmed in Open Questions decisions (Option A).

## 8. Open Questions (Resolved)

- Question: Should zero-discount fallback in `GetDiscount` remain the default behavior when no coupon exists?
  - Why this matters: affects contract compatibility and not-found semantics for all consumers.
  - Option A: Keep synthetic zero-discount coupon response.
    - Implication: highest backward compatibility; hides explicit not-found distinction.
  - Option B: Return `NotFound` for missing coupons.
    - Implication: clearer semantics; likely requires downstream client updates.
  - Decision: Option A (same as current running flow).

- Question: Which provider strategy should be mandatory in CI for repository integration tests?
  - Why this matters: balances execution speed against production-like confidence.
  - Option A: SQLite in-memory only.
    - Implication: fastest execution; lower fidelity for file/connection and migration behavior.
  - Option B: Docker/Testcontainers relational instance for provider-coupled slice.
    - Implication: stronger confidence; slower and depends on container runtime.
  - Decision: Option B.

- Question: Should duplicate coupon detection (`AlreadyExists`) be enforced only at repository logic or also via unique DB constraint validation in tests?
  - Why this matters: affects long-term data integrity and race-condition resilience.
  - Option A: Validate application-level duplicate checks only.
    - Implication: simpler test setup; weaker guarantees under concurrent writes.
  - Option B: Validate both application behavior and DB-level uniqueness enforcement.
    - Implication: stronger integrity confidence; requires schema/index assertion in integration tests.
  - Decision: Option A.

## 9. Learning Notes

- Decision: Separate mock-solvable logic from provider-dependent behavior.
  - Why: keeps feedback loop fast while preserving meaningful infrastructure validation.
  - What we learned: most gRPC status branching can be verified without DB coupling.
  - Watch next: drift between mocked assumptions and repository implementation.

- Decision: Keep service coverage ownership bounded to Discount folders.
  - Why: avoids inflating/deflating service quality metrics with shared-library code.
  - What we learned: explicit ownership boundaries make coverage gates actionable.
  - Watch next: ensure exclusions stay aligned with project refactors.

## 10. Handoff to Test Plan

- Planned test project: `test/Discount.Tests/`
- README update required: yes
- Next command/skill: `/generate-test-plan` with `docs/specs/20260617-discountGrpcTestSpec.md`
