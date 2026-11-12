---
name: code-review
description: The feature-end gate of the delivery workflow — in a fresh context, re-runs verify.ps1, proves every acceptance criterion, checks that no test was weakened, and reviews the diff against the Tale Code philosophy and ClaudeCodingGuide rules. Read-only; returns a PASS / FAIL verdict with findings at file:line and a semver classification. Use when every step of a feature is done and `code-review:` is pending, or when the user asks for a review of a diff, a branch, or a pull request.
---

# Code Review

Decide whether a change may be accepted. Assume it must not ship and look for the reason; report
`PASS` only when none survives. This skill is the Code review phase of the
[delivery workflow](../../../docs/architecture/delivery-workflow.md#code-review).

## Fresh context

- **ALWAYS run in a context that did not plan or implement the change.** Claude Code: a subagent
  whose prompt names this file, the brief or diff, and the base commit. Copilot: a new session.
  For the strongest check, use the other tool or a different model family than the implementer.
- The invoking session relays the report unchanged and applies fixes only afterwards.

## Critical rules

- **Read-only.** No edits to source, tests, docs, or plan files. No commits. No status changes;
  the driver sets `code-review:` from the verdict.
- **Trust nothing reported.** Re-run every command. A result this context did not produce is
  unverified, whoever claimed it. Do not read the implementer's report.
- **Commands outrank judgment.** The verdict follows the gates and the acceptance criteria. A
  finding counts only with `file:line`, the rule it breaks, and a concrete failure it causes.
- **Falsify your own findings.** Before reporting, re-read each finding against the diff alone;
  drop every finding you cannot reproduce from the cited lines.
- **Tale Code first.** Readability is a primary criterion, not a nice-to-have.
- **No user questions.** The report is the only output.

## Inputs

- A feature: the brief path and the baseline commit.
- A change without a plan: the pass/fail checks from the request and the base commit.

## Process

### 1. Read the contract and the diff

Read the brief and every file under its `steps/`: `Affected components`, `Changes`,
`Acceptance criteria`, `Deviations`. Read `git diff <base>` and `git status` for untracked files.
Group the changed files by module (`SolTechnology.Core.<Module>`,
`sample-tale-code-apps/<App>/`, `tests/`, `docs/`).

### 2. Run the gates

Run `./.github/verify.ps1`. Record each gate's result. A failing gate is `Blocking`.

### 3. Prove the acceptance criteria

For each criterion of the brief and of every step, run its command and record the output line
that proves it, or the `file:line` that satisfies it. A criterion that cannot be run is
`Not verified`, never `PASS`.

### 4. Check the tests

In the diff of test files, each of these is `Blocking` unless a step's `Changes` planned it:

- a deleted test or assertion;
- a test disabled with `[Ignore]`, `[Explicit]`, or `Assert.Pass` / `Assert.Inconclusive`;
- an expected value changed to match new output without a planned behaviour change;
- production code that special-cases a test input.

Also:

- Each behavioural change has a test in `tests/SolTechnology.Core.<Module>.Tests/`; a new
  `Result.Failure` path has a negative test. A missing one is `Major`; the fix goes to
  [test-writing](../test-writing/SKILL.md).
- A new mock of `IMediator`, `HttpClient`, `DbContext`, or a repository in a unit test is `Major`
  (Coding Guide §8).

### 5. Check the scope

Every changed file is listed under `Affected components` or covered by a recorded deviation, and
every listed component changed. An unlisted production change is `Major`. An action from
`CLAUDE.md` §2 that the brief does not record as approved is `Blocking`. The steps integrate: no
step's change is undone or contradicted by a later one.

### 6. Coding guide compliance

Choose the sections of [docs/ClaudeCodingGuide.md](../../../docs/ClaudeCodingGuide.md) from the
changed paths, not from a general read:

- §3 CQRS — handlers return `Result<T>`, registered in `ModuleInstaller`, single responsibility.
- §7 API — controllers send through Core `IMediator`; direct handler injection bypasses pipeline
  behaviors and is a finding.
- §10 Naming — public symbols follow acronym capitalization and compatibility constraints from
  [`naming-and-public-api.md`](../../../docs/architecture/naming-and-public-api.md).
- §11 Logging — `logger.Log*` template, level, scope, no PII.
- Class-size budget — flag files exceeding the limit.
- §15 Anti-patterns — a diff that drags an anti-pattern from the surrounding file, or introduces
  one, is a `Major` finding; the fix goes to [refactor](../refactor/SKILL.md).
- Each touched module's conventions — its doc under [docs/](../../../docs/).

### 7. Public API and semver

- Identify added / removed / changed public/protected symbols in `src/SolTechnology.Core.*`.
- Classify semver impact (MAJOR / MINOR / PATCH).
- MAJOR without explicit user confirmation, a dated feature record that explains the breaking
  rationale and migration, and a `BREAKING CHANGE:` footer is `Blocking` (see
  [commit-message](../commit-message/SKILL.md)).

### 8. DI, build hygiene, and premortem

- Every new injectable type is registered in its module's `ModuleInstaller.cs`.
- Package versions match
  [`package-management/references/canonical-versions.md`](../package-management/references/canonical-versions.md);
  do not hard-code a remembered version in the review.
- A diff that masks a `NU1605` / `NU190x` warning is `Blocking`; the fix goes to
  [dependency-audit](../dependency-audit/SKILL.md).
- A `CLAUDE.md §4` premortem trigger in the diff with `premortem:` neither `go`,
  `go-with-mitigations`, nor a user-authorized `skipped` is `Blocking`.

### 9. Documentation sync

- A public API change updates the module's file under [docs/](../../../docs/); the module is
  listed in [README.md](../../../README.md).
- The brief records every material deviation visible in the diff.
- Architecture pages are updated by the retrospective, after this review; report a page that
  will need it under `Required before merge`.

## Report

```markdown
# Code review: <feature or change>

**Verdict:** PASS | FAIL

## Gates
| Gate | Result |
|---|---|

## Acceptance criteria
| Criterion | Command or file:line | Result |
|---|---|---|

## Findings
| # | Severity | File:Line | Rule | Finding | Failure it causes | Suggested fix |
|---|---|---|---|---|---|---|

## Semver
MAJOR | MINOR | PATCH — <evidence>

## Required before merge
- <item>

## Not verified
- <what no command covered, and why>
```

Severities: `Blocking`, `Major`, `Minor`, `Nit`. `FAIL` when a gate fails, a criterion fails, or
a `Blocking` or `Major` finding stands. `Minor` and `Nit` findings are reported and do not fail
the verdict.

## Constraints

- Do not fix what you find; report it.
- Do not soften a finding because the gates pass, and do not fail a change on taste.
- Do not report a finding without `file:line`.
- A lesson learnt is proposed to the user per `CLAUDE.md §9`, never written into an instruction
  file unasked.
- Write the report in English.
