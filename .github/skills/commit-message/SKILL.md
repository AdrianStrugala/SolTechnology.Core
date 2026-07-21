---
name: commit-message
description: Produce a Conventional Commits message for SolTechnology.Core, using module names as scopes and surfacing semver impact in the footer.
---

# Commit Message

Generate a commit message that matches the SolTechnology.Core convention.

## Format

```
<type>(<scope>): <imperative summary, <= 72 chars>

<optional body — what and why, wrapped at 100 chars>

<optional footers>
```

## Types

| Type | Meaning |
|---|---|
| `feat` | New user-visible capability |
| `fix` | Bug fix |
| `refactor` | Behaviour-preserving change |
| `perf` | Performance improvement |
| `test` | Tests only |
| `docs` | Docs only (architecture, feature records, READMEs) |
| `build` | Build / packaging / `Directory.Build.props` |
| `ci` | CI workflow under `.github/workflows/` |
| `chore` | Repo housekeeping with no other category |

## Scopes

Use the module name in lowercase, dropping `soltechnology.core.`:

`api`, `api-testing`, `apiclient`, `auid`, `auth`, `blob`, `cache`, `cqrs`, `faker`,
`flow`, `guards`, `http`, `jobs`, `journey`, `logging`, `messagebus`, `scheduler`,
`sql`, `story`.

Cross-module: `core` (multiple modules) or `repo` (root files only).
Sample apps: `dreamtravel`, `talecode`, `elsa`.
Meta: `docs`, `architecture`, `features`, `skills`, `build`, `ci`.

## Rules

- Subject in imperative mood ("add", not "added"/"adds").
- No trailing period in the subject.
- Body explains *what* and *why*, never *how* — the diff already shows *how*.
- **Semver footer** is mandatory for any change touching public symbols in
  `src/SolTechnology.Core.*`:
  - `Semver: PATCH` — internal only / docs / tests.
  - `Semver: MINOR` — additive public API.
  - `Semver: MAJOR` — breaking public API. **Must** also include a
    `BREAKING CHANGE: <one-line description>` footer and reference the dated feature record that
    explains the rationale and migration.
- Reference the [premortem](../premortem/SKILL.md) output in the body when the change required one.
- No issue tracker IDs unless the user supplies them.

## Process

1. Read the staged diff (`git diff --cached`).
2. Pick the **narrowest** type that fits. If two types apply, split the commit.
3. Pick the scope from the touched module(s). If more than two modules, use `core`.
4. Write the subject (imperative, ≤72 chars).
5. If non-trivial, add a body. Lead with the user-visible effect.
6. Add the semver footer when applicable. Add `BREAKING CHANGE:` when MAJOR.
7. Re-read against the rules above before printing.

## Output format

```
<type>(<scope>): <subject>

<body>

Semver: PATCH | MINOR | MAJOR
BREAKING CHANGE: <only if MAJOR>
```

## Examples

```
feat(story): persist interactive step inputs across host restart

Interactive workflows previously lost user-supplied inputs when the worker
process restarted between steps. Inputs are now serialized with the rest
of the story state under the existing storage contract.

Semver: MINOR
```

```
refactor(cqrs)!: return Result.Failure instead of throwing in pipeline

The MediatR pipeline behaviour now surfaces validation errors as
Result.Failure values, aligning with §3 of the Coding Guide. Consumers
that previously caught ValidationException must switch to inspecting
Result.IsFailure.

Semver: MAJOR
BREAKING CHANGE: ValidationException no longer thrown from CQRS pipeline.
See docs/features/2026-07-21-cqrs-validation-result.md.
```

