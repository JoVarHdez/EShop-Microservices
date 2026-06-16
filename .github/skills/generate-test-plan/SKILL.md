---
name: generate-test-plan
description: 'Generate comprehensive test plans from specifications for .NET microservices with focus on unit tests, code coverage (90% target), routing tests (API/messaging/gateway), and xUnit + Moq framework. Use when: create test plan from spec, generate unit tests from specification, test plan with coverage goals, routing test strategy, test API endpoints, test message handlers, test YARP gateway.'
argument-hint: 'Provide the spec file path (docs/specs/*.md) or spec name'
---

# Generate Test Plan

## When to Use

- You have a finalized specification and need a test plan before implementation
- You need to define unit test scope, integration test routes, and coverage targets
- You want to ensure API, message handler, and gateway routing are comprehensively tested
- You need to align test strategy with spec requirements (BasketCheckoutEvent, gRPC discount lookup, Wolverine handlers, etc.)

## Procedure

### 1. Provide the Specification

Either:
- Paste the spec content directly
- Reference a spec file from `docs/specs/` (e.g., `20260515-catalogApiWolverineModernization.md`)
- Describe the feature or service to test

### 2. Analyze Spec for Test Categories

The agent will extract:
- **Business logic & domain models**: Unit test candidates
- **API endpoints/handlers**: Routing and integration test candidates
- **External integrations**: Mocking strategy (gRPC, message bus, database)
- **Data transformations**: Edge cases and boundary tests

Also identify **provider-coupled behaviors** that cannot be faithfully validated with pure mocks.

Examples:
- Marten async LINQ/paging extensions (`ToPagedListAsync`, `ToListAsync` on Marten queryables)
- ORM/provider-specific translation or query execution behavior
- Infrastructure startup/schema paths that only execute with real dependencies

### 3. Generate Test Plan Document

Output includes:
- **Test structure** organized by category (Unit, Integration, Routing)
- **Per-feature test cases** with descriptions
- **Code coverage targets**: 90% line coverage per service
- **Routing test matrix**: API endpoints, message handlers, gateway routes
- **xUnit + Moq examples**: Setup patterns, mocking strategies
- **Verification checklist**: How to validate each test passes

Coverage ownership rule:
- Service test plans must measure service-owned code only.
- Shared assemblies (for example `BuildingBlocks`) must have a separate dedicated test project and separate coverage report.

### 4. Review & Refine

- Confirm test cases match spec requirements
- Identify mocking/integration points (gRPC, MassTransit, Wolverine message bus)
- Adjust coverage targets if service-specific constraints exist
- Flag any gaps or assumptions

When coverage is below target, explicitly classify gaps as:
- **Mock-solvable**: Can be covered with additional unit/endpoint tests
- **Provider-dependent**: Requires real database/broker provider execution

For provider-dependent gaps, include an executable plan for a **container-backed test slice** (Testcontainers/Docker) and document fallback behavior when Docker is unavailable.

If overall coverage is reduced by shared library assemblies, do not inflate service plans with unrelated tests. Instead:
- Exclude shared-library files from the service coverage scope
- Add or reference a dedicated test plan/project for that shared library

### 5. Implement Tests Following Plan

Use test case descriptions as specs for implementation:
- Write xUnit test classes with `Fact` and `Theory` attributes
- Mock dependencies with Moq
- Structure unit tests by handler/validator/business logic
- Create integration tests for message routing and API endpoints
- Verify line coverage ≥ 90% before merging

## Test Categories

### Unit Tests
- **Domain Models**: Value objects, entities, invariants
- **Handlers**: Command/event handler logic with mocked dependencies
- **Validators**: FluentValidation rules, input edge cases
- **Services**: Business logic calculations, transformations

**Framework**: xUnit with Moq for dependencies

### Integration Tests
- **Message Routing**: Wolverine/MassTransit event handler delivery
- **API Endpoints**: Request → handler → response contract
- **External Services**: gRPC stub behavior, database interaction
- **Event Sourcing**: Domain event capture and replay (if applicable)

**Framework**: xUnit with TestContainer for messaging, Moq for external calls

**Important**:
- Prefer a two-layer strategy:
	- Fast mock-only tests for most logic and contracts
	- Small real-provider slice for database/provider-coupled paths
- If real-provider tests are used, include local automation (single script/task) and an environment gate (e.g., fail fast when Docker is required but unavailable).

### Routing Tests
- **API Gateway (YARP)**: Request routing, authentication/authorization enforcement
- **Message Handler Routing**: Event → correct handler invocation
- **Service-to-Service**: gRPC endpoint discovery and contract validation

**Coverage Targets**: 90% line coverage per service, 70%+ branch coverage on routing logic

## Examples from eShop Microservices

### Basket Service (Wolverine + Checkout)
- **Unit**: `BasketService.AddItemAsync()`, discount application logic
- **Integration**: `BasketCheckoutEventHandler` event consumption
- **Routing**: YARP gateway → Basket API endpoint validation

### Ordering Service
- **Unit**: `Order` aggregate, `OrderItemDto` mapping from basket items
- **Integration**: `BasketCheckoutEventHandler` with `IMessageBus` (post-Wolverine)
- **Routing**: Message handler delivery, event sourcing

### Discount Service (gRPC)
- **Unit**: Discount calculation, promotion rules
- **Integration**: gRPC `GetDiscount()` contract validation
- **Routing**: Basket → Discount gRPC call path

## Reference Documents

- [Test Plan Template](./references/test-plan-template.md) — Markdown structure for test plans
- [xUnit + Moq Patterns](./references/xunit-moq-patterns.md) — Code snippets and setup
- [Coverage Configuration](./references/coverage-config.md) — Coverlet coverage targets
- [Routing Test Matrix](./references/routing-test-matrix.md) — API/messaging/gateway examples

## Next Steps

1. Ask for the spec (file path or content)
2. Generate a test plan document saved to `docs/test-plans/[YYYYMMDD]-[feature-name].md`
3. Review the plan with the team
4. Implement tests using the plan as specification
5. Use `/coder` skill to write tests following the plan

## Coverage Gap Checklist (Required in New Plans)

- [ ] List excluded files and justification (bootstrap/seed/generated only)
- [ ] Identify non-service assemblies in coverage output and define ownership (`service` vs `shared library`)
- [ ] If shared library code appears in service coverage, require separate test project/report for that library
- [ ] Separate mock-only vs provider-dependent coverage candidates
- [ ] Include malformed input and route edge cases (bad query params, malformed route values, missing segments)
- [ ] Include default-value behavior tests (pagination defaults, optional parameters)
- [ ] Define Docker/Testcontainers local run command if provider slice exists
- [ ] Define expected behavior when Docker is unavailable (skip/fail-fast policy)
