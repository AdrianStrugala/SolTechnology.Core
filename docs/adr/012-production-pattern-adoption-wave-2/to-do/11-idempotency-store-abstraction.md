---
adr: 012-production-pattern-adoption-wave-2
step: 11 of 24
status: to-do
---

# Step 11: A1.1 — Idempotency store abstraction + in-memory store + response selector (`Core.Api`)

## Summary
Lay the persistence + policy primitives for HTTP request idempotency in `Core.Api`: the
`IIdempotencyStore` abstraction, the stored-response model, an in-memory default store, and the
`IResponseSelector` strategy that decides what is cacheable (store `2xx`–`4xx`, **never** `5xx`).
This is the "what gets stored and where" slice — pure primitives, no middleware yet. The middleware
+ options + DI entry land in step 12; the Redis store in step 13. Split this way to keep plumbing
(store) out of the request-pipeline logic (middleware).

## Affected components
- `src/SolTechnology.Core.API/Idempotency/IIdempotencyStore.cs` — abstraction
  (`TryGet`, `Save`, `Remove`, all `async` + `CancellationToken`).
- `src/SolTechnology.Core.API/Idempotency/StoredResponse.cs` — the captured response model (status
  code, headers subset, body bytes, content type, stored-at timestamp).
- `src/SolTechnology.Core.API/Idempotency/InMemoryIdempotencyStore.cs` — default store
  (memory-cache-backed; `TimeProvider`-sourced expiry per ADR-010 G1).
- `src/SolTechnology.Core.API/Idempotency/IResponseSelector.cs` +
  `src/SolTechnology.Core.API/Idempotency/DefaultResponseSelector.cs` — the cacheability strategy.
- `tests/SolTechnology.Core.API.Tests/` — store round-trip + selector tests.

## Details
- **`IResponseSelector` rule (acceptance-critical):** "store `2xx`–`4xx`, **never** `5xx`" so a
  transient server error stays retryable. Encode it as a tiny strategy interface (a single
  `bool ShouldStore(int statusCode)`), default impl returns `status is >= 200 and < 500`.
- **`StoredResponse`** captures enough to **replay the full response** byte-for-byte on a duplicate:
  status, content-type, a curated header set (avoid replaying hop-by-hop / `Set-Cookie` unless
  intended), and the body. Keep the model serialisable so the Redis store (step 13) can persist it.
- **In-memory store** is the zero-config default; expiry/TTL comes from options in step 12 but the
  store API should accept an expiry argument so it stays options-agnostic here.
- **Concurrency:** the store must tolerate concurrent duplicate keys (a "first writer wins" / atomic
  add). Document the contract the middleware (step 12) relies on for conflict detection.
- No DI registration entry point in this step (that arrives with the middleware in step 12) — these
  are primitives the middleware composes. Keep them `internal` where they need not be public yet,
  `public` where step 13's external store impl must implement `IIdempotencyStore`.

## Acceptance criteria
- `IResponseSelector` default stores `2xx`–`4xx` and never `5xx` (table-driven test across status
  ranges).
- `InMemoryIdempotencyStore` round-trips a `StoredResponse` and honours expiry via `TimeProvider`.
- The store exposes an atomic add so concurrent duplicates can be detected by step 12.
- `StoredResponse` carries everything needed to replay a full response.
- No `5xx` is ever persisted through the selector.

## Open questions
- Header-replay policy: which headers are safe to store/replay. Recommend an allow-list
  (`Content-Type`, `Location`, app-specific) rather than replaying everything; flag for the reviewer.

