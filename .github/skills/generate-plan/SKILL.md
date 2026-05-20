---
name: generate-plan
description: 'Use when the user wants to "create an implementation plan", "generate a plan", "write a plan", "plan the implementation", or "how do I implement this spec". Reads an approved spec and the current codebase, then produces a phase-ordered, file-level implementation plan with code snippets and verification steps. Always requires an existing spec. Saves to docs/implementations/[YYYYMMDD]-[feature-name].md.'
argument-hint: 'Path to the spec file, e.g. docs/specs/20260520-my-feature.md'
---

# Generate Implementation Plan

This skill produces a concrete, phase-ordered implementation plan from an approved specification. It bridges the gap between "what to build" (the spec) and "how to build it" (actual file edits), with enough detail that each step can be executed without re-reading the spec.

This is a **learning-oriented** skill: every non-trivial change includes context explaining *why* the old approach is replaced and *what* the new approach achieves.

## When to Use

- User says "create an implementation plan", "plan the implementation", "write a plan", or "let's plan this"
- An approved spec exists in `docs/specs/`
- Before handing off to the `coder` skill for execution

Do NOT generate a plan without a spec. If no spec exists, invoke the `generate-spec` skill first.

## Process

### Step 1: Read the Spec

Load the target spec from `docs/specs/`. Extract:
- All **business rules** — these map directly to phases and steps
- All **acceptance criteria** — these feed into the Verification section
- The **out-of-scope** list — any file not in the spec is off-limits
- Any resolved **decisions** — these settle ambiguities before planning starts

### Step 2: Map the Current Codebase

Explore only the files listed in the spec. For each file, identify:
- What currently exists (types, interfaces, packages, registrations)
- Which of those are **deleted**, **modified**, or **kept unchanged**
- Which new files must be **created**

This mapping determines the exact content of each phase step.

### Step 3: Order the Phases

Use the dependency rules in `./references/plan-template.md` to order work:

1. **Phase 1 — Delete & remove packages** always comes first (zero risk, unblocks everything)
2. **Phase 2 — Infrastructure / data layer** depends on Phase 1 deletions
3. **Phase 3 — Domain/business logic** can run parallel with Phase 2 if files are independent
4. **Phase 4 — Routing / endpoint layer** depends on Phases 2 and 3
5. **Phase 5 — Host wiring (Program.cs)** always comes last (depends on Phase 1 packages + Phase 4 endpoints)

Adjust phase count to the feature — not every plan needs exactly 5 phases. The rule is: **lowest-risk changes first, highest-coupling changes last**.

### Step 4: Write the Plan

Using `./references/plan-template.md`, produce the full plan. For each step:

- State the **file path** (workspace-relative)
- List what to **REMOVE** (exact symbol names, using blocks, method calls)
- List what to **ADD** (exact new code with snippets for non-trivial changes)
- Call out what stays **UNCHANGED** — this prevents accidental over-editing
- Add a one-sentence **learning note** when the change introduces a new pattern

Code snippets are required when:
- The new registration/wiring is non-obvious (e.g., keyed services, Wolverine host setup)
- A method signature changes (e.g., nullable return type)
- A pattern switches paradigm (e.g., exception → discriminated union)

### Step 5: Build the Relevant Files Table

Summarize every touched file as `DELETE`, `MODIFY`, or `CREATE`. This gives the implementer a single-glance overview before starting.

### Step 6: Write the Verification Section

Pull directly from the spec's acceptance criteria. Every plan must have:

1. **Build check** — `dotnet build` (or equivalent) with 0 errors
2. **Grep check** — list the exact symbols/types that must NOT appear after the change
3. **Functional smoke tests** — at minimum one test per endpoint/feature, covering both success and failure paths

### Step 7: Output

Save the completed plan at `docs/implementations/[YYYYMMDD]-[feature-name].md` using the same date and kebab-case name as the spec.

Offer to also open it as an `untitled:plan-[camelCaseName].prompt.md` scratch file for refinement before saving.

## Quality Standards

**Phase boundaries are dependency boundaries.** If step B cannot start until step A is done, they are in different phases. If they can run concurrently, they are siblings in the same phase.

**File-level, not feature-level.** "Update the repository" is not a step. "Change `GetBasketAsync` return type in `Data/BasketRepository.cs` from `Task<ShoppingCart>` to `Task<ShoppingCart?>`" is a step.

**Snippets for non-obvious changes only.** Trivial deletions (remove a `using`, strip an interface suffix) do not need snippets. New wiring patterns, signature changes, and paradigm shifts do.

**Unchanged = explicit.** Always call out what is NOT changing in a file. This prevents the implementer from accidentally removing business logic while cleaning up boilerplate.

**Verification is binary.** Every smoke test must be a pass/fail HTTP call or build result — no "verify the behavior looks correct."

## Anti-Patterns to Avoid

❌ **No spec input** → Never generate a plan from a verbal description alone; insist on a spec
❌ **Feature-level steps** → "Rewrite the handler" is not actionable; name the exact changes
❌ **Missing unchanged callouts** → Causes accidental deletion of valid business logic
❌ **Verification without grep check** → Build success does not mean old symbols are gone
❌ **Parallel steps with hidden coupling** → Two steps in the same phase that share a file must be serialized

## Reference Materials

See `./references/plan-template.md` for the exact template structure and phase-ordering rules.
