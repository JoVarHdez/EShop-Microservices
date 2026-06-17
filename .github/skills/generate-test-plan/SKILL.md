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

If the referenced spec file cannot be found or read, stop and respond: "The spec file at [path] could not be located. Please paste the spec content directly or verify the file path and try again." Do not attempt to infer or fabricate spec content.

If the user provides only a feature description rather than a formal spec, generate a partial test plan and explicitly mark each section that could not be populated as: "[INCOMPLETE — requires formal spec to finalize]". List the specific information needed to complete each incomplete section.

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
- **Test README update guidance** aligned with repository test-folder README standard

README standard rule:
- When test plans include creating/updating a service test project, include a README update section using the repository standard structure.
- Use the template in `references/test-readme-template.md`. If a referenced file (e.g., `references/test-readme-template.md`) is not accessible, note it in the ## Gaps and Assumptions section and inline a minimal placeholder structure derived from the section description rather than omitting the output entirely.

Apply coverage rules as defined in [## Coverage Rules](#coverage-rules).

### 4. Review & Refine

- Confirm test cases match spec requirements
- Identify mocking/integration points (gRPC, MassTransit, Wolverine message bus)
- Adjust the 90% line coverage target downward only when a documented technical constraint exists, such as: auto-generated migration files, infrastructure bootstrap entry points, or provider-coupled code paths that require real dependencies. Document the constraint and the revised target explicitly in the plan.
- At the end of the test plan document, add a dedicated ## Gaps and Assumptions section. For each gap, state: (1) the spec area not covered, (2) why it cannot be tested as specified, and (3) the recommended resolution.

Apply coverage rules as defined in [## Coverage Rules](#coverage-rules).

See [## Automation Rules](#automation-rules) for script and CI constraints.

### 5. Implement Tests Following Plan

Use test case descriptions as specs for implementation:
- Write xUnit test classes with `Fact` and `Theory` attributes
- Mock non-messaging dependencies with Moq; use TestContainers for provider-coupled messaging paths
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

If the spec or project context indicates a test framework other than xUnit or Moq is already in use, note the discrepancy in the ## Gaps and Assumptions section and generate examples using the existing framework. Do not introduce a second framework into an existing project.

### Integration Tests
- **Message Routing**: Wolverine/MassTransit event handler delivery
- **API Endpoints**: Request → handler → response contract
- **External Services**: gRPC stub behavior, database interaction
- **Event Sourcing**: Domain event capture and replay (if applicable)

**Framework**: xUnit with TestContainer for messaging, Moq for external calls

**Important**:
- Prefer a two-layer strategy:
	- Fast mock-only tests for most logic and contracts
	- A single xUnit test class per service containing only the minimum tests required to exercise provider-coupled code paths (e.g., Marten async LINQ extensions, schema initialization). All other logic must be covered by mock-only tests.
- If real-provider tests are used, include local automation (single script/task) and an environment gate (e.g., fail fast when Docker is required but unavailable).
- See [## Automation Rules](#automation-rules) for script and CI constraints.

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

## Automation Rules

1. Do not assume or require new CI workflow yaml files as part of the generated plan unless the user explicitly asks for CI/workflow work.
2. Keep the default execution guidance based on local `dotnet test` commands.
3. Custom scripts are permitted only when they are docker-backed for provider-coupled tests (for example, Testcontainers or required local infrastructure bootstrapping). Do not add or recommend non-docker helper scripts.

## Coverage Rules

**Ownership**:
- Service test plans must measure service-owned code only.
- Shared assemblies (for example `BuildingBlocks`) must have a separate dedicated test project and separate coverage report.
- Exclude shared-library files from the service coverage scope; add or reference a dedicated test plan/project for that shared library.

**Gap classification**:
When coverage is below target, explicitly classify gaps as:
- **Mock-solvable**: Can be covered with additional unit/endpoint tests.
- **Provider-dependent**: Requires real database/broker provider execution.

For provider-dependent gaps, include an executable plan for a **container-backed test slice** (Testcontainers/Docker) and document fallback behavior when Docker is unavailable.

**Excluded files**:
List excluded files and justification (bootstrap/seed/generated only). Identify non-service assemblies in coverage output and define ownership (`service` vs `shared library`).

## Reference Documents

- [Test Plan Template](./references/test-plan-template.md) — Markdown structure for test plans
- [Test README Template](./references/test-readme-template.md) — Standardized structure for service test-folder READMEs
- [xUnit + Moq Patterns](./references/xunit-moq-patterns.md) — Code snippets and setup
- [Coverage Configuration](./references/coverage-config.md) — Coverlet coverage targets
- [Routing Test Matrix](./references/routing-test-matrix.md) — API/messaging/gateway examples

## Next Steps

1. Ask for the spec (file path or content)
2. Generate a test plan document saved to `docs/test-plans/[YYYYMMDD]-[feature-name].md`, where `[feature-name]` is derived from the spec filename (without date prefix and extension) if a file was provided, or from the first H1 heading in the spec content if pasted directly. Use lowercase-kebab-case.
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
- [ ] Define expected behavior when Docker is unavailable (skip/fail-fast policy). When not specified, default to: annotate provider-coupled tests with `[Trait("Category", "RequiresDocker")]` and include a test fixture that calls `Assert.Skip` (xUnit v3) or `throw new SkipException` (xUnit v2) when the Docker socket is unreachable. Document this default in the plan and note that it can be changed to fail-fast by removing the skip guard.
