---
name: refactor
description: Plan and execute a behaviour-preserving refactor inside `src/SolTechnology.Core.*` or a sample app. Use when renaming inside one module, splitting a class above the size budget, extracting a primary constructor, removing a `#region`, replacing a Newtonsoft usage with `System.Text.Json`, or paying down any `ClaudeCodingGuide §15` anti-pattern. Routes to `implementation-planning` when scope grows past a single module or touches a public symbol.
---

# Refactor

Behaviour-preserving cleanup local to one module. Encodes "small, safe, test-backed" — the
opposite of the multi-PR planning path. Where `implementation-planning` answers *"how do we
build this new thing?"*, this skill answers *"how do I fix a smell I can see right now without
a behaviour change?"*.

## When to use

- Rename a private / internal type inside one module.
- Split a class above the §9 size budget (≤ 100 lines target, ~150 hard cap).
- Extract a primary constructor over hand-written `private readonly` capture.
- Remove a `#region` (forbidden by §9.10).
- Replace `Newtonsoft.Json` with `System.Text.Json` in new code per §15.
- Collapse a `try/catch` + `JsonConvert.SerializeObject(ex.Message)` controller into the
  `ExceptionFilter` path.
- Fix any other [ClaudeCodingGuide §15](../../../docs/ClaudeCodingGuide.md) anti-pattern that
  the surrounding file already drags in.
- `code-review` flagged a §15 finding and the fix is local.

## When NOT to use

- The change adds, removes, or renames a **public/protected** symbol in
  `src/SolTechnology.Core.*` — that is `CLAUDE.md §1` forbidden without confirmation and an
  ADR. Route to [`implementation-planning`](../../agents/implementation-planning.agent.md).
- The refactor crosses two or more modules. Route to `implementation-planning`.
- The refactor changes behaviour — even subtly (different error code, different log level,
  different exception type). Route to `implementation-planning` with the behavioural change as
  the explicit decision to record in the ADR.
- The refactor introduces or removes a `PackageReference`. Use
  [`package-management`](../package-management/SKILL.md) (add / bump) or
  [`dependency-audit`](../dependency-audit/SKILL.md) (CVE) first.
- The "refactor" is actually a redesign (new abstraction, new pattern, replacing CQRS chain
  handlers with Story steps, etc.) — route to `implementation-planning`.

## Critical rules

- **Behaviour-preserving.** Every test that was green stays green. No behavioural drift hidden
  under a "refactor" label.
- **One smell per invocation.** Split a class **or** rename a type **or** extract a primary
  constructor — do not bundle.
- **Tests cover the surface first.** If the file being refactored has no tests covering the
  behaviour you are moving, write them via [`test-writing`](../test-writing/SKILL.md) **before**
  changing structure. Refactoring without a regression net is how silent breakage ships.
- **Public surface is off-limits.** This skill is for private / internal / file-scoped changes.
- **Tale Code lens.** Every refactor must make the file read more like prose, not less.
  Removing a `#region` only to introduce three new helper classes that obscure the flow is a
  net loss.

## Procedure

### 1. Identify the smell

Cite the file:line and the rule it violates. One-sentence statement:

```
src/SolTechnology.Core.HTTP/Resilience/RetryPolicy.cs:142 — class is 187 lines (§9 hard cap 150).
```

The cite is mandatory; without it the refactor is a vibe-cleanup.

### 2. Confirm the surface boundary

Run:

```bash
grep -rE 'public (class|record|interface|struct|enum) <TypeName>' src/SolTechnology.Core.*/ \
  | grep -v 'src/SolTechnology.Core.<Module>/'
```

If the type or any member you plan to touch is referenced outside its module, STOP and route
to `implementation-planning`. Public-surface refactors are ADR-grade.

### 3. Verify test coverage of the affected surface

For each behaviour the refactor moves, find the test that pins it:

```bash
grep -rn '<TypeName>\|<MethodName>' tests/SolTechnology.Core.<Module>.Tests/
```

If coverage is missing, invoke [`test-writing`](../test-writing/SKILL.md) **first**, land the
tests in a separate commit, then refactor. Tests in the same commit as the refactor are
unfalsifiable — the reviewer cannot tell whether the test pins old or new behaviour.

### 4. Execute the smallest possible change

| Smell | Mechanical fix |
|---|---|
| Class > 150 lines | Extract one collaborator with a name that describes its responsibility (`CityMapper`, `StreetTrafficUpdater`); inject it via primary constructor. Never extract a `*Helper` / `*Manager` / `*Util` — see §9.9. |
| Hand-written `private readonly` ctor capture | Replace with C# 12 primary constructor. `ILogger<TSelf>` stays in the parameter list. |
| `#region ... #endregion` | Delete the markers. If the class is still hard to scan, split into partial files (one method per file for HTTP clients, per §9.10) or extract a collaborator. |
| `try/catch` in controller / handler | Delete the `try/catch`; let `ExceptionFilter` handle it. Verify the controller test asserts the same response status the filter produces. |
| `Newtonsoft.Json` in new code | Replace with `System.Text.Json`. Newtonsoft stays **only** in MessageBus / Hangfire-integrated paths (see `package-management` constraints). |
| Multi-line essay comment restating *what* | Delete it. Keep one-line *why* comments only (§9.11). |
| `+`-concatenated log string | Replace with structured `[Placeholder]` template per §11. |
| Entity returned past DataLayer boundary | Add a `*Mapper.ToDomain(...)` call; return the domain type. The mapper lives in DataLayer. |

### 5. Run the verification triple

After **every** mechanical edit:

```bash
dotnet build SolTechnology.Core.slnx
dotnet test tests/SolTechnology.Core.<Module>.Tests/
```

Plus `get_errors` per `CLAUDE.md §2` after every file edit.

Any test that flips red → STOP. The refactor was not behaviour-preserving. Revert the edit;
re-think; do not "fix" the test to match the new behaviour.

### 6. Update the Coding Guide if a new anti-pattern was learnt

If the smell you fixed was **not** in [§15](../../../docs/ClaudeCodingGuide.md), add a row to
the §15 table in the same PR with the file you fixed as the "Where seen" cell. This is the
self-improvement loop from `CLAUDE.md §8` — silent retention is forbidden.

### 7. Commit per `commit-message`

```
refactor(<scope>): extract CityRouteCalculator from CalculateBestPathHandler

CalculateBestPathHandler was 187 lines, above the §9 hard cap of 150. Pulled the
route-scoring loop into CityRouteCalculator (78 lines) injected via primary ctor.
No behaviour change; all 14 tests in CalculateBestPathHandlerTests still green.

Semver: PATCH
```

`refactor` type is mandatory. **Never** use `feat` or `fix` for a refactor — semver footer
must be `PATCH` because consumers see no contract change.

## Pre-yield checklist

- [ ] The smell was cited file:line with the §15 / §9 rule it violates.
- [ ] The touched type / member is **not** public outside its module (verified via grep).
- [ ] Tests covering the moved behaviour existed **before** the refactor — or were added in a
      prior commit via `test-writing`.
- [ ] `dotnet build SolTechnology.Core.slnx` green.
- [ ] `dotnet test tests/SolTechnology.Core.<Module>.Tests/` green; same test count as before
      (or higher only when the prior commit added regression tests).
- [ ] `get_errors` clean on every edited file.
- [ ] Class-size budget respected on every file produced by the refactor.
- [ ] No new `Helper` / `Manager` / `Util` suffix introduced.
- [ ] Commit type is `refactor`; semver footer is `PATCH`.
- [ ] If the smell was novel, §15 was updated in the same PR.

## Constraints

- DO NOT change behaviour. A refactor that introduces a different error code, log level,
  exception type, or branching outcome is a feature change — re-route to
  `implementation-planning`.
- DO NOT touch public / protected symbols in `src/SolTechnology.Core.*`. That is
  `CLAUDE.md §1` forbidden.
- DO NOT cross module boundaries inside this skill. One module, one PR.
- DO NOT add or remove a `PackageReference`. Use `package-management` / `dependency-audit`.
- DO NOT write new tests in the same commit as the refactor — they belong in a prior commit so
  the reviewer can verify they pin the pre-refactor behaviour.
- DO NOT bundle multiple smells. One smell per invocation; one mechanical fix per commit.
- DO NOT introduce a `Helper` / `Manager` / `Util` suffix when extracting a collaborator.
  Name by responsibility (§9.9).
- DO NOT silence a failing test to make the refactor green. Revert the edit instead.
- DO NOT use `feat` / `fix` commit types for a refactor. `refactor` + `Semver: PATCH` is the
  only correct shape.
- DO NOT improvise a freehand refactor flow when this skill is unavailable. STOP and tell the
  user `refactor` is required (CLAUDE.md §2). Freehand refactors are how silent behavioural
  drift ships under an innocent-looking commit.

