---
status: completed
created: 2026-06-24
completed: 2026-06-25
---

# Production Pattern Adoption — Wave 2

> Historical delivery record. It may not describe the current system.

## Goal

A second production application — a multi-tenant, multi-region payments / financial-storage
service built on the same Tale-Code philosophy — was onboarded into the production-pattern
programme started by [wave 1](2026-06-12-production-pattern-adoption-wave-1.md). The candidates
were evaluated by target module, semver impact, effort, and production value.

This is the **direct continuation** of the Feature-001 programme: adopt the **accepted** wave-2
items as one production-hardening effort, grouped by module and sequenced as independent steps.

### Constraints

- **Additive only.** Every item is a new API or a new extension field — no breaking change to any
  shipped public symbol. Overall semver impact is **MINOR**.
- **Tale-Code readability first.** New surface follows the module conventions in
  [`docs/ClaudeCodingGuide.md`](../ClaudeCodingGuide.md) (single `ModuleInstaller` entry point,
  options bound + `ValidateOnStart`, `Result`/`Error` at boundaries, `TimeProvider` for time).
- **Build on Feature-001, don't duplicate it.** Correlation work extends the `ICorrelationIdService`
  already shipped in Feature-001; timing diagnostics reuse `TimeProvider` (G1); typed HTTP
  errors map onto the canonical `Error` subtypes (`src/SolTechnology.Core/Errors/`).
- **Guard-rails are source-defect rules**, not suggestions — each carries into its step file's
  acceptance criteria and the `00` premortem gate.

### Affected modules

- `SolTechnology.Core.Api` — A6 security headers, B4 `Recoverable` extension, A1 idempotency,
  B1 inbound/response correlation, F rate-limiting recipe.
- `SolTechnology.Core.Testing` — D1 `Result` assertions, D2 `Ct` matcher, D3 fitness guard tests.
- **New** `SolTechnology.Core.DistributedLock` — A2 distributed lock + Medallion backends.
- **New** `SolTechnology.Core.HealthChecks` — A3 cached upstream check toolkit.
- `SolTechnology.Core.Scheduler` — C1 deployment-slot gating, C2 leader-elected poller.
- `SolTechnology.Core.HTTP` — B1 outbound correlation, B2 recoverable-aware retry, B3 typed errors.
- `SolTechnology.Core.Logging` — B1 correlation model, A5 timing diagnostics, F bridge recipe.
- `SolTechnology.Core.Cache` — A1 Redis idempotency store, A3 cache health check.
- `SolTechnology.Core.SQL` / `SolTechnology.Core.MessageBus` — A3 per-module health checks,
  B1 queue correlation propagation.

### Affected sample apps

- `sample-tale-code-apps/DreamTravel` — compiles against most touched modules; used to smoke-test
  the additive surface. No behaviour change expected (every new feature is opt-in).
- `sample-tale-code-apps/aiia-storage` — `Aiia.Storage.SolutionTests/SolutionTest.cs` is prior art
  for the D3 build-hygiene guard.

## Scope

Ship all accepted wave-2 items under one feature plan, grouped by module, sequenced as independent
steps. Two candidates looked like new packages; both were folded into existing modules during
implementation (sub-sections below). A premortem gate ran before implementation.

### What ships

| Group | Items | Module(s) | New dependency | Semver |
|---|---|---|---|---|
| API quick wins | A6 security-headers middleware · B4 surface `Recoverable` in `ProblemDetails` | `Core.Api` | none | MINOR |
| Testing quick wins | D1 `Result` assertion helpers · D2 `Ct` matcher alias | `Core.Testing` | none | MINOR |
| ~~New package~~ **→ Option B** | A2 distributed lock | **`Core.Cache`** (thin layer, not a separate package) | none (reuses existing `StackExchange.Redis`) | MINOR |
| ~~New package~~ **→ per-module** | A3 health checks: endpoint + per-module checks + upstream base | **`Core.Api`** (endpoint) + `Core.SQL`/`Core.Cache`/`Core.MessageBus`/`Core.HTTP` (checks; base in `Core.HTTP`) | `Microsoft.Extensions.Diagnostics.HealthChecks` (per data/messaging module) | MINOR |
| Background | C1 deployment-slot gating · C2 leader-elected polling base | `Core.Scheduler` (→ `Core.Cache`) | none | MINOR |
| API idempotency | A1 inbound HTTP idempotency middleware + store abstraction | `Core.Api` (+ `Core.Cache` store) | none | MINOR |
| Correlation deltas | B1 two-level model · inbound+response · outbound · queue | `Core.Logging`/`Api`/`HTTP`/`MessageBus` | none | MINOR |
| HTTP enhancements | B2 recoverable-aware retry predicate · B3 typed call-error taxonomy | `Core.HTTP` | none | MINOR |
| Diagnostics | A5 per-request timing diagnostics | `Core.Logging` | none | MINOR |
| Fitness | D3 build-hygiene + test-host containment guard tests | `Core.Testing` + repo self-tests | none | MINOR (docs+tests) |
| Recipes | F per-principal rate limiting · singleton→scoped bridge (B6) · delay-queue vs Hangfire (C4) | docs only | none | PATCH (docs) |

### Out of scope (explicit)

- **FI-001 outbound webhooks** (was A4) and **FI-002 priority worker pool** (was C3) — parked in
  [`docs/future-ideas/`](../future-ideas/README.md); each starts a dated feature when a consumer
  exists.
- **Section E "not porting"** — custom `IDateTimeProvider` (Core uses `TimeProvider`, Feature-001 G1),
  encryption-at-rest, multi-cloud broker switch (Feature-001 Q3), MACRO_CASE JSON policy, app-specific
  domain code.
- **B5** (`OperationCanceledException` response) — already handled by `Core.Api` cancellation
  logging; parity only. **D4** (primary-ctor caution) — already a Core rule (Feature-001 G7).

### Decision sub-section — Distributed Lock (A2) — implemented in `Core.Cache` (Option B)

> **Original plan** was a separate `SolTechnology.Core.DistributedLock` package with
> Medallion.Threading. During implementation (2026-06-24) the maintainer chose **Option B**: a thin
> lock layer directly in `Core.Cache`. Rationale: same Redis, same connection, same namespace — a
> separate package adds complexity with no value when the infra already exists.

**Surface.** `IDistributedLockService.TryAcquireLockAsync(string name, TimeSpan expiry,
CancellationToken ct) → ValueTask<IAsyncDisposable?>`. A non-null handle means the lock is held;
disposing it releases. `null` means "not acquired" — **never an exception** into the caller's loop.

**Backends (in `Core.Cache`):**
- `AddDistributedLock()` — Redis `SET NX EX` with Lua fencing release (production, multi-instance).
- `AddLocalLock()` — in-process `SemaphoreSlim` per key (local dev, single instance).

No Medallion.Threading, no new NuGet dependencies — `StackExchange.Redis` (already a transitive
dependency of the distributed cache tier) is the only requirement.

**Guard-rail.** Lock keys MUST be tenant/principal-namespaced where relevant. Acquisition failure
returns `null` + logs at a single level; it never throws into the host loop.

### Decision sub-section — Health checks (A3) — per-module, no foundation package

> **Original plan** was a separate `SolTechnology.Core.HealthChecks` foundation package that every
> module would reference. During implementation (2026-06-25) the maintainer chose to **drop the
> foundation package** — same reasoning as the DistributedLock Option-B decision: health checks
> should live **next to the implementation they probe**, not behind a shared foundation that every
> module must reference (and that risks dragging ASP.NET into data-store modules).

**Placement.** Each check lives in its own module and references the framework-agnostic
`Microsoft.Extensions.Diagnostics.HealthChecks` **directly**:
- `Core.SQL` → `AddSqlHealthCheck()` (`SELECT 1` connectivity ping)
- `Core.Cache` → `AddRedisHealthCheck()` (Redis ping via `IConnectionMultiplexer`)
- `Core.MessageBus` → `AddServiceBusHealthCheck()` (broker liveness)
- `Core.HTTP` → `BaseUpstreamServiceHealthCheck<TReport>` + `AddUpstreamHttpHealthCheck<TReport>()`
  — the cached upstream base lives here (it probes a downstream `/health` over `HttpClient`)
- `Core.Api` → `HealthReportJsonFormatter` + `MapCoreHealthChecks(path)` — the **only** ASP.NET piece

**Composition.** Consumers call the framework `AddHealthChecks()` directly and chain the per-module
checks; no `AddCoreHealthChecks()` wrapper:
```csharp
builder.Services.AddHealthChecks()
    .AddSqlHealthCheck().AddRedisHealthCheck().AddServiceBusHealthCheck();
app.MapCoreHealthChecks("/health");
```

**Blocker-1 (stronger than the package plan).** With no foundation, there is no shared package that
could drag ASP.NET into `Core.SQL`/`Core.Cache`. Data/messaging modules reference only the
framework-agnostic abstractions; the ASP.NET endpoint is isolated to `Core.Api` (already an ASP.NET
package). The step-21 build-hygiene guard asserts no data/messaging module gains a
`FrameworkReference Microsoft.AspNetCore.App`.

**Guard-rail.** Caller-cancellation MUST rethrow (a cancelled probe is not an Unhealthy
dependency); every upstream call MUST carry its own timeout independent of the probe deadline.

### Guard-rails (source-defect rules, carried into acceptance criteria)

- **DistributedLock** — namespaced keys; acquisition failure → `null` + log, never throw.
- **HealthChecks** — caller-cancellation rethrows; per-call timeout independent of probe.
- **Leader-elected poller (C2)** — `async void` timer callbacks never throw; loops swallow + log +
  continue; `StopAsync` = cancel → release lock → stop timers.
- **Idempotency (A1)** — store `2xx`–`4xx`, **never** `5xx`; remove key on handler exception;
  tenant/principal-namespaced key `{tenantId}/{key}`.
- **Correlation (B1)** — platform id flows onto queue messages; client id deliberately does **not**
  (handlers run out of client context).
- **B4** — `Extensions["recoverable"]` is always emitted (absence ≠ ambiguous); unmapped 5xx ⇒
  recoverable, mapped 4xx ⇒ not, conservative + overridable.

### Dependency impact

| Package (NuGet ID) | Module | Already in repo? | Note |
|---|---|---|---|
| ~~`DistributedLock.*`~~ | ~~`Core.DistributedLock`~~ | — | **Superseded by Option B** — lock uses existing `StackExchange.Redis` in `Core.Cache`. No new dependency. |
| `StackExchange.Redis` | `Core.Cache` | yes (transitive via `Microsoft.Extensions.Caching.StackExchangeRedis`) | Now also a **direct** `PackageReference` (`2.8.41`) for `IConnectionMultiplexer` access in the lock service. |
| `Microsoft.Extensions.Diagnostics.HealthChecks` | `Core.SQL`, `Core.Cache`, `Core.MessageBus`, `Core.HTTP` | no | Framework-agnostic health-check abstractions + builder (`10.0.x`). Referenced **per-module** — no foundation package. **Not** the ASP.NET variant. |
| (existing) `Microsoft.AspNetCore.RateLimiting` | `Core.Api` (recipe only) | built-in (`net10.0`) | F recipe — no `PackageReference`. |

The only new package this wave is the glue `Core.Api.Idempotency.Redis` (step 13) — it adds a `.slnx`
`Project` entry, inherits `src/Directory.Build.props` (so `TreatWarningsAsErrors=true` applies), and
is added to `.github/workflows/publishPackages.yml`. The distributed lock (Option B) and the health
checks (per-module) do **not** add packages — they live in `Core.Cache` and in each probed module
respectively; the health **endpoint** lives in `Core.Api`.

## Alternatives considered

1. **One feature per item, each with its own plan and premortem.** Rejected for the same reason
  wave 1 rejected it: process overhead disproportionate to the work. The items are related
   production-hardening concerns harvested together, not independent decisions. A blue/red review
   (the `00` premortem gate) favoured the single-plan shape: one place to track, steps still
   independently mergeable.
2. **Fold the distributed lock into `Core.Scheduler` and the upstream check into `Core.HTTP`
   instead of new packages.** Rejected: it forces a `DistributedLock.*` dependency on every
   `Core.Scheduler` consumer and a health-check dependency on every `Core.HTTP` consumer, even
   those that want neither. Dedicated opt-in packages keep the dependency graph honest (mirrors
   Feature-001 Q1/Q2 reasoning on isolating new dependencies).
3. **Ship A1 idempotency as a standalone `Core.Idempotency` package.** Rejected: the middleware is
   ASP.NET-Core-coupled and belongs with the other request-pipeline middleware in `Core.Api`; the
   pluggable store keeps the Redis dependency optional without a new package.
4. **Do nothing / document-only for the whole wave.** Rejected: A2 (distributed lock) and A3
   (health checks) are genuine capability gaps every multi-instance deployment hits; a recipe
   cannot substitute for a tested, shipped primitive.

## Semver impact

**MINOR** overall (additive APIs + two new packages; recipes are PATCH/docs).

## Related

- [Production pattern adoption — wave 1](2026-06-12-production-pattern-adoption-wave-1.md)
- [HTTP client architecture](../architecture/http-client.md)
- [Delivery workflow](../architecture/delivery-workflow.md)
- [`docs/future-ideas/`](../future-ideas/README.md) — parked FI-001 / FI-002 (out of scope).

## Completion summary

Completed 2026-06-25. The temporary step folder was deleted after consolidation. The `00`
premortem gate cleared **Go with mitigations** (M1–M8) before any code began.

| # | Step | Shipped |
|---|---|---|
| 00 | Premortem gate | *Go with mitigations* (M1–M8); no `src/` code. |
| 01 | A6 — Security headers | `SecurityHeadersMiddleware` + `UseSecurityHeaders()` (`src/SolTechnology.Core.API/Security/`); CSP/`nosniff`/`Referrer-Policy`, Swagger relaxation. |
| 02 | B4 — `Recoverable` in ProblemDetails | `ApiProblemDetailsFactory` writes `extensions.recoverable` on both `FromError` (direct) and `FromException` (5xx⇒true, 4xx⇒false). |
| 03 | D1+D2 — Test helpers | `ResultAssertions` (`ShouldBeSuccess/Failure`) + `Ct.Any` matcher in `Core.Testing`. |
| 04 | A2 — Distributed lock | `IDistributedLockService` (local `SemaphoreSlim` + Redis `SET NX EX` w/ fencing) **in `Core.Cache`** — `AddLocalLock()` / `AddDistributedLock()`. **No new package** (Option B). |
| 05 | A2.2 — Medallion backends | **Superseded** by Option B → [`future-ideas/005`](../future-ideas/005-medallion-lock-backends.md). |
| 06 | A3.1 — Health endpoint | Pure `HealthReportJsonFormatter` + `MapCoreHealthChecks()` in `Core.Api`. **No foundation package.** |
| 07 | A3.2 — Data-store checks | `AddSqlHealthCheck()` (`Core.SQL`) + `AddRedisHealthCheck()` (`Core.Cache`), referencing framework-agnostic `Microsoft.Extensions.Diagnostics.HealthChecks`. |
| 08 | A3.3 — Messaging + upstream | `AddServiceBusHealthCheck()` (`Core.MessageBus`, `PeekMessageAsync` probe) + `BaseUpstreamServiceHealthCheck<TReport>` / `AddUpstreamHttpHealthCheck()` (`Core.HTTP`, cached + `TimeProvider`). |
| 09 | C1 — Deployment-slot gating | **Deferred** → [`future-ideas/003`](../future-ideas/003-deployment-slot-gating.md). |
| 10 | C2 — Leader-elected poller | **Deferred** → [`future-ideas/004`](../future-ideas/004-leader-elected-poller.md). |
| 11 | A1.1 — Idempotency store | `IIdempotencyStore` / `StoredResponse` (local + Redis) **in `Core.Cache`** — `AddLocalIdempotency()` / `AddDistributedIdempotency()`. |
| 12 | A1.2 — Idempotency middleware | **Docs recipe** in `Cache.md` (no library middleware — same call as the lock). |
| 13 | A1.3 — Redis glue package | **Removed** — the Redis store lives in `Core.Cache`. |
| 14–17 | B1 — Two-level correlation | **Removed** from scope — the single `ICorrelationIdService` (Feature-001) already propagates across HTTP / queue / jobs. |
| 18 | B2 — Recoverable-aware retry | `RetryPredicates.RecoverableOnly` + `HttpPolicyConfiguration.RetryPredicate` (`Core.HTTP`). |
| 19 | B3 — Typed call-error taxonomy | `ServiceCallErrorMapper` + `RequestBuilder.TryXxxAsync<T>()` → `Result<T>` (`Core.HTTP`). |
| 20 | A5 — Timing diagnostics | `ITimingService` + emission in `LoggingMiddleware` (`Core.Logging`), `TimeProvider`-sourced. |
| 21 | D3 — Fitness guards | `BuildHygieneGuardTests` + `TestHostContainmentGuardTests` in `tests/SolTechnology.Core.Tests`. |
| 22 | F — Document-only recipes | Per-principal rate limiting (`Api.md`), singleton→scoped correlation bridge (`Log.md`), delay-queue-vs-Hangfire note (`Hangfire.md`). |
| 23 | Publish workflow | **No-op** — zero new package IDs; everything ships via version bumps of existing packages. |

**Net surface delta:** additive APIs on `Core.Api`, `Core.Cache`, `Core.HTTP`, `Core.Logging`,
`Core.Testing`, `Core.SQL`, `Core.MessageBus` — all via **version bumps of existing packages**.
Semver impact **MINOR** as predicted, but with **zero new package IDs**.

### Preserved deviations

- **No new packages (steps 04–06, 11–13).** Both proposed packages (`Core.DistributedLock`,
  `Core.HealthChecks`) and the idempotency glue package were eliminated by in-module decisions: the
  lock and idempotency store live in `Core.Cache` (≈95 % infra overlap with the cache — same Redis,
  connection, `InstanceName` namespace), and health checks live per-module referencing the
  framework-agnostic `Microsoft.Extensions.Diagnostics.HealthChecks`. **Lesson:** prefer extending an
  existing module over minting a package when the infrastructure overlap is high.
- **Correlation (14–17) removed.** The two-level model added no value without a consumer; the single
  `ICorrelationIdService` from Feature-001 already covers HTTP / queue / job propagation.
- **`Core.Scheduler` deprecated + removed from the solution** rather than extended (steps 09–10
  deferred). The D3 build-hygiene guard then drove `Core.SQL` and `Core.MessageBus` to **remove**
  their `TreatWarningsAsErrors=false` (both compiled clean); only the deprecated `Core.Scheduler`
  remains allow-listed.


