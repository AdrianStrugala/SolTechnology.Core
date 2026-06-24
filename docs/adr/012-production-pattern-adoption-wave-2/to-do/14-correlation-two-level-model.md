---
adr: 012-production-pattern-adoption-wave-2
step: 14 of 24
status: to-do
---

# Step 14: B1.1 — Two-level correlation model (`Core.Logging`)

## Summary
Extend the correlation foundation shipped in ADR-010 with an optional **two-level** model: a *client*
correlation id (caller-supplied, echoed) and a *platform* correlation id (internal, always present),
with distinct header keys and lifetimes. This is the model/abstraction the other B1 steps
(15 API, 16 HTTP, 17 MessageBus) consume, so it lands first.

## Affected components
- `src/SolTechnology.Core.Logging/Correlations/ICorrelationIdService.cs` — extend to expose both
  ids (e.g. `ClientCorrelationId` + `PlatformCorrelationId`) while preserving the existing
  single-id members (additive — do not break the ADR-010 surface).
- `src/SolTechnology.Core.Logging/Correlations/CorrelationIdService.cs` — implement the two-level
  semantics (platform id always generated if absent; client id only when supplied).
- `src/SolTechnology.Core.Logging/Correlations/CorrelationId.cs` — extend the value type if needed
  to carry both, or add a sibling type; keep existing usages compiling.
- `src/SolTechnology.Core.Logging/Correlations/CorrelationHeaderNames.cs` (new or existing) —
  constants for the distinct client vs platform header keys.
- `docs/Log.md` — document the two-level model and the header keys.
- `tests/SolTechnology.Core.Logging.Tests/` — model tests (platform always present; client optional;
  distinct keys).

## Details
- **Additive only.** The existing `ICorrelationIdService.GetOrGenerate()` and the single
  `X-Correlation-Id` behaviour from ADR-010 must keep working unchanged — the platform id maps onto
  the existing id so current consumers see no difference. The client id is the new, optional layer.
- **Lifetimes:** platform id is internal and always present (generated at the edge if absent); client
  id is whatever the caller supplied and is echoed back but never invented.
- **Header keys:** distinct constants for client vs platform so steps 15–17 can read/write the right
  one. Keep the existing `X-Correlation-Id` as the platform key for backward compatibility; add a
  separate client key.
- The harvest marks the two-level split as "optional but clean" — implement the model here so the
  must-have deltas (response-enrichment in step 15, outbound in 16, queue in 17) have a clean
  foundation, but the *split itself* can remain off by default if the reviewer prefers a single id.

## Acceptance criteria
- `ICorrelationIdService` exposes both a platform id (always present) and an optional client id,
  additively over the ADR-010 surface.
- Existing single-id behaviour and `X-Correlation-Id` semantics are unchanged (no breaking change).
- Distinct header-key constants exist for client vs platform.
- `docs/Log.md` documents the model.
- Tests prove platform-always-present, client-optional, and backward compatibility.

## Open questions
- Whether to default the two-level split **on** or **off**. Recommend off by default (single
  platform id preserves ADR-010 behaviour); the client layer is opt-in. Flag for the reviewer.

