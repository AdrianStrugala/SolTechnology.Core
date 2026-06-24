---
adr: 012-production-pattern-adoption-wave-2
step: 12 of 24
status: to-do
---

# Step 12: A1.2 — Idempotency middleware + options + `AddIdempotency`/`UseIdempotency` + logging (`Core.Api`)

## Summary
Add the inbound HTTP idempotency middleware that consumes the store + selector from step 11: it reads
the `Idempotency-Key` header, namespaces it by tenant/principal, replays the stored full response on
a duplicate, removes the key if the core handler throws, and traces the lifecycle behind one logging
interface. Ships with its `IdempotencyOptions` (the options class travels with the middleware that
consumes it) and the `AddIdempotency()` / `UseIdempotency()` entry points.

## Affected components
- `src/SolTechnology.Core.API/Idempotency/IdempotencyMiddleware.cs` — the middleware.
- `src/SolTechnology.Core.API/Idempotency/IdempotencyOptions.cs` — options (header name, legacy
  header, TTL, key-namespacing toggle) — **ships with its consumer (the middleware)**.
- `src/SolTechnology.Core.API/Idempotency/IIdempotencyLogger.cs` +
  `…/IdempotencyLogger.cs` — single-interface lifecycle logging
  (received / stored / replayed / conflict / not-completed / removed-on-exception).
- `src/SolTechnology.Core.API/Idempotency/IdempotencyServiceCollectionExtensions.cs` —
  `AddIdempotency(Action<IdempotencyOptions>?)` (registers store from step 11 + selector + logger +
  options with `ValidateOnStart()`); registers `InMemoryIdempotencyStore` as the default.
- `src/SolTechnology.Core.API/Idempotency/IdempotencyApplicationBuilderExtensions.cs` —
  `UseIdempotency()`.
- `docs/Api.md` — "HTTP idempotency" section (registration, key header, semantics).
- `tests/SolTechnology.Core.API.Tests/` — duplicate-replay, store-vs-skip-`5xx`, remove-on-exception,
  tenant-namespacing, legacy-header fallback.

## Details
- **Key source:** read `Idempotency-Key`. Support a **deprecated legacy header** via a fallback,
  marked `[Obsolete]` on whatever public surface exposes it, so old clients keep working while the
  new header is canonical.
- **Namespacing (guard-rail):** the effective key is `{tenantId}/{key}` (principal/tenant-scoped) so
  keys cannot collide across tenants. Source the tenant/principal from the correlation/principal
  context already present in the host; document the expectation.
- **Replay:** on a duplicate completed key, replay the **stored full response** (status + headers +
  body) from step 11's `StoredResponse` and short-circuit the pipeline.
- **Store policy:** only persist responses the `IResponseSelector` accepts (`2xx`–`4xx`, never
  `5xx`).
- **Remove-on-exception (guard-rail):** if the core handler throws, remove the key so a crashed
  request can be safely retried.
- **Concurrency:** use the store's atomic add (step 11) to detect an in-flight duplicate; return a
  defined "in progress / conflict" response rather than racing two executions.
- **Lifecycle logging:** all transitions go through `IIdempotencyLogger` at a single level, so an
  operator can silence or redirect them in one place.
- Do **not** auto-wire into `AddApiCore`/`UseSwaggerWithVersioning` — idempotency is opt-in per host.

## Acceptance criteria
- A repeated request with the same `Idempotency-Key` replays the stored response without re-running
  the handler.
- A `5xx` response is never stored (so the client can retry); `2xx`–`4xx` are.
- A handler exception removes the key (next retry re-executes).
- Keys are tenant/principal-namespaced (`{tenantId}/{key}`); cross-tenant same-key requests do not
  collide.
- The legacy header still works and its surface is `[Obsolete]`.
- All lifecycle transitions are logged through `IIdempotencyLogger`.
- `docs/Api.md` documents the middleware and its semantics.

## Open questions
- The in-flight-duplicate response shape (409 Conflict `ProblemDetails` vs 425 Too Early). Recommend
  a `ProblemDetails`-shaped 409 with `recoverable=true` (pairs with step 02); flag for the reviewer.

## Premortem mitigations (required — added by the `00` gate, 2026-06-24)
- **M1 (correctness, H):** the middleware MUST use the store's **atomic add** (step 11) to detect an
  in-flight duplicate and return the defined conflict response rather than racing two executions; add
  a concurrency test (two simultaneous same-key requests ⇒ one execution + one conflict/replay).
- **M1 (correctness, H):** keep the "never store `5xx`" + "remove key on handler exception" tests as
  hard acceptance — these two guard-rails are the difference between a transient blip and a
  permanently wedged key for payment-style endpoints.
- **M7 (config, M):** `AddIdempotency()` MUST register `IdempotencyOptions` with `.ValidateOnStart()`
  (ADR-010 G3) so misconfiguration fails at boot, not first request.

