---
adr: 012-production-pattern-adoption-wave-2
step: 18 of 24
status: reviewed
---

# Step 18: B2 — Recoverable-aware retry predicate (`Core.HTTP`)

## Summary
Add an opt-in, result-aware retry hook to the `Core.HTTP` resilience pipeline so a client can retry
**only when the response body's errors are all recoverable** — avoiding wasted retries on a
deterministic business rejection that returned `200`/`4xx` but will never succeed. Pairs with B4
(step 02): the producer states recoverability; this lets the consumer act on it.

> **Kept separate from B3 (step 19), per decision.** B2 (this step) is the **retry pipeline** concern;
> B3 (step 19) is the **error-result shape** concern. They are independently mergeable and are
> deliberately **not** merged.

## Affected components
- `src/SolTechnology.Core.HTTP/HttpPolicyConfiguration.cs` — add an opt-in predicate knob (e.g. a
  `RetryWhen` delegate / a flag enabling body-content-aware retry).
- `src/SolTechnology.Core.HTTP/HttpPolicyFactory.cs` — extend `ShouldRetry` to consult the predicate
  in addition to the existing status-code + transport-exception rules.
- `src/SolTechnology.Core.HTTP/ModuleInstaller.cs` — surface a way to register the predicate (e.g. an
  overload accepting `Func<HttpResponseMessage,bool>` or a result-aware predicate type).
- `docs/Clients.md` + `docs/HTTP-Production-Checklist.md` — document the opt-in predicate and the
  "retry only when all body errors are recoverable" semantics. (**Note:** `docs/HTTP.md` does not
  exist in this repo — the HTTP module is documented in `docs/Clients.md`, and the resilience
  defaults checklist in `docs/HTTP-Production-Checklist.md`.)
- `tests/SolTechnology.Core.HTTP.Tests/` — **existing** project: predicate fires only when all errors
  recoverable; a non-recoverable error short-circuits retries even on a retryable-looking status.

## Details
- **Existing behaviour (do not regress):** `HttpPolicyFactory.ShouldRetry` already refuses retries on
  unsafe verbs (unless `RetryOnUnsafeVerbs`) and retries on a fixed transient status set
  (`408/429/500/502/503/504`) + transport exceptions. The new predicate is **additive and opt-in** —
  when set, it can *further restrict* (or, by design, further qualify) retries based on the response
  body, but the default pipeline is unchanged when no predicate is supplied.
- **Body inspection caveat:** reading the response body inside a retry predicate must not consume a
  forward-only stream the caller later needs. Document/buffer appropriately (or have the predicate
  operate on already-buffered content). Flag this as the main implementation risk.
- **Recoverable source:** the predicate's reference shape is "retry only when every error in the body
  carries `recoverable = true`" — aligning with the `Error.Recoverable` / B4 extension. Provide a
  default helper that parses the standard `ProblemDetails`/error envelope's `recoverable` field, plus
  the raw `Func<HttpResponseMessage,bool>` escape hatch.
- Keep the predicate composable with the existing breaker predicate (`ShouldBreak`) — a body-level
  non-recoverable error should still let the breaker observe transport/status failures as today.

## Acceptance criteria
- With no predicate configured, retry behaviour is byte-for-byte the existing behaviour.
- With the predicate configured, a response whose body errors are all recoverable is retried (within
  the existing status/verb rules); a response with any non-recoverable error is not retried.
- Body inspection does not corrupt the response stream returned to the caller.
- `docs/Clients.md` (and the `docs/HTTP-Production-Checklist.md` entry) document the opt-in predicate.
- Tests cover all-recoverable (retry), any-non-recoverable (no retry), and no-predicate (unchanged).

## Open questions
- Whether the predicate can *expand* retries (retry a normally-non-retryable status when recoverable)
  or only *restrict* them. Recommend restrict-only for safety; flag for the reviewer.

