---
adr: 012-production-pattern-adoption-wave-2
step: 19 of 24
status: done
---

# Step 19: B3 — Typed service-call error taxonomy (`Core.HTTP`)

## Summary
Offer a typed-error result shape from the HTTP layer so service-to-service call sites get an
actionable error instead of a bare exception or status code: `ConnectionFailed` (detected by
unwrapping `SocketException`), `HttpErrorCode` (carries status + extracted validation message), and
`DeserializationError` — mapped onto Core's canonical `Error` subtypes
(`src/SolTechnology.Core/Errors/`).

> **Kept separate from B2 (step 18), per decision.** B3 (this step) is the **error-result shape**
> concern; B2 (step 18) is the **retry pipeline** concern. Independently mergeable; deliberately
> **not** merged.

## Affected components
- `src/SolTechnology.Core.HTTP/` (new error area) — typed call-error types and a `Result<T>`-returning
  call surface (or extension) that classifies failures into the taxonomy.
- Mapping onto `src/SolTechnology.Core/Errors/` — `ConnectionFailed`/timeout → `TimeoutError` or a new
  connection-flavoured `Error`; `HttpErrorCode` → status-appropriate subtype; `DeserializationError`
  → a deserialisation `Error`. Reuse existing subtypes (`TimeoutError`, `ValidationError`, etc.)
  where they fit rather than inventing parallels.
- `src/SolTechnology.Core.HTTP/RequestBuilder.cs` / `HttpClientExtensions.cs` — surface the typed
  result path alongside the existing exception-throwing path (`HttpRequestFailedException`).
- `docs/Clients.md` — document the typed-error result option. (**Note:** `docs/HTTP.md` does not exist
  in this repo — the HTTP module is documented in `docs/Clients.md`.)
- `tests/SolTechnology.Core.HTTP.Tests/` — **existing** project: connection failure → `ConnectionFailed`;
  non-2xx → `HttpErrorCode` with extracted message; bad body → `DeserializationError`.

## Details
- **Detection:** unwrap inner exceptions to find `SocketException` for `ConnectionFailed`; capture
  the status code + any extracted validation/`ProblemDetails` detail for `HttpErrorCode`; catch
  `JsonException`/deserialisation failures for `DeserializationError`.
- **Map onto Core `Error`:** the public contract should surface a Core `Error` subtype so call sites
  use the same `Result`/`Error` model as the rest of the stack (no bespoke HTTP-only error type
  leaking into domain code). Carry the recoverability hint where known (a connection failure is
  typically recoverable; a `400 ValidationError` is not) — aligns with B2/B4.
- **Additive:** the existing `HttpRequestFailedException` throwing path stays. The typed-result path
  is an opt-in alternative for callers that prefer railway-style handling over try/catch.
- Keep the taxonomy small and closed (the three cases above) — do not over-model.

## Acceptance criteria
- A socket-level failure yields `ConnectionFailed` mapped to a Core `Error` (recoverable).
- A non-2xx response yields `HttpErrorCode` carrying the status + extracted message, mapped to the
  status-appropriate Core `Error` subtype.
- A bad/undeserialisable body yields `DeserializationError` mapped to a Core `Error`.
- The existing exception-throwing path is unchanged.
- `docs/Clients.md` documents the typed-result option.

## Open questions
- Whether to add a new `ConnectionError` subtype under `src/SolTechnology.Core/Errors/` or reuse
  `TimeoutError`/a generic `Error` for connection failures. Recommend a dedicated subtype only if a
  consumer needs to branch on it; otherwise reuse. Flag for the reviewer (touches the foundation
  `Error` JSON-derived-type list — a public-surface change with its own premortem weight).


