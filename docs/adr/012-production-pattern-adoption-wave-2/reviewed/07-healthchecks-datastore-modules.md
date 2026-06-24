---
adr: 012-production-pattern-adoption-wave-2
step: 07 of 24
status: reviewed
---

# Step 07: A3.2 — Data-store health checks (`Core.SQL` + `Core.Cache`)

## Summary
Contribute health checks for the two data-store modules: a SQL reachability check in `Core.SQL` and
a distributed-cache (Redis) reachability check in `Core.Cache`, each exposed as a chainable
`AddXxxHealthCheck()` on the `Core.HealthChecks` builder from step 06. Grouped because both are
"can I reach my data store" checks following the same shape; separate from messaging/HTTP (step 08)
to keep each PR to two modules.

> **ASP.NET-free (Blocker 1 resolution).** The foundation (`Core.HealthChecks`, step 06) references
> only `Microsoft.Extensions.Diagnostics.HealthChecks` (framework-agnostic). Referencing it here adds
> `IHealthCheck` to `Core.SQL` and `Core.Cache` **without** any ASP.NET `FrameworkReference` to
> `Microsoft.AspNetCore.App`. Neither module gains an ASP.NET dependency.

## Affected components
- `src/SolTechnology.Core.SQL/HealthChecks/SqlHealthCheck.cs` + `…/SqlHealthCheckExtensions.cs` —
  `IHealthCheck` running a cheap probe (e.g. `SELECT 1`) via the existing connection seam
  (`ISqlConnectionStringProvider`, ADR-010 S1) and the module's existing `Microsoft.Data.SqlClient`;
  `AddSqlHealthCheck(this IHealthChecksBuilder, …)`.
- `src/SolTechnology.Core.SQL/SolTechnology.Core.SQL.csproj` — add `ProjectReference` to
  `Core.HealthChecks` (a non-ASP.NET reference — see the note above).
- `src/SolTechnology.Core.Cache/HealthChecks/RedisHealthCheck.cs` +
  `…/RedisHealthCheckExtensions.cs` — `IHealthCheck` pinging the distributed cache
  (`IDistributedCache`/Redis from ADR-010 C1, via the module's existing
  `Microsoft.Extensions.Caching.StackExchangeRedis`); `AddRedisHealthCheck(this IHealthChecksBuilder, …)`.
- `src/SolTechnology.Core.Cache/SolTechnology.Core.Cache.csproj` — add `ProjectReference` to
  `Core.HealthChecks` (non-ASP.NET).
- `tests/SolTechnology.Core.SQL.Tests/` — **existing** project: negative SQL test (unreachable → `Unhealthy`).
- `tests/SolTechnology.Core.Cache.Tests/` — **new** NUnit test project (none exists today): negative
  Redis test (unreachable → `Unhealthy`). Wire it into `SolTechnology.Core.slnx` under `/Tests/`.
  (CLAUDE.md §1 new-test-folder confirmation **GIVEN** for this wave.)
- `docs/SQL.md` + `docs/Cache.md` — short "Health check" subsections.

## Details
- Each check maps store-unreachable → `Unhealthy`, transient/slow → consider `Degraded` only if the
  module already distinguishes it; keep it simple (reachable = `Healthy`, else `Unhealthy`).
- Reuse the foundation's per-call timeout pattern (link a CTS with the probe `ct`); these are local
  data-store pings, so they may not need the full cached-upstream base — use the base where it fits,
  otherwise a plain `IHealthCheck` that still honours **caller-cancellation rethrow** and a per-call
  timeout.
- **`Core.SQL` `TreatWarningsAsErrors=false`** today (`SolTechnology.Core.SQL.csproj`, verified) —
  adding a project reference must not introduce new warnings that would surprise the step-21
  build-hygiene guard; keep the addition clean.
- Each `AddXxxHealthCheck()` is opt-in and chains onto `AddCoreHealthChecks()`; do not auto-register
  from the module installers.

## Acceptance criteria
- `AddSqlHealthCheck()` and `AddRedisHealthCheck()` chain onto the `Core.HealthChecks` builder.
- An unreachable SQL/Redis store reports `Unhealthy`; a reachable one reports `Healthy`.
- Caller-cancellation rethrows (does not report `Unhealthy`).
- `Core.SQL` and `Core.Cache` reference `Core.HealthChecks` with **no** ASP.NET `FrameworkReference`;
  both still build green.
- The new `tests/SolTechnology.Core.Cache.Tests` project is in `SolTechnology.Core.slnx` (`/Tests/`)
  alongside the existing `tests/SolTechnology.Core.SQL.Tests`.
- `docs/SQL.md` and `docs/Cache.md` document the new checks.

## Open questions
- Whether the SQL/Redis checks derive from `BaseUpstreamServiceHealthCheck<TReport>` (they are not
  strictly "upstream HTTP /health" calls) or are plain `IHealthCheck`s sharing the timeout/cancel
  helpers. Recommend plain `IHealthCheck` reusing shared helpers; flag for the reviewer.

