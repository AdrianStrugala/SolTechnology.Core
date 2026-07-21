---
name: code-review
description: Review a SolTechnology.Core change against the Tale Code philosophy, ClaudeCodingGuide rules, and module conventions.
---

# Code Review

Evidence-based review skill for changes inside `src/SolTechnology.Core.*` and the sample apps.

## Documentation references

- [docs/ClaudeCodingGuide.md](../../../docs/ClaudeCodingGuide.md) — binding coding rules
- [docs/architecture/](../../../docs/architecture/) — current architecture and rationale
- [docs/features/](../../../docs/features/) — historical delivery context

## Critical rules

- **Cite file:line** for every finding. No vibe reviews.
- **Reference the rule.** Each finding links to the Coding Guide section, architecture page, or module doc
  that the change violates.
- **Report what, not why-it-feels-wrong.** Suggested fix must be concrete.
- **Tale Code first.** Readability is a primary criterion, not a nice-to-have.

## Process

### 1. Gather diff

Identify changed files. Group them by module
(`SolTechnology.Core.<Module>`, `sample-tale-code-apps/<App>/`, `tests/`, `docs/`).

### 2. Coding guide compliance

Walk through the relevant sections of
[docs/ClaudeCodingGuide.md](../../../docs/ClaudeCodingGuide.md) for each diff hunk:

- §3 CQRS — handlers return `Result<T>`, registered in `ModuleInstaller`, single responsibility.
- §10 Naming — public symbols follow acronym capitalization and compatibility constraints from
  [`naming-and-public-api.md`](../../../docs/architecture/naming-and-public-api.md).
- §11 Logging — `logger.Log*` template, level, scope, no PII.
- Class-size budget — flag files exceeding the limit.
- §20 Self-Improvement — if a lesson was learnt, the guide itself or `CLAUDE.md` must be updated
  in the same PR.
- §15 Anti-patterns — if the diff drags an anti-pattern from the surrounding file (or
  introduces one), hand off the fix to [refactor](../refactor/SKILL.md). Do not approve a PR
  that propagates a §15 anti-pattern.

### 3. Module-specific checks

Verify the change against each touched module's conventions — its doc under
[docs/](../../../docs/) and the relevant [ClaudeCodingGuide](../../../docs/ClaudeCodingGuide.md)
sections. Note any module-specific convention the diff violates.

### 4. Public API and semver

- Identify added / removed / changed public/protected symbols in `src/SolTechnology.Core.*`.
- Classify semver impact (MAJOR / MINOR / PATCH).
- If MAJOR: require explicit user confirmation, a dated feature record that explains the breaking
  rationale and migration, and a `BREAKING CHANGE:` footer (see
  [commit-message](../commit-message/SKILL.md)).

### 5. Tests

- Each behavioural change has a test in `tests/SolTechnology.Core.<Module>.Tests/`.
- No reduction in test count without a stated reason.
- New `Result.Failure` paths have a negative test.
- Missing or freehand tests → hand off to [test-writing](../test-writing/SKILL.md). Do not
  invent ad-hoc layouts here.

### 6. DI and build hygiene

- Every new injectable type is registered in its module's `ModuleInstaller.cs`.
- No new warnings under `TreatWarningsAsErrors=true`
  ([src/Directory.Build.props](../../../src/Directory.Build.props)).
- `Microsoft.Extensions.*` references stay aligned at version `10.0.1`.
- Any `NU1605` / `NU190x` warning surfaced by the diff → hand off to
  [dependency-audit](../dependency-audit/SKILL.md). Do not approve a PR that masks the warning.

### 7. Documentation sync

- Each touched module has a docs file under [docs/](../../../docs/) — verify it is updated when the
  public API changes.
- Each architecture-affecting change updates the owning page under
  [docs/architecture/](../../../docs/architecture/) to current behavior and rationale.
- The active dated feature record captures delivery outcome and material deviations.
- The module is listed in [README.md](../../../README.md).
- Run [documentation-cleanup](../documentation-cleanup/SKILL.md) if doc changes are non-trivial.

### 8. Premortem hand-off

Require a [premortem](../premortem/SKILL.md) before merge exactly for `CLAUDE.md §4` triggers.
Check `CLAUDE.md §2` confirmation gates separately. Recommend premortem for other changes only when
their risk warrants it.

## Output format

### Code Review — `<change title>`

#### Files Reviewed
<placeholder>List grouped by module.</placeholder>

#### Findings

| # | Severity | File:Line | Rule (Guide §/Architecture/Review) | Finding | Suggested Fix |
|---|---|---|---|---|---|
| 1 | Blocking / Major / Minor / Nit | <placeholder> | <placeholder> | <placeholder> | <placeholder> |

#### Semver Classification
**Impact**: MAJOR / MINOR / PATCH — <placeholder>evidence</placeholder>

#### Required Before Merge
- <placeholder></placeholder>

#### Premortem Required
<placeholder>Yes / No — reason.</placeholder>

#### Summary
**Blocking findings**: <placeholder>count</placeholder>
**Recommendation**: Approve / Approve with comments / Request changes

