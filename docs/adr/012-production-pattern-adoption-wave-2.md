# ADR-012: Production pattern adoption — wave 2

> **Status:** Accepted
> **Decision Date:** 2026-06-24
> **Decision Maker:** Repository maintainers
> **Stakeholders:** Consumers of `SolTechnology.Core.*` (NuGet + `sample-tale-code-apps/DreamTravel`)

---

## Context

A second production application — a multi-tenant, multi-region payments / financial-storage
service built on the same Tale-Code philosophy — was onboarded into the production-pattern
programme started by [ADR-010](010-production-pattern-adoption-programme.md). The harvest is
recorded in [`docs/production-harvest-second-app.md`](../production-harvest-second-app.md), which
catalogues each candidate with a verdict, target module, semver and effort, and records the
decisions taken on 2026-06-24.

This ADR is the **direct continuation** of the ADR-010 programme. It adopts the **accepted** items
from the wave-2 harvest as a single production-hardening effort, grouped by module and sequenced as
independent, individually-mergeable steps — exactly the shape ADR-010 used. Two of the accepted
items are **genuinely new packages** (`SolTechnology.Core.DistributedLock`,
`SolTechnology.Core.HealthChecks`); because they add new NuGet surface and new third-party
dependencies, each gets its own decision sub-section below.

### Constraints

- **Additive only.** Every item is a new API or a new extension field — no breaking change to any
  shipped public symbol. Overall semver impact is **MINOR**.
- **Tale-Code readability first.** New surface follows the module conventions in
  [`docs/ClaudeCodingGuide.md`](../ClaudeCodingGuide.md) (single `ModuleInstaller` entry point,
  options bound + `ValidateOnStart`, `Result`/`Error` at boundaries, `TimeProvider` for time).
- **Build on ADR-010, don't duplicate it.** Correlation work extends the `ICorrelationIdService`
  already shipped in ADR-010; timing diagnostics reuse `TimeProvider` (ADR-010 G1); typed HTTP
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

## Decision

Ship all accepted wave-2 items **under this single ADR** with one implementation plan, mirroring
ADR-010. Work is grouped by module and sequenced as independent steps. The two new packages each
have a dedicated decision sub-section (below). Implementation is gated by the **`00` premortem**
(authored last, executed first — [ADR-006 §5](006-implementation-plan-workflow.md)).

### What ships

| Group | Items | Module(s) | New dependency | Semver |
|---|---|---|---|---|
| API quick wins | A6 security-headers middleware · B4 surface `Recoverable` in `ProblemDetails` | `Core.Api` | none | MINOR |
| Testing quick wins | D1 `Result` assertion helpers · D2 `Ct` matcher alias | `Core.Testing` | none | MINOR |
| ~~New package~~ **→ Option B** | A2 distributed lock | **`Core.Cache`** (thin layer, not a separate package) | none (reuses existing `StackExchange.Redis`) | MINOR |
| **New package** | A3 cached upstream health-check base + JSON writer + per-module checks | **`Core.HealthChecks`** + SQL/Cache/Bus/HTTP | none (uses `Microsoft.Extensions.Diagnostics.HealthChecks`) | MINOR (new package) |
| Background | C1 deployment-slot gating · C2 leader-elected polling base | `Core.Scheduler` (→ `Core.Cache`) | none | MINOR |
| API idempotency | A1 inbound HTTP idempotency middleware + store abstraction | `Core.Api` (+ `Core.Cache` store) | none | MINOR |
| Correlation deltas | B1 two-level model · inbound+response · outbound · queue | `Core.Logging`/`Api`/`HTTP`/`MessageBus` | none | MINOR |
| HTTP enhancements | B2 recoverable-aware retry predicate · B3 typed call-error taxonomy | `Core.HTTP` | none | MINOR |
| Diagnostics | A5 per-request timing diagnostics | `Core.Logging` | none | MINOR |
| Fitness | D3 build-hygiene + test-host containment guard tests | `Core.Testing` + repo self-tests | none | MINOR (docs+tests) |
| Recipes | F per-principal rate limiting · singleton→scoped bridge (B6) · delay-queue vs Hangfire (C4) | docs only | none | PATCH (docs) |

### What does NOT ship (explicitly out of scope)

- **FI-001 outbound webhooks** (was A4) and **FI-002 priority worker pool** (was C3) — parked in
  [`docs/future-ideas/`](../future-ideas/README.md); each wants its own ADR when a consumer exists.
- **Section E "not porting"** — custom `IDateTimeProvider` (Core uses `TimeProvider`, ADR-010 G1),
  encryption-at-rest, multi-cloud broker switch (ADR-010 Q3), MACRO_CASE JSON policy, app-specific
  domain code.
- **B5** (`OperationCanceledException` response) — already handled by `Core.Api` cancellation
  logging; parity only. **D4** (primary-ctor caution) — already a Core rule (ADR-010 G7).

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

### Decision sub-section — new package `SolTechnology.Core.HealthChecks` (A3)

**Why a new package.** Core ships no health-check helpers today. A naïve upstream check is a known
production footgun — it either hammers the dependency or hangs the probe and takes the pod down. A
reusable, cached, correctly-timed base solves it once. It lives in its own package so the
`Microsoft.Extensions.Diagnostics.HealthChecks` surface and the per-module check helpers do not
leak into modules that do not want them.

**Surface.** `BaseUpstreamServiceHealthCheck<TReport>` — calls a downstream `/health`, caches the
result in memory (~30 s), applies a per-call timeout independent of the probe, deserialises a typed
report, and maps the exception taxonomy: connection failure → `Unhealthy`, timeout → `Unhealthy`,
**caller-cancellation → rethrow** (not "Unhealthy"), bad payload → `Degraded`. Plus a JSON
`ResponseWriter` for the health endpoint. Per-module checks (`Core.SQL`, `Core.Cache`/Redis,
`Core.MessageBus`, `Core.HTTP`) reference this package and contribute an `AddXxxHealthCheck()`.

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
| `Microsoft.Extensions.Diagnostics.HealthChecks` | `Core.HealthChecks` | no | Health-check abstractions + builder. Shared-framework family (`10.0.x`). |
| (existing) `Microsoft.AspNetCore.RateLimiting` | `Core.Api` (recipe only) | built-in (`net10.0`) | F recipe — no `PackageReference`. |

The new `Core.HealthChecks` package (and the glue `Core.Api.Idempotency.Redis`) add `.slnx`
`Project` entries, inherit `src/Directory.Build.props` (so `TreatWarningsAsErrors=true` applies),
and are added to `.github/workflows/publishPackages.yml`. The distributed lock does **not** add a
package — it is part of `Core.Cache` (Option B).

## Alternatives Considered

1. **One child ADR per item (012–02x), each with its own plan + premortem.** Rejected for the same
   reason ADR-010 rejected it: process overhead disproportionate to the work. The items are related
   production-hardening concerns harvested together, not independent architectural decisions. A
   blue/red review of this (see the `00` premortem gate) favoured the single-ADR shape: one
   place to track, steps still independently mergeable.
2. **Fold the distributed lock into `Core.Scheduler` and the upstream check into `Core.HTTP`
   instead of new packages.** Rejected: it forces a `DistributedLock.*` dependency on every
   `Core.Scheduler` consumer and a health-check dependency on every `Core.HTTP` consumer, even
   those that want neither. Dedicated opt-in packages keep the dependency graph honest (mirrors
   ADR-010 Q1/Q2 reasoning on isolating new dependencies).
3. **Ship A1 idempotency as a standalone `Core.Idempotency` package.** Rejected: the middleware is
   ASP.NET-Core-coupled and belongs with the other request-pipeline middleware in `Core.Api`; the
   pluggable store keeps the Redis dependency optional without a new package.
4. **Do nothing / document-only for the whole wave.** Rejected: A2 (distributed lock) and A3
   (health checks) are genuine capability gaps every multi-instance deployment hits; a recipe
   cannot substitute for a tested, shipped primitive.

## Consequences

**Positive**

- One tracked place for all wave-2 hardening; steps remain independently mergeable.
- Two long-standing gaps (distributed coordination, production-safe health checks) close with
  tested primitives instead of per-app reinvention.
- Correlation, retry, and error-shape deltas compose with ADR-010's foundation rather than
  duplicating it.
- The D3 fitness guards make several existing coding-guide rules self-enforcing.

**Negative**

- Large blast radius per ADR — wave 2 touches ~8 modules and adds 2 packages. Mitigated: steps are
  independent, additive, and opt-in; nothing changes for consumers who do not call the new APIs.
- Two new packages add NuGet surface to maintain and version. Mitigated: dependency-light, each
  with its own decision sub-section and CVE gate.
- The D3 build-hygiene guard will immediately flag existing `TreatWarningsAsErrors=false` projects
  (`Core.SQL`, `Core.Scheduler`, `Core.MessageBus`). Mitigated: the guard ships with an explicit,
  commented allow-list documenting each laggard (or they are fixed in the same step).

**Semver impact:** **MINOR** overall (additive APIs + two new packages; recipes are PATCH/docs).

## Related

- [ADR-010](010-production-pattern-adoption-programme.md) — wave-1 programme this continues.
- [ADR-005](005-http-production-defaults.md) — HTTP resilience that B2/B3 extend.
- [ADR-006](006-implementation-plan-workflow.md) — plan-folder layout this plan follows.
- [`docs/production-harvest-second-app.md`](../production-harvest-second-app.md) — authoritative
  harvest + decisions.
- [`docs/future-ideas/`](../future-ideas/README.md) — parked FI-001 / FI-002 (out of scope).

## Implementation plan

Tracked in [`012-production-pattern-adoption-wave-2/summary.md`](012-production-pattern-adoption-wave-2/summary.md).
Implementation is **blocked** until the **`00` premortem gate** returns *Go* or *Go with
mitigations* — it is authored last but runs first ([ADR-006 §5](006-implementation-plan-workflow.md)).

