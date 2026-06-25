# ADR-012: Production pattern adoption — wave 2 — Implementation Summary

Tracking the implementation steps for [ADR-012](../012-production-pattern-adoption-wave-2.md).
Steps are ordered by the harvest's prioritised roadmap (quick `Core.Api`/`Core.Testing` wins →
distributed lock (in `Core.Cache`, Option B) → health checks (per-module, no foundation package) →
background → idempotency (incl. the one new package, the `Core.Api.Idempotency.Redis` glue package) →
correlation/HTTP deltas → diagnostics → fitness → recipes). Each step is independently mergeable.

Implementation is **blocked** until the **`00` premortem gate** returns *Go* or *Go with mitigations*.
The gate is authored last but **numbered first** so it runs before any code
([ADR-006 §5](../006-implementation-plan-workflow.md)).

> **Gate cleared 2026-06-24 — verdict: GO WITH MITIGATIONS.** Required mitigations **M1–M8** are
> recorded in [`done/00-run-premortem.md`](done/00-run-premortem.md) and folded into the owning step
> files (11, 12, 13, 06, 14 carry a `## Premortem mitigations (required)` section; 04/05/10/21 were
> already covered by their acceptance criteria). Steps `01..23` may now proceed.

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 00 | Run premortem — **gate, cleared (Go w/ mitigations)** | [`done/00-run-premortem.md`](done/00-run-premortem.md) | ✅ done |
| 01 | A6 — Security-headers middleware (`Core.Api`) | [`done/01-api-security-headers-middleware.md`](done/01-api-security-headers-middleware.md) | ✅ done |
| 02 | B4 — Surface `Recoverable` in API `ProblemDetails` (`Core.Api`) | [`done/02-api-problemdetails-recoverable.md`](done/02-api-problemdetails-recoverable.md) | ✅ done |
| 03 | D1+D2 — `Result` assertions + `Ct` matcher (`Core.Testing`) | [`done/03-testing-result-assertions-and-ct-matcher.md`](done/03-testing-result-assertions-and-ct-matcher.md) | ✅ done |
| 04 | A2.1 — `Core.DistributedLock` → **implemented in `Core.Cache`** (Option B) | [`done/04-distributedlock-package-and-abstraction.md`](done/04-distributedlock-package-and-abstraction.md) | ✅ done |
| 05 | ~~A2.2 — Medallion.Threading backends~~ — **superseded by Option B** (lock lives in Core.Cache, no Medallion) | [`reviewed/05-distributedlock-medallion-backends.md`](reviewed/05-distributedlock-medallion-backends.md) | ~~superseded~~ |
| 06 | A3.1 — Health endpoint (`Core.Api`: JSON formatter + `MapCoreHealthChecks`) — **no foundation package** | [`done/06-healthchecks-api-endpoint.md`](done/06-healthchecks-api-endpoint.md) | ✅ done |
| 07 | A3.2 — Data-store health checks (`Core.SQL` + `Core.Cache`, ref framework pkg directly) | [`done/07-healthchecks-datastore-modules.md`](done/07-healthchecks-datastore-modules.md) | ✅ done |
| 08 | A3.3 — Messaging + upstream health checks (`Core.MessageBus` + `Core.HTTP`; base lives in `Core.HTTP`) | [`done/08-healthchecks-messaging-and-http-modules.md`](done/08-healthchecks-messaging-and-http-modules.md) | ✅ done |
| 09 | C1 — Deployment-slot gating (`Core.Scheduler`) | [`reviewed/09-scheduler-deployment-slot-gating.md`](reviewed/09-scheduler-deployment-slot-gating.md) | 🔍 reviewed |
| 10 | C2 — Leader-elected polling service base (`Core.Scheduler`) | [`reviewed/10-scheduler-leader-elected-poller.md`](reviewed/10-scheduler-leader-elected-poller.md) | 🔍 reviewed |
| 11 | A1.1 — Idempotency store abstraction + in-memory + selector (`Core.Api`) | [`to-do/11-idempotency-store-abstraction.md`](to-do/11-idempotency-store-abstraction.md) | ⬜ to-do |
| 12 | A1.2 — Idempotency middleware + options + Add/Use + logging (`Core.Api`) | [`to-do/12-idempotency-middleware-and-options.md`](to-do/12-idempotency-middleware-and-options.md) | ⬜ to-do |
| 13 | A1.3 — Redis idempotency store (new glue package `Core.Api.Idempotency.Redis`) | [`reviewed/13-idempotency-redis-store.md`](reviewed/13-idempotency-redis-store.md) | 🔍 reviewed |
| 14 | B1.1 — Two-level correlation model (`Core.Logging`) | [`to-do/14-correlation-two-level-model.md`](to-do/14-correlation-two-level-model.md) | ⬜ to-do |
| 15 | B1.2 — Inbound extraction + response enrichment (`Core.Api`) | [`to-do/15-correlation-api-inbound-and-response.md`](to-do/15-correlation-api-inbound-and-response.md) | ⬜ to-do |
| 16 | B1.3 — Outbound `AddCorrelation` helper (`Core.HTTP`) | [`reviewed/16-correlation-http-outbound.md`](reviewed/16-correlation-http-outbound.md) | 🔍 reviewed |
| 17 | B1.4 — Queue correlation propagation (`Core.MessageBus`) | [`to-do/17-correlation-messagebus-queue.md`](to-do/17-correlation-messagebus-queue.md) | ⬜ to-do |
| 18 | B2 — Recoverable-aware retry predicate (`Core.HTTP`) | [`reviewed/18-http-recoverable-retry-predicate.md`](reviewed/18-http-recoverable-retry-predicate.md) | 🔍 reviewed |
| 19 | B3 — Typed service-call error taxonomy (`Core.HTTP`) | [`reviewed/19-http-typed-call-error-taxonomy.md`](reviewed/19-http-typed-call-error-taxonomy.md) | 🔍 reviewed |
| 20 | A5 — Per-request timing diagnostics (`Core.Logging`) | [`to-do/20-logging-timing-diagnostics.md`](to-do/20-logging-timing-diagnostics.md) | ⬜ to-do |
| 21 | D3 — Architecture fitness guard tests + recipe (`Core.Testing`) | [`to-do/21-testing-architecture-fitness-guards.md`](to-do/21-testing-architecture-fitness-guards.md) | ⬜ to-do |
| 22 | F — Document-only recipes (rate limiting · singleton→scoped · delay-queue) | [`to-do/22-document-only-recipes.md`](to-do/22-document-only-recipes.md) | ⬜ to-do |
| 23 | Wire new packages into the publish workflow | [`reviewed/23-publish-workflow-new-packages.md`](reviewed/23-publish-workflow-new-packages.md) | 🔍 reviewed |

Status values: `⬜ to-do` / `🔍 reviewed` / `✅ done`. Link in each row points to the step's
current location (`to-do/` / `reviewed/` / `done/`).

## Dependency notes

- **Steps 04–05 (DistributedLock)** — step 04 shipped as thin lock layer in `Core.Cache` (Option B);
  step 05 (Medallion) is **superseded**. Step 10 (leader-elected poller) depends on `Core.Cache`
  (which now contains `IDistributedLockService`).
- **Steps 06–08 (HealthChecks)** — **no foundation package** (2026-06-25 decision). Step 06 ships the
  ASP.NET endpoint in `Core.Api` (JSON formatter + `MapCoreHealthChecks`); steps 07–08 ship per-module
  checks (`Core.SQL`/`Core.Cache`/`Core.MessageBus`/`Core.HTTP`) that reference the framework-agnostic
  `Microsoft.Extensions.Diagnostics.HealthChecks` **directly**. The cached upstream base lives in
  `Core.HTTP` (step 08). All three steps are **independent** — the endpoint renders whatever checks
  are registered.
- **Step 11 (store abstraction)** must land before **steps 12–13** (middleware + Redis store
  consume the **public** `IIdempotencyStore` / `StoredResponse`). Step 13 ships them in the separate
  glue package **`SolTechnology.Core.Api.Idempotency.Redis`** (references `Core.Api` + `Core.Cache`),
  so `Core.Api` stays Redis-free.
- **Step 14 (correlation model)** must land before **steps 15–17** (each module consumes the model).
- **Step 23 (publish workflow)** must land after **step 13** (the only new package this wave —
  `Core.Api.Idempotency.Redis`; `Core.DistributedLock` and `Core.HealthChecks` are no longer separate
  packages per the Option-B / no-foundation decisions).
- **Step 00 (premortem)** is the gate — it is authored last but **runs first**; implementation of any
  `01..23` step is blocked until it returns *Go* or *Go with mitigations*
  ([ADR-006 §5](../006-implementation-plan-workflow.md)).

