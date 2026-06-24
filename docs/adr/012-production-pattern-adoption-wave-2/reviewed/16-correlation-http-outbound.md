---
adr: 012-production-pattern-adoption-wave-2
step: 16 of 24
status: reviewed
---

# Step 16: B1.3 — Outbound `AddCorrelation` helper (`Core.HTTP`)

## Summary
Add a typed-client outbound propagation helper — `HttpRequestHeaders.AddCorrelation(service)` — so an
outgoing request carries the current correlation id explicitly when a caller hand-builds a request,
complementing the existing `CorrelationPropagatingHandler` that already stamps headers automatically.
Consumes the two-level model from step 14 (platform id flows; client id per policy).

## Affected components
- `src/SolTechnology.Core.HTTP/Handlers/CorrelationPropagatingHandler.cs` — confirm it propagates the
  **platform** id from the step-14 model (and the client id only if policy says so); adjust if the
  two-level split changes which header(s) it writes.
- `src/SolTechnology.Core.HTTP/Correlation/HttpRequestHeadersExtensions.cs` (new) —
  `AddCorrelation(this HttpRequestHeaders headers, ICorrelationIdService service)` for explicit
  per-request propagation.
- `docs/Clients.md` — document both the automatic handler and the explicit helper. (**Note:**
  `docs/HTTP.md` does not exist in this repo — the HTTP module is documented in `docs/Clients.md`.)
- `tests/SolTechnology.Core.HTTP.Tests/` — **existing** project: header-present-after-AddCorrelation;
  handler-still-works tests.

## Details
- The existing `AddHTTPClient(..., propagateCorrelation: true)` path already inserts
  `CorrelationPropagatingHandler` before the resilience handler so every retry carries the same
  `X-Correlation-Id`. This step **adds the explicit helper** for cases where a caller constructs an
  `HttpRequestMessage` directly and wants to stamp the id without the handler, and **aligns the
  handler** with the two-level model from step 14 (platform id is the one that flows on
  service-to-service hops).
- Keep `propagateCorrelation: false` hosts untouched (they own correlation via OpenTelemetry/their
  own middleware) — the explicit helper is opt-in and does not change the handler's opt-out.
- Client id policy: on outbound service-to-service calls the **platform** id flows; whether the
  client id also flows is a documented choice — default to platform-only on internal hops (the
  client id is a caller-facing concept), matching the queue rule in step 17.

## Acceptance criteria
- `AddCorrelation(service)` stamps the platform correlation header onto an `HttpRequestHeaders`
  instance.
- `CorrelationPropagatingHandler` propagates the platform id consistently with the step-14 model.
- `propagateCorrelation: false` behaviour is unchanged.
- `docs/Clients.md` documents the automatic vs explicit options.
- Tests cover the explicit helper and confirm the handler still propagates on retries.

## Open questions
- Whether the client id ever flows outbound on internal hops. Recommend platform-only by default
  (consistent with step 17's queue rule); flag for the reviewer.

