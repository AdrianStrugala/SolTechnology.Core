---
adr: 010-production-pattern-adoption-programme
step: 08 of 10
status: to-do
---

# Step 08: Author the Hangfire-defaults child ADR (H4)

## Summary
Author the child ADR (provisional ADR-017) that finalizes the recommended retry-backoff and
worker-count defaults and decides whether a `MigrateHangfire()` convenience ships as code. H4 is
"document-only" today (guidance already in `docs/Hangfire.md`); this ADR closes the
code-vs-doc question. Seeds its own plan and premortem.

## Affected components
- `docs/adr/<next>-hangfire-defaults-and-migrate.md` — the child ADR.
- `docs/adr/<next>-hangfire-defaults-and-migrate/` — its plan folder (if any code ships).

## Details
- **Retry/worker defaults.** Finalize the recommended `AutomaticRetry` backoff array and
  `WorkerCount` default guidance in `docs/Hangfire.md`.
- **`MigrateHangfire()` convenience.** Decide whether an `app.MigrateHangfire()`-style app-builder
  helper ships as code in `SolTechnology.Core.Hangfire`, or stays a documented app-owned pattern.
  The plugin references `Hangfire.Core` only (ADR-009) — keep storage/server bootstrap app-owned.
- **Do not edit ADR-009.** It is published; per `CLAUDE.md` §1, only an Amendment/cross-link note is
  permitted there. H4 lands as its own ADR + `docs/Hangfire.md` update.

## Acceptance criteria
- Child ADR authored with blue/red + premortem-as-final-step; semver **MINOR** if `MigrateHangfire()`
  ships, otherwise **PATCH** (docs-only).
- The code-vs-doc decision for `MigrateHangfire()` is explicit.
- ADR-009 is not edited beyond a permitted cross-link note.
- Index row added in `docs/adr/README.md`.

## Open questions
- Ships `MigrateHangfire()` as code, or documents the pattern only? — decided within this child ADR.

