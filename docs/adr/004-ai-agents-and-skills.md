# ADR-004: AI Agents and Skills Library

> **Status:** Accepted
> **Decision Date:** 2026-05-15
> **Decision Maker:** Repository maintainers
> **Stakeholders:** Contributors using Claude Code / GitHub Copilot

---

## Context

SolTechnology.Core has grown a stable Tale Code doctrine ([`docs/ClaudeCodingGuide.md`](../ClaudeCodingGuide.md)),
three Architecture Decision Records, and five per-module review templates under
[`docs/reviews/`](../reviews/). AI assistants (Claude Code, Copilot) participate in nearly every
change, but they had no codified, repo-specific procedure for:

1. Risk-checking changes before merge (especially public NuGet API and `ModuleInstaller`
   contracts).
2. Writing commits and ADRs in a uniform shape.
3. Reviewing diffs against the Coding Guide and module reviews.
4. Keeping the docs tree (`docs/`, ADRs, READMEs) internally consistent.

A related effort, **aiex**, packages a "Copilot Change Manager" workflow with a set of skills under
`.github/skills/<name>/SKILL.md` and a shared `AGENTS.md`. aiex is built for an Open Banking
operations domain (Mastercard adapters, EUEC work items, Risk Matrix, legislation) — most of which
is irrelevant here — but its **mechanism** (front-matter SKILL.md files, evidence-based doctrine,
mandatory read-before-use) maps cleanly onto this repo.

## Decision

Adopt the aiex skills mechanism in SolTechnology.Core, with three concrete decisions:

1. **Merge agent doctrine into [`CLAUDE.md`](../../CLAUDE.md).** No separate `AGENTS.md`. The
   "Agents & Skills" section in `CLAUDE.md` is the single source of truth for principles, quality
   standards, the mandatory-read rule, the skill index, and the premortem gate.

2. **Maintain a refactored copy of aiex skills under [`.github/skills/`](../../.github/skills/).**
   We do not git-submodule aiex; we copy and adapt. Adaptations strip the banking domain and
   re-target the workflow at a NuGet-package codebase.

3. **Make `premortem` the central risk gate** for the repo. The skill is mandatory before merging
   any change that touches a public/protected symbol in `src/SolTechnology.Core.*`, a
   `ModuleInstaller.cs`, `Directory.Build.props`, or a persisted contract.

### Skills Adopted

| Skill | Origin | Adaptation |
|---|---|---|
| [premortem](../../.github/skills/premortem/SKILL.md) | aiex `premortem-failure-analysis` | Module-specific failure-mode checklists (CQRS, Story, Logging, HTTP, MessageBus, Sql, Blob, Cache, DI, Build, .NET 10). Output extended with semver classification, blast radius, decision verdict. |
| [blue-red-team](../../.github/skills/blue-red-team/SKILL.md) | aiex `blue-red-team-analysis` | Tale Code readability lens; explicit pairing with premortem; ADR-seeding focus. |
| [documentation-cleanup](../../.github/skills/documentation-cleanup/SKILL.md) | aiex `documentation-cleanup` | Re-targeted from `Documentation/` to [`docs/`](../); added module ↔ doc parity check and ADR structural validation; EUEC rule removed. |
| [code-review](../../.github/skills/code-review/SKILL.md) | none (new) | Built around [`docs/ClaudeCodingGuide.md`](../ClaudeCodingGuide.md) sections and [`docs/reviews/`](../reviews/) templates. |
| [commit-message](../../.github/skills/commit-message/SKILL.md) | none (new) | Conventional Commits with module-name scopes; semver footer mandatory for public-API changes. |
| [implementation-planning](../../.github/skills/implementation-planning/SKILL.md) | aiex `implementation-planning` (concept) | Output is an ADR draft matching [ADR-001..003](.) shape; ends with mandatory premortem gate. |

### Skills Rejected

| aiex skill | Reason |
|---|---|
| `agent-output-file-writer` | Generic IO utility; current tooling persists outputs directly via PR review. |
| `change-scope-selection` | Tied to a multi-repo banking workflow with `vscode/askQuestions`; over-engineered for a single-repo NuGet codebase. |
| `git-change-analysis` | References Mastercard `Services.md` and EUEC work items; no analogue here. |
| `human-factors-framework` | Ten-dimension behavioural-science framework citing 12 books; valuable but out of scope for an OSS NuGet repo. |
| `inverse-dependency-analysis` | Targets a service architecture documented as Mermaid in aiex; SolTechnology.Core has a static module graph already visible in [README.md](../../README.md). |

## Alternatives Considered

1. **Git submodule aiex.** Rejected: pulls in banking-domain skills, legislation HTML, and a
   `Documentation/` tree we do not want to maintain in this repo.
2. **Keep `AGENTS.md` separate from `CLAUDE.md`.** Rejected: a second file with overlapping
   doctrine invites drift. `CLAUDE.md` already enforces a self-improvement rule; folding agent
   doctrine in there keeps one canonical location.
3. **Inline the skill instructions directly inside `CLAUDE.md`.** Rejected: SKILL.md files are
   independently loadable by other agents (Copilot, custom tooling) and benefit from the
   front-matter convention.
4. **Adopt only `premortem`.** Tempting (it was the user's primary interest), but `code-review`,
   `commit-message`, and `documentation-cleanup` were already informal practices in the repo —
   formalising them at the same time costs little and yields a coherent toolkit.

## Consequences

**Positive**

- Every contributor (human or AI) has a uniform, file-cited procedure for risk-checking,
  reviewing, committing, and documenting changes.
- `premortem` makes breaking-change risk explicit and forces module-by-module thinking before a
  public NuGet API ships.
- Skills are independently loadable, so new agents (Copilot, future tooling) plug in by reading
  the same files.
- Doctrine merges into the existing `CLAUDE.md` self-improvement loop; new lessons land in one
  place.

**Negative**

- Slight overhead in writing PRs that touch public API (premortem output expected).
- Skills must be kept in sync with the Coding Guide; each ADR or guide change may require a skill
  edit. `documentation-cleanup` mitigates this by checking ADR/doc/index integrity.

**Semver impact**: PATCH — repo metadata and tooling only; no code change.

## Follow-ups

- Add a PR template referencing the premortem gate.
- Consider a CI check that flags PRs touching `src/SolTechnology.Core.*/**/*.cs` without a
  `Premortem:` block in the PR body.
- Reassess skill rejection list when the repo grows additional non-trivial cross-cutting concerns
  (e.g. observability, security review).

