# Feature Plans

Multi-step delivery of features that are **not** architecture decisions. A feature adds or
extends capability without a hard-to-reverse choice between alternatives. Decisions live in
[`docs/adr/`](../adr/README.md); this folder is the **backlog**, kept separate so the ADR index
stays a list of decisions, not a roadmap.

The plan workflow is identical to ADR-006 — only the parent folder differs.

## Index

| # | Title | Created | Status |
|---|---|---|---|
| 001 | [Production pattern adoption — wave 1](001-production-pattern-adoption-programme.md) | 2026-06-12 | ✅ Done |
| 002 | [Production pattern adoption — wave 2](002-production-pattern-adoption-wave-2.md) | 2026-06-24 | ✅ Done |
| 2026-07-06 | [JWT Bearer authentication + API-key hardening](2026-07-06-jwt-bearer-authentication.md) | 2026-07-06 | 🔍 Implementing — see [summary](2026-07-06-jwt-bearer-authentication/summary.md) |

Status values: `⬜ To-do` / `🔍 Implementing` / `✅ Done`.

## Layout

Layout, naming, status vocabulary, and gate fields are fixed by
[ADR-006](../adr/006-implementation-plan-workflow.md) (amended 2026-07-04) — this section is a
pointer, not a second source of truth:

```
YYYY-MM-DD-<feature>.md               ← feature spec
YYYY-MM-DD-<feature>/                 ← working folder (exists only while work is in flight)
  summary.md                          ← step table + pipeline gate fields (frontmatter)
  steps/
    00-run-premortem.md               ← opening bracket (only when premortem: pending)
    01-<step-title>.md
    NN-retrospective.md               ← closing bracket, always the highest number
```

- **Spec file**: `YYYY-MM-DD-kebab-title.md` — dates self-allocate; no numbers to reserve.
- **Step files**: `NN-step-title.md` (numeric, kebab-case, no dates). Files never move —
  state lives in each file's frontmatter `status: to-do | blocked | in-progress | done`.
- **Gate fields** (`review:`, `premortem:`) live in `summary.md` frontmatter per ADR-006 §7;
  the premortem gate applies when the feature touches public API, `ModuleInstaller`,
  `Directory.Build.props`, or a persisted contract.
- **`summary.md`** mirrors each step's status (`⬜ to-do` / `⛔ blocked` / `🔧 in-progress` /
  `✅ done`) in the same change that flips it.

## Decision vs feature

If you cannot name a hard-to-reverse choice with at least two real alternatives, it is a feature —
plan it here. If a feature reveals a buried decision (a broker, a public contract, a vendor swap),
split the decision into an ADR and link it from the feature spec.


