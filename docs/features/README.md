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

Status values: `⬜ To-do` / `🔍 Implementing` / `✅ Done`.

## Layout

Same three-folder, mutually-exclusive state model as ADR plans:

```
NNN-feature-name/
  summary.md
  to-do/        ← steps not yet started
  reviewed/     ← drafts produced by the `plan-reviewer` agent
  done/         ← completed steps
```

- **Spec file**: `NNN-kebab-title.md` (monotonic numbering, no gaps, separate from ADR numbers).
- **Step files**: `NN-step-title.md` (numeric, kebab-case, no dates).
- **Premortem `00` gate** still applies when the feature touches public API, `ModuleInstaller`,
  `Directory.Build.props`, or a persisted contract.
- **`summary.md`** is the row-by-row tracker (`⬜ to-do` / `🔍 reviewed` / `✅ done`).

## Decision vs feature

If you cannot name a hard-to-reverse choice with at least two real alternatives, it is a feature —
plan it here. If a feature reveals a buried decision (a broker, a public contract, a vendor swap),
split the decision into an ADR and link it from the feature spec.


