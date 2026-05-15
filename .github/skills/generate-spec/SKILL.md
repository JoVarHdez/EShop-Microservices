---
name: generate-spec
description: This skill should be used when the user wants to "create a spec", "generate a specification", "write a formal spec", or needs to document business requirements for a new feature. Generates a structured specification document following the 6-section template.
---

# Generate Specification

This skill generates formal specification documents that define the "What" and "Why" of a feature before any implementation planning or coding begins.

## Purpose

Create business-focused specification documents that serve as contracts between stakeholders and engineering, ensuring all parties agree on requirements before development starts.

## When to Use

- User asks to "create a spec", "write a specification", or "document requirements"
- Before starting any new feature implementation
- When requirements are vague and need to be clarified and formalized
- When planning a feature that will be built across multiple sessions

## Process

### Step 1: Clarify the Feature

Before generating the spec, ask targeted clarifying questions:

Ask one question at a time so we can develop a thorough, step-by-step spec for the idea. Each question should build on the previous answers, and our the end goal is to have a detailed specification that can be handed off to a developer. Let's do this iteratively and dig into every relevant detail. Remember, only one question at a time.

At minimum, you should ask questions that clarify:
- **Who** will use this feature?
- **What problem** does it solve?
- **What data** or entities are involved?
- **What constraints** must be enforced?
- **What is explicitly out of scope?**

Do NOT skip this step. Assumptions lead to incomplete specs.

### Step 2: Generate the Spec

Using the template in `references/spec-template.md`, create a specification document with all six required sections:

1. Feature Summary
2. Data Model / Entities
3. Business Rules & Constraints
4. Acceptance Criteria
5. Out of Scope
6. Open Questions

### Step 3: Validate Completeness

Before presenting the spec, check:
- [ ] Feature summary is one paragraph, clear and concise
- [ ] All entities list their fields (no implementation detail)
- [ ] Business rules are non-negotiable requirements
- [ ] Acceptance criteria are testable (can pass or fail)
- [ ] "Out of Scope" explicitly states what is NOT being built
- [ ] "Open Questions" lists decisions only a human can make

### Step 4: Output Location

Save the completed spec at @docs/specs/[YYYYMMDD]-[feature-name].md and provide a link to the user. Replace the date and feature name with appropriate values.

Use kebab-case for the filename (e.g., `employee-project-assignment.md`)

## Quality Standards

**Clear over comprehensive.** A spec should be scannable in 3 minutes. If it takes longer, it's too detailed.

**Testable criteria.** Every acceptance criterion must be binary: it either works or it doesn't. Avoid vague phrases like "user-friendly" or "performant."

**Explicit scope boundaries.** The "Out of Scope" section is as important as what IS in scope. It prevents feature creep.

**Human decisions only.** Open Questions should never be technical ("What database should we use?"). They should be business decisions ("Should admins be able to delete other users' projects?").

## Anti-Patterns to Avoid

❌ **Skipping clarification questions** → Results in a spec full of AI assumptions
❌ **Including technical implementation details** → That belongs in the Implementation Plan, not the Spec
❌ **Vague acceptance criteria** → "The system should be fast" is not testable
❌ **Empty "Out of Scope" section** → Always define boundaries
❌ **Answering your own open questions** → Flag them for the human to decide

## Reference Materials

See `references/spec-template.md` for the exact template structure.
See `examples/20260512-example-employee-spec.md` for a complete example.

## Notes

- This skill focuses on WHAT to build, not HOW to build it
- Generated specs should be reviewed and approved by humans before moving to implementation planning
- The spec is a living document—it can be updated as requirements evolve