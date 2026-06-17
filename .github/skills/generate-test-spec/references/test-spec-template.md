# Test Specification: [Feature / Service Name]

**Source Spec**: [path or title]
**Service**: [service-name]
**Last Updated**: [YYYY-MM-DD]

## 1. Testing Objective

Describe what quality risks this specification addresses and why this test scope exists.

## 2. Scope

### In Scope
- [behavior/contract 1]
- [behavior/contract 2]

### Out of Scope
- [explicitly excluded area 1]
- [explicitly excluded area 2]

## 3. Test Surfaces

### 3.1 Domain and Validation
- [rules and invariants to validate]

### 3.2 API / Messaging / gRPC Contracts
- [endpoint/handler/contract behaviors]

### 3.3 Routing and Dispatch
- [route ambiguity, handler routing, gateway mapping]

### 3.4 Data Access and Provider-Coupled Paths
- [query/provider-specific behavior]

## 4. Strategy and Environment

### 4.1 Test Layering
- Unit: [scope]
- Integration: [scope]
- Routing: [scope]

### 4.2 Mock-Solvable vs Provider-Dependent Classification
- Mock-solvable:
  - [item]
- Provider-dependent:
  - [item]

### 4.3 Provider-Dependent Execution Policy
- Real-provider mechanism: [Docker/Testcontainers/etc]
- Docker policy: [skip|fail-fast]
- Local command baseline: [command]

## 5. Quality Gates

- Line coverage target: [value]
- Branch coverage target: [value]
- Reliability gate: [all required tests pass]
- Contract gate: [all contract assertions pass]

### Coverage Ownership
- Service-owned code included: [list]
- Shared library code excluded from service KPI: [list]
- Shared library test project/report: [path]

## 6. Required Edge Cases

- [ ] Pagination defaults and nullable query behavior
- [ ] Empty result semantics for filters/categories
- [ ] Malformed route/query combinations
- [ ] Not-found branches
- [ ] Validation failure behavior and no-dispatch checks

## 7. Gaps and Assumptions

- Gap: [missing information]
  - Impact: [why it matters]
  - Resolution: [action/owner]

## 8. Open Questions

1. Question: [decision needed]
  - Why this matters: [impact on quality/speed/maintenance]
  - Option A: [choice]
    - Implication: [impact]
  - Option B: [choice]
    - Implication: [impact]
  - Option ... (where necessary)

If none:

`None — all testing decisions confirmed as of [YYYY-MM-DD].`

## 9. Learning Notes

- Decision: [what was chosen]
  - Why: [rationale]
  - What we learned: [reusable insight]
  - Watch next: [risk or follow-up signal]

## 10. Handoff to Test Plan

- Planned test project: [path]
- README update required: [yes/no]
- Next command/skill: [`/generate-test-plan` with this file]
