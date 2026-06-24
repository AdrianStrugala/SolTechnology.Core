---
adr: 012-production-pattern-adoption-wave-2
step: 15 of 24
status: to-do
---

# Step 15: B1.2 — Inbound extraction + response enrichment (`Core.Api`)

## Summary
On the inbound edge, extract the correlation id from **header *and* query string** (some clients can
only set query params) and write it back on **every response** — including error responses — via
`HttpResponse.OnStarting(...)`. Consumes the two-level model from step 14. This closes the gap where
the correlation id is currently only attached to `ProblemDetails`, not to successful responses.

## Affected components
- `src/SolTechnology.Core.API/Correlation/` (new area) — inbound extraction (header + query) and a
  response-enrichment hook registered via `HttpResponse.OnStarting`.
- `src/SolTechnology.Core.API/ModuleInstaller.cs` — wire the extraction + response enrichment into
  the existing correlation setup (the module already resolves `ICorrelationIdService` for
  `ProblemDetails.Extensions["correlationId"]`).
- `docs/Api.md` — document header+query extraction and the always-on response header.
- `tests/SolTechnology.Core.API.Tests/` — header-source, query-source, and response-header-on-success
  + response-header-on-error tests.

## Details
- **Extraction precedence:** header first, then query string (document the precedence). Use the
  platform-id key from step 14; if a client id is supplied, capture it too (two-level).
- **Response enrichment via `OnStarting`:** register a callback that writes the
  `X-Correlation-Id` (platform) response header before the response starts, so it is present on
  success **and** on error paths (the current code only stamps the `ProblemDetails` extension). Avoid
  double-writing where the ProblemDetails pipeline already sets it — `OnStarting` should be the
  single writer for the header.
- Keep alignment with the existing `ApiProblemDetailsFactory` `correlationId` extension — the header
  and the extension must carry the same value (one token, one search in Seq/App Insights).
- This is `Core.Api`-only; it depends on the model from step 14 but not on steps 16/17.

## Acceptance criteria
- The correlation id is extracted from a request header and, when absent there, from the query
  string (documented precedence).
- Every response carries the `X-Correlation-Id` header — successful **and** error responses — written
  via `OnStarting`.
- The response header value matches the `ProblemDetails.extensions.correlationId` on error paths.
- `docs/Api.md` documents extraction sources and the always-on response header.
- Tests cover header source, query source, success-response header, and error-response header.

## Open questions
- Query-string parameter name. Recommend mirroring the header name (`X-Correlation-Id` →
  `correlationId` query key) and documenting it; flag for the reviewer.

