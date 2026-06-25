---
adr: 012-production-pattern-adoption-wave-2
step: 07 of 24
status: done
---

# Step 07: A3.2 — Data-store health checks (`Core.SQL` + `Core.Cache`)

## Summary
Contribute connectivity health checks for the two data-store modules: a SQL reachability check in
`Core.SQL` and a distributed-cache (Redis) reachability check in `Core.Cache`. Each is a plain
`IHealthCheck` exposed as a chainable `AddXxxHealthCheck()` on the **framework**
`IHealthChecksBuilder` (from `AddHealthChecks()`). The check lives **next to the implementation** it
probes — no foundation package.

> **No foundation package (2026-06-25 decision).** Each module references the framework-agnostic
> `Microsoft.Extensions.Diagnostics.HealthChecks` (`IHealthCheck`, `IHealthChecksBuilder`,
> `HealthCheckResult`) **directly** — **not** a `Core.HealthChecks` package and **not** the ASP.NET
> `Microsoft.AspNetCore.Diagnostics.HealthChecks`. No `FrameworkReference` to
> `Microsoft.AspNetCore.App`. The ASP.NET health **endpoint** lives only in `Core.Api` (step 06).

## Affected components
- `src/SolTechnology.Core.SQL/HealthChecks/SqlHealthCheck.cs` + `…/SqlHealthCheckExtensions.cs` —
  `IHealthCheck` running a cheap probe (`SELECT 1`) via the existing connection seam
  (`ISqlConnectionStringProvider`, ADR-010 S1) and the module's existing `Microsoft.Data.SqlClient`;
  `AddSqlHealthCheck(this IHealthChecksBuilder, …)`.
- `src/SolTechnology.Core.SQL/SolTechnology.Core.SQL.csproj` — add `PackageReference`
  `Microsoft.Extensions.Diagnostics.HealthChecks` (`10.0.x`). **Non-ASP.NET.**
- `src/SolTechnology.Core.Cache/HealthChecks/RedisHealthCheck.cs` +
  `…/RedisHealthCheckExtensions.cs` — `IHealthCheck` pinging Redis via the `IConnectionMultiplexer`
  already registered by `AddDistributedCache` (ADR-010 C1 + the Option-B lock work in step 04);
  `AddRedisHealthCheck(this IHealthChecksBuilder, …)`.
- `src/SolTechnology.Core.Cache/SolTechnology.Core.Cache.csproj` — add `PackageReference`
  `Microsoft.Extensions.Diagnostics.HealthChecks` (`10.0.x`). **Non-ASP.NET.**
- `.github/skills/package-management/references/canonical-versions.md` — **add a row** for
  `Microsoft.Extensions.Diagnostics.HealthChecks` pinned at `10.0.x` (first introduced here; shared
  `Microsoft.Extensions.*` family, same `10.0.x` minor). **Step 08 reuses it** (MessageBus + HTTP);
  step 06's `Core.Api` gets the same abstractions transitively via the ASP.NET shared framework, so
  it needs no explicit `PackageReference`.
- `tests/SolTechnology.Core.SQL.Tests/` — **existing** project: negative SQL test (unreachable → `Unhealthy`).
- `tests/SolTechnology.Core.Cache.Tests/` — **new** NUnit test project (none exists today): negative
  Redis test (unreachable → `Unhealthy`). Wire it into `SolTechnology.Core.slnx` under `/Tests/`.
  (CLAUDE.md §1 new-test-folder confirmation **GIVEN** for this wave.)
- `docs/SQL.md` + `docs/Cache.md` — short "Health check" subsections (live next to each module's doc).

## Details
- Each check maps store-unreachable → `Unhealthy`, reachable → `Healthy`. Keep it simple — these are
  local connectivity pings, not typed-report upstreams, so no `Degraded` branch.
- Each check honours **caller-cancellation rethrow** and a per-call timeout (link a CTS with the
  probe `ct`). These are small (~15 lines) and self-contained per module — there is no shared
  foundation to host a common helper, and that is fine: the pattern is trivial and each module owns
  its own probe.
- **`Core.SQL` `TreatWarningsAsErrors=false`** today (verified) — the added `PackageReference` must
  not introduce new warnings that would surprise the step-21 build-hygiene guard; keep it clean.
- Each `AddXxxHealthCheck()` is opt-in and chains onto the framework `AddHealthChecks()` builder; do
  **not** auto-register from the module installers.

## Acceptance criteria
- `AddSqlHealthCheck()` and `AddRedisHealthCheck()` chain onto the framework `IHealthChecksBuilder`.
- An unreachable SQL/Redis store reports `Unhealthy`; a reachable one reports `Healthy`.
- Caller-cancellation rethrows (does not report `Unhealthy`).
- `Core.SQL` and `Core.Cache` reference `Microsoft.Extensions.Diagnostics.HealthChecks` with **no**
  ASP.NET `FrameworkReference`; both still build green.
- The new `tests/SolTechnology.Core.Cache.Tests` project is in `SolTechnology.Core.slnx` (`/Tests/`)
  alongside the existing `tests/SolTechnology.Core.SQL.Tests`.
- `canonical-versions.md` has the `Microsoft.Extensions.Diagnostics.HealthChecks` `10.0.x` row.
- `docs/SQL.md` and `docs/Cache.md` document the new checks.

## Open questions
- none — the per-module placement is resolved (checks live in `Core.SQL` / `Core.Cache`, referencing
  the framework package directly).

