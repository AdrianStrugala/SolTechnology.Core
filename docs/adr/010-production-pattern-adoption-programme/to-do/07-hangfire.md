---
adr: 010-production-pattern-adoption-programme
step: 07 of 07
status: to-do
---
# Step 07: Hangfire — document defaults + `MigrateHangfire()` pattern

## Summary
Document recommended retry-backoff array, worker-count guidance, and the `MigrateHangfire()` snippet in `docs/Hangfire.md`. No code changes.

## Affected components
- `docs/Hangfire.md` — add "Recommended defaults" + "Database migration" sections

## Details
- **Retry backoff:** `[10s, 30s, 1m, 5m, 15m, 30m, 1h, 2h, 4h, 8h]` (10 attempts). Enforced by `SmartRetryJobFilter`.
- **Worker count:** `Environment.ProcessorCount * 2` (Hangfire default). Document, don't override.
- **`MigrateHangfire()`:** Document the `SqlServerStorage.Install(...)` snippet. Decision: docs-only, no code helper (storage is app-owned).

## Acceptance criteria
- `docs/Hangfire.md` has both sections
- No code changes

## Open questions
- none

