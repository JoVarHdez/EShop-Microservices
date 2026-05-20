# Implementation Plan Template

## Frontmatter

```markdown
# Implementation Plan: [Feature Name]

> Spec: [`docs/specs/[YYYYMMDD]-[feature-name].md`](../specs/[YYYYMMDD]-[feature-name].md)

**TL;DR** — [One sentence per phase, e.g. "delete dead code first, then update the data layer, then strip interfaces, then rewrite endpoints, then wire the host."] [Note any phases that can run in parallel.] [Note any work that was already completed in a prior modernization.]
```

---

## Phase Structure Rules

Order phases by this dependency chain — never reverse it:

| Phase | Role | Can start when |
|-------|------|----------------|
| 1 | Delete dead code & remove packages | Immediately — zero dependencies |
| 2 | Infrastructure / data layer changes | Phase 1 deletions complete |
| 3 | Domain / business logic changes | Parallel with Phase 2 if files are independent |
| 4 | Routing / endpoint layer | Phases 2 and 3 complete |
| 5 | Host wiring (`Program.cs`) | Phase 1 packages removed + Phase 4 complete |

Adjust the number of phases to the feature. Not every plan needs 5. The rule is: **lowest-risk first, highest-coupling last.**

Mark each phase header with its dependency: `*(depends on Phase X.Y)*` or `*(parallel with Phase N)*`.

Mark steps within a phase that are independent: `All steps in this phase are **independent and can run in parallel**.`

---

## Phase Template

```markdown
## Phase N — [Phase Name] *([dependency note])*

[One sentence describing what this phase accomplishes and why it is safe to do in this order.]

### N.X — [Step Name]

[One sentence explaining what this step changes and why — the learning note.]

**File**: `src/Path/To/File.cs`

- Remove `using Some.Old.Namespace;`
- Strip `: IOldInterface<T>` from `MyClass` declaration
- Change return type from `Task<Foo>` to `Task<Foo?>`
- Replace:
  ```csharp
  // old code
  return value is null ? throw new NotFoundException() : value;
  ```
  With:
  ```csharp
  // new code
  return value;
  ```
- **Unchanged**: constructor body, `StoreAsync` method, validator class
```

**Step writing rules:**
- Name every symbol being removed (exact `using`, interface name, method name)
- Show code snippets when the change is non-trivial (new patterns, signature changes, paradigm shifts)
- Always include an **Unchanged** callout for business logic that must be preserved
- For new files, show the full content or skeleton

---

## Relevant Files Table Template

```markdown
## Relevant Files

| Action | File |
|--------|------|
| DELETE | `src/Path/To/FileA.cs` |
| DELETE | `src/Path/To/FileB.cs` |
| MODIFY | `src/Path/To/FileC.cs` |
| MODIFY | `src/Path/To/FileD.cs` |
| CREATE | `src/Path/To/FileE.cs` |
```

List every file touched by the plan. Actions:
- `DELETE` — file is removed entirely
- `MODIFY` — existing file is edited
- `CREATE` — new file is added

---

## Verification Template

```markdown
## Verification

1. **Build**: `dotnet build src/[solution-file].slnx` must succeed with 0 errors.
2. **Grep check**: search across `src/[ServicePath]/` for [comma-separated list of banned symbols] — must return no results.
3. **HTTP smoke tests** (run against `docker-compose up`):
   - `[VERB] /[route]` with [scenario] → `[expected status]` [and expected body/behavior]
   - `[VERB] /[route]` with [failure scenario] → `[expected error status]`
   - `GET /health` → `200 OK` with [expected health checks] passing
```

Smoke test rules:
- At least one **success path** test per endpoint
- At least one **failure path** test per endpoint (invalid input → 400, missing resource → 404)
- The health check endpoint is always included
- Each test must be binary: it either returns the stated status or it doesn't

---

## Full Skeleton

```markdown
# Implementation Plan: [Feature Name]

> Spec: [`docs/specs/[YYYYMMDD]-[feature-name].md`](../specs/[YYYYMMDD]-[feature-name].md)

**TL;DR** — [Summary sentence.]

---

## Phase 1 — Delete dead code & remove packages

All steps in this phase are **independent and can run in parallel**.

### 1.1 — Delete `[FileName.cs]`

[Learning note: why this file is deleted and what replaces it.]

- `src/Path/To/FileName.cs`

### 1.2 — Remove NuGet package references from `[Project.csproj]`

**`src/Path/To/Project.csproj`** — remove:
- `PackageName` — [one-line reason]

---

## Phase 2 — [Name] *(depends on Phase 1.X)*

[All steps touch different files and are **independent of each other**.] or [Steps must be done in order.]

### 2.1 — `Path/To/File.cs`

[Learning note.]

- Remove `using Old.Namespace;`
- Change `[symbol]` from `[old]` to `[new]`
- **Unchanged**: [list of preserved logic]

---

## Phase 3 — [Name] *(parallel with Phase 2)*

### 3.1 — `Path/To/File.cs`

...

---

## Phase 4 — [Name] *(depends on Phase 2 and Phase 3)*

### 4.1 — Create `Path/To/NewFile.cs` *(new file)*

[Learning note.]

```csharp
// Full content or representative skeleton
```

### 4.2 — `Path/To/ExistingEndpoint.cs`

- `public class Foo : ICarterModule` → `public static class Foo`
- `public void AddRoutes(...)` → `public static RouteGroupBuilder MapFooEndpoint(this RouteGroupBuilder group)`
- Replace `ISender sender` with `IMessageBus bus`
- Replace `sender.Send(cmd)` with `await bus.InvokeAsync<TResult>(cmd)`
- **Unchanged**: Mapster `.Adapt<>()` calls, `.WithName()`, `.WithSummary()`, `.WithDescription()`

---

## Phase 5 — Update `Program.cs` *(depends on Phase 1.X, Phase 4)*

### 5.1 — Remove

- `using OldPackage;`
- `builder.Services.AddOldThing(...);`
- `app.MapOldThing();`

Keep `[symbol]` — [reason it stays].

### 5.2 — Add [new registration]

After `[anchor line]`:

```csharp
// new registration code
```

---

## Relevant Files

| Action | File |
|--------|------|
| DELETE | `src/...` |
| MODIFY | `src/...` |
| CREATE | `src/...` |

---

## Verification

1. **Build**: `dotnet build src/solution.slnx` must succeed with 0 errors.
2. **Grep check**: search across `src/ServicePath/` for `Symbol1`, `Symbol2`, `Symbol3` — must return no results.
3. **HTTP smoke tests** (run against `docker-compose up`):
   - `GET /route/{existing}` → `200 OK`
   - `GET /route/{missing}` → `404 Not Found`
   - `POST /route` valid body → `201 Created`
   - `POST /route` invalid body → `400 Bad Request`
   - `GET /health` → `200 OK`
```
