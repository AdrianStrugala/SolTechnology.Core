---
adr: 012-production-pattern-adoption-wave-2
step: 22 of 24
status: to-do
---

# Step 22: F — Document-only recipes (rate limiting · singleton→scoped bridge · delay-queue)

## Summary
Capture the three accepted **document-only** patterns from harvest section F as recipes — **no
production code**. Grouped into one step because each is a short, code-free doc addition; kept
separate from any code step. Each recipe uses built-in .NET primitives or documents an existing
trade-off rather than shipping new surface.

## Affected components
- `docs/Api.md` — **Per-principal rate limiting** recipe.
- `docs/Log.md` (or a worker-patterns doc) — **Singleton→scoped correlation bridge** recipe (B6).
- `docs/Cron.md` / `docs/Hangfire.md` — **Delay-queue vs Hangfire** decision note (C4).

## Details
- **Per-principal rate limiting (Core.Api recipe):** show how to use the built-in
  `Microsoft.AspNetCore.RateLimiting` middleware, resolving the limit from the principal/tenant with
  a generic fallback, and returning a **`429` shaped like the standard `ProblemDetails`/error
  envelope** (so it matches the rest of the API's error contract — reference
  `ApiProblemDetailsFactory` conventions and the `recoverable` field from step 02). No new code in
  `Core.Api`; a copy-pasteable host snippet.
- **Singleton→scoped correlation bridge (B6):** document how a singleton (background worker, health
  check) spins a DI scope and manually attaches principal + correlation context (the harvested app's
  `ScopeBuilderService` shape — `WithTenant`/`WithCorrelation`/`WithSystemTenant`, ordered disposal),
  built on the `ICorrelationIdService` from ADR-010 + step 14. Explicitly a **pattern**, not shipped
  code (it is too tenant-model-coupled to generalise).
- **Delay-queue vs Hangfire (C4):** a short decision note — "hold a message until time T then
  enqueue" overlaps conceptually with Hangfire scheduled jobs which Core already provides; document
  when (not) to build a non-Hangfire delay-queue, cross-linking `docs/Hangfire.md`. Mirrors the
  parked FI-002 reasoning (prefer Hangfire-backed jobs).
- Keep each recipe self-contained with a clear "this is a recipe, not shipped code" banner.

## Acceptance criteria
- `docs/Api.md` has a per-principal rate-limiting recipe using the built-in limiter + a
  `ProblemDetails`-shaped 429, with no new `Core.Api` code.
- `docs/Log.md` (or worker doc) has the singleton→scoped correlation bridge recipe, flagged as a
  pattern not shipped code.
- `docs/Cron.md`/`docs/Hangfire.md` has the delay-queue-vs-Hangfire decision note.
- No files under `src/` or `tests/` are added or changed by this step.

## Open questions
- Home for the singleton→scoped recipe: `docs/Log.md` vs a new `docs/Workers.md`. Recommend
  `docs/Log.md` (correlation lives there) to avoid a thin new doc; flag for the reviewer.

