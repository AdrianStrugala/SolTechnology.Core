---
adr: 012-production-pattern-adoption-wave-2
step: 06 of 24
status: reviewed
---

# Step 06: A3.1 — `Core.HealthChecks` foundation: cached upstream base + JSON formatter (new package)

## Summary
Create the new `SolTechnology.Core.HealthChecks` package with the production-safe foundation: a
`BaseUpstreamServiceHealthCheck<TReport>` that caches its result (~30 s), applies a per-call timeout
independent of the probe, deserialises a typed health report, and maps a careful exception taxonomy;
plus a **pure `HealthReport`→JSON formatter** for the health endpoint. Per-module checks land in
steps 07–08. New-package decision sub-section: see
[ADR-012](../../012-production-pattern-adoption-wave-2.md).

> **New top-level `src/` folder confirmation: GIVEN.** The maintainer approved
> `src/SolTechnology.Core.HealthChecks/` (and its `tests/` counterpart) per CLAUDE.md §1.
>
> **Foundation stays ASP.NET-free (Blocker 1 resolution).** This package references
> `Microsoft.Extensions.Diagnostics.HealthChecks` (the framework-agnostic abstractions:
> `IHealthCheck`, `HealthReport`, `HealthCheckResult`), **not** the ASP.NET
> `Microsoft.AspNetCore.Diagnostics.HealthChecks`. No `FrameworkReference` to
> `Microsoft.AspNetCore.App`. This keeps the per-module checks in steps 07–08 (`Core.SQL`,
> `Core.Cache`, `Core.MessageBus`, `Core.HTTP`) ASP.NET-free too.

## Affected components
- `src/SolTechnology.Core.HealthChecks/SolTechnology.Core.HealthChecks.csproj` — new project
  (inherits `src/Directory.Build.props`; `Version` `0.1.0`; metadata mirroring sibling `.csproj`;
  `PackageReference` `Microsoft.Extensions.Diagnostics.HealthChecks` pinned at `10.0.x`).
- `SolTechnology.Core.slnx` — add the `<Project>` entry under `/src/`.
- `src/SolTechnology.Core.HealthChecks/BaseUpstreamServiceHealthCheck.cs` — abstract cached upstream
  check (`IHealthCheck`).
- `src/SolTechnology.Core.HealthChecks/HealthReportJsonFormatter.cs` — **pure** formatter:
  `HealthReport` → JSON `string` (and a `Stream` / `Utf8JsonWriter` overload). **No `HttpContext`, no
  ASP.NET types** — it operates only on `HealthReport`.
- `src/SolTechnology.Core.HealthChecks/HealthChecksOptions.cs` — cache TTL + per-call timeout
  defaults (`TimeProvider`-sourced cache expiry per ADR-010 G1).
- `src/SolTechnology.Core.HealthChecks/ModuleInstaller.cs` — registration helpers
  (`AddCoreHealthChecks()` returning an `IHealthChecksBuilder` to chain module checks onto).
- `.github/skills/package-management/references/canonical-versions.md` — **add a row** for
  `Microsoft.Extensions.Diagnostics.HealthChecks` pinned at `10.0.x`, introduced by
  `SolTechnology.Core.HealthChecks` (shared-framework family — keep on the same `10.0.x` minor as the
  other `Microsoft.Extensions.*` references).
- `docs/HealthChecks.md` — new module doc, including a short **host snippet** showing how to wire the
  formatter as the ASP.NET `HealthCheckOptions.ResponseWriter` (see the writer note below).
- `tests/SolTechnology.Core.HealthChecks.Tests/` — **new** NUnit test project (the package is new).
  Wire it into `SolTechnology.Core.slnx` under `/Tests/`. (CLAUDE.md §1 new-test-folder confirmation
  **GIVEN** for this wave.) Taxonomy + caching + timeout + formatter tests.

## Details
- **Exception taxonomy (acceptance-critical, the whole point of the base):**
  - connection failure → `Unhealthy`
  - timeout (the check's own per-call timeout) → `Unhealthy`
  - **caller cancellation (the probe's `CancellationToken`) → rethrow** — a cancelled probe is not
    an Unhealthy dependency.
  - bad/undeserialisable payload → `Degraded`
- **Caching:** memoise the last result for ~30 s (configurable) so a frequently-scraped probe does
  not hammer the upstream. Source the clock from `TimeProvider` (testable; ADR-010 G1) — do **not**
  use `DateTime.UtcNow`.
- **Per-call timeout:** every upstream call gets its own timeout independent of the probe deadline
  (link a CTS: the check's timeout + the caller's `ct`). This prevents a hung upstream from hanging
  the probe and taking the pod down.
- **`TReport`:** the typed health report deserialised from the downstream `/health` body. Keep the
  base generic so each module supplies its own report shape.
- **JSON writer — pure formatter, not an ASP.NET `ResponseWriter` (Blocker 1 resolution):** ship
  `HealthReportJsonFormatter` that takes a `HealthReport` and emits status + per-check detail as JSON
  to a `string`/`Stream`. It has **no `HttpContext` dependency**, so the foundation stays ASP.NET-free.
  The `HttpContext`-shaped `HealthCheckOptions.ResponseWriter`
  (`Func<HttpContext, HealthReport, Task>`) is **not** shipped in this package — it is a one-line host
  adapter that calls the formatter (`ctx.Response.WriteAsync(HealthReportJsonFormatter.Format(report))`),
  documented as a **host snippet** in `docs/HealthChecks.md`. (It MAY later be added to `Core.Api` as a
  convenience extension, since `Core.Api` already carries ASP.NET surface — but that is explicitly
  **not** part of this foundation step.)
- **DI:** `AddCoreHealthChecks()` wraps the framework `AddHealthChecks()` and returns the builder so
  module steps (07–08) can chain `.AddSqlHealthCheck()` etc. Bind options with `ValidateOnStart()`.
- Keep the foundation free of any specific module reference — SQL/Cache/Bus/HTTP checks reference
  *this* package, not the reverse.

## Acceptance criteria
- `BaseUpstreamServiceHealthCheck<TReport>` maps the four taxonomy cases exactly as above, with
  caller-cancellation rethrown (verified by a test that cancels the probe token).
- Results are cached for the configured TTL using `TimeProvider`; a second call within the window
  does not re-hit the upstream.
- Each upstream call carries its own timeout independent of the probe `ct`.
- `HealthReportJsonFormatter` emits valid JSON for a multi-check `HealthReport` with **no ASP.NET
  dependency**; the `HttpContext` adapter exists only as a documented host snippet.
- The package references `Microsoft.Extensions.Diagnostics.HealthChecks` (`10.0.x`, recorded in
  `canonical-versions.md`) and **no** `Microsoft.AspNetCore.App` `FrameworkReference`.
- `SolTechnology.Core.HealthChecks` builds under `TreatWarningsAsErrors=true` and is in `.slnx`
  (`/src/`), with `tests/SolTechnology.Core.HealthChecks.Tests` in `/Tests/`.
- `docs/HealthChecks.md` documents the base + formatter + the host `ResponseWriter` snippet + the two
  guard-rails.

## Open questions
- Whether to ship the service-to-service **aggregate** check (loops configured remotes, ~10 s cache,
  reports first failure) in this foundation step or defer it. Recommend deferring — it is not needed
  by the per-module checks and can be a follow-up; note the deferral.

## Premortem mitigations (required — added by the `00` gate, 2026-06-24)
- **M4 (dependency hygiene, M):** make "the foundation `.csproj` has **no** `FrameworkReference`
  `Microsoft.AspNetCore.App`" an **executable assertion** (csproj-walking test, or fold into the
  step-21 build-hygiene guard) rather than a manual acceptance line. This is the executable form of
  the Blocker-1 fix — without it, a later edit can silently re-introduce ASP.NET into the foundation
  and, transitively, into `Core.SQL` / `Core.Cache` (steps 07–08).
- **M3 (correctness, H):** the caller-cancellation-rethrow test is acceptance-critical — a cancelled
  probe (deploy/shutdown) must **not** be reported as `Unhealthy`, or orchestrators cycle healthy
  pods.

