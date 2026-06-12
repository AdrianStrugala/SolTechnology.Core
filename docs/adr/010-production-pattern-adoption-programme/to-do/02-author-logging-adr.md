---
adr: 010-production-pattern-adoption-programme
step: 02 of 10
status: to-do
---

# Step 02: Author the Logging child ADR (L1, L2, L3)

## Summary
Author the child ADR (provisional ADR-011) that unifies correlation, adds `ILogger` scope helpers,
and ships PII masking for `SolTechnology.Core.Logging`. This is the **foundational** workstream — its
L1 correlation contract is what the MessageBus workstream (ADR-015, M2) consumes — so it is authored
first. The ADR seeds its own `to-do/` plan and ends with its own premortem.

## Affected components
- `docs/adr/<next>-logging-correlation-and-masking.md` — the child ADR (decision only, no code).
- `docs/adr/<next>-logging-correlation-and-masking/` — its `summary.md` + `to-do/` plan.

## Details
- **L1 — single correlation primitive.** Make `ICorrelationIdService`
  (`src/SolTechnology.Core.Logging/Correlations/`) the documented single correlation dependency.
  Evidence: HTTP and the shipped Hangfire `CorrelationIdJobFilter` already consume it;
  `MessageBusReceiver` does not — that gap is closed in ADR-015, cross-link it.
- **L2 — scope helpers.** `ILogger` extensions `PushToScope`/`AddToScope` returning `IDisposable`,
  wrapping `BeginScope(...)` for a single key/value. Lower priority — later step in the child plan.
- **L3 — PII masking.** A `Mask()` helper plus a masking `JsonConverter`/enricher.
  **Guard-rail (source defect):** the masking contract MUST be explicit (partial-mask vs
  `MaskToZero`); a mask that always returns `0`/empty is forbidden.
- Stack/conventions: `ClaudeCodingGuide.md` §11 (logging), §9 (class size), §18 (doc shape);
  update `docs/Log.md`.

## Acceptance criteria
- Child ADR authored with `Status: Proposed`, blue/red argument, and a premortem as its plan's final
  step; semver declared **MINOR** (additive).
- L3 mask contract is explicit in the ADR (no always-returns-zero mask).
- Cross-link to ADR-015 records that MessageBus correlation consumption lands there.
- Index row added in `docs/adr/README.md` for the child ADR.

## Open questions
- none — L1/L2/L3 are unblocked.

