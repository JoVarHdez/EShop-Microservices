---
name: generate-test-spec
description: Generate a formal testing specification from a service/feature spec, defining test scope, quality gates, coverage ownership, provider-coupled strategy, and execution constraints before creating the test plan.
argument-hint: Provide the source spec path (docs/specs/*.md) or describe the service/feature to test.
---

# Generate Test Specification

This skill creates a **testing-focused specification** that defines what must be validated and why, before writing a test plan or tests.

## Purpose

Use this when you need a clear test contract for a service/feature, including quality targets, environment constraints, and ownership boundaries for coverage.

This repository is a learning project, so the testing spec should also teach: include rationale, trade-offs, and what each decision helps the team learn.

## When to Use

- User asks for a "test spec", "testing specification", or "quality spec"
- You have a feature spec and want a dedicated testing contract before planning tests
- Coverage ownership or provider-coupled behavior is ambiguous
- You want consistent inputs for `/generate-test-plan`

## Procedure

### 1. Source Input and Scope

Collect one of:
- A formal spec file in `docs/specs/`
- Pasted spec content
- A feature/service description (mark output partial)

If a spec file path is provided but missing, stop and respond:
"The spec file at [path] could not be located. Please paste the spec content directly or verify the file path and try again."

### 2. Extract Test-Relevant Surface

Identify and list:
- Business rules and acceptance criteria to validate
- API/message/gRPC contracts to verify
- Validation rules and negative paths
- Data access/query paths
- Routing surfaces (HTTP, gateway, message handlers)

Classify each area into:
- **Mock-solvable**
- **Provider-dependent** (real provider needed)

### 3. Define Test Strategy Decisions

For the test spec, explicitly decide:
- Unit vs integration vs routing boundaries
- Provider-dependent strategy (minimal real-provider slice using Docker/Testcontainers)
- Docker availability policy (`skip` or `fail-fast`)
- Coverage ownership (`service-owned` vs `shared library`)

Automation scope rule:
- Do not assume or require new CI workflow yaml files unless the user explicitly requests CI/workflow work.
- Default execution guidance should use local `dotnet test` commands.
- Script-based commands should be documented only for docker-backed provider-coupled test slices.

Coverage ownership rule:
- Service test scope must exclude shared libraries (for example `BuildingBlocks`)
- Shared libraries require their own test project and coverage report

### 4. Produce Testing Spec Document

Use `references/test-spec-template.md` and generate:
- `docs/specs/[YYYYMMDD]-[feature-name]-testing-spec.md`

If no formal source spec exists, produce a partial test spec and mark missing sections as:
`[INCOMPLETE — requires formal spec to finalize]`

### 5. Validate Spec Completeness

Before finalizing, verify:
- [ ] Test scope is explicit and bounded
- [ ] Critical paths include happy + failure + edge cases
- [ ] Provider-dependent areas are listed with real-provider approach
- [ ] Coverage goals and ownership boundaries are explicit
- [ ] Docker/external setup requirements are documented
- [ ] Automation scope follows policy (no implicit workflow yaml requirements, scripts only for docker-backed paths)
- [ ] Gaps and assumptions are listed with next actions
- [ ] Open Questions are listed in multi-choice format with implications for each option

### 6. Add Open Questions (Required)

Every testing spec must include an `Open Questions` section for unresolved decisions that need human input.

Rules:
- Use business/testing decisions, not implementation details (for example avoid "which ORM API should we call")
- Use multi-choice format with at least 2 options (highlight the recommended option if applicable)
- For each option, include why it matters and expected impact on quality, speed, or maintenance
- If no open questions remain, explicitly state: `None — all testing decisions confirmed as of [date]`

## Required Edge-Case Checklist

Include at minimum when applicable:
- Pagination defaults and nullable query parameters
- Empty collection semantics (for example category with no results)
- Malformed route/query combinations
- Not-found behavior for identifier-based operations
- Validation failure behavior and dispatch suppression

## Quality Standards

- Testing spec must be implementation-agnostic and testable
- Every quality gate must be measurable (pass/fail)
- Avoid mixing service coverage with shared-library coverage
- Keep provider-dependent tests minimal and intentional
- Include brief learning notes for major decisions (what was learned, what to watch next)

## Handoff

After the test spec is approved:
1. Run `/generate-test-plan` using this testing spec
2. Implement tests with `/coder`
3. Update `test/<Service>.Tests/README.md` using the repository test README standard sections:
	- `Current Status`
	- `Coverage Exclusions`
	- `Run Commands`
	- `Docker or External Setup`
	- `Remaining Gaps and Improvements`

README standard rule:
- Prefer `dotnet test` commands as baseline documentation.
- Include custom scripts only when they are docker-backed and required for provider-coupled tests.

## Related Skills

- `generate-spec` for business/feature specs
- `generate-test-plan` for executable test plan generation
- `coder` for test implementation
