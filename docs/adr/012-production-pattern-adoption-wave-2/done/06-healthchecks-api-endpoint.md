---
adr: 012-production-pattern-adoption-wave-2
step: 06 of 24
status: done
---

# Step 06: A3.1 — Health-check endpoint in `Core.Api` (JSON formatter + `MapCoreHealthChecks`)

## Summary
Ship the **health endpoint** in `Core.Api` (which already carries ASP.NET surface): a
`HealthReportJsonFormatter` that renders a `HealthReport` as JSON, and a `MapCoreHealthChecks(path)`
extension that maps the ASP.NET health endpoint using that formatter. **No new package, no foundation
package, no `AddCoreHealthChecks()` wrapper** — consumers call the framework `AddHealthChecks()`
directly and chain the per-module checks from steps 07–08 onto it.

> **Decision (2026-06-25): health checks live next to their implementation, no foundation package.**
> The original plan introduced a `SolTechnology.Core.HealthChecks` foundation package that every
> module would reference. The maintainer chose to drop it (same reasoning as the DistributedLock
> Option-B decision): per-module checks reference the framework-agnostic
> `Microsoft.Extensions.Diagnostics.HealthChecks` **directly** (steps 07–08), and the only ASP.NET
> piece — the endpoint — lives in `Core.Api`, which is already an ASP.NET package. This is a
> **stronger** Blocker-1 resolution: there is no shared foundation that could ever drag ASP.NET into
> `Core.SQL` / `Core.Cache`.

## Affected components
- `src/SolTechnology.Core.API/HealthChecks/HealthReportJsonFormatter.cs` — pure formatter:
  `HealthReport` → JSON (`string` + a `Stream` / `Utf8JsonWriter` overload). Emits overall status +
  per-check name/status/description/duration.
- `src/SolTechnology.Core.API/HealthChecks/HealthChecksEndpointExtensions.cs` —
  `MapCoreHealthChecks(this IEndpointRouteBuilder, string path = "/health")` that maps the ASP.NET
  health endpoint with `HealthCheckOptions.ResponseWriter` wired to the formatter. `Core.Api` already
  references the ASP.NET shared framework, so the `HttpContext`-shaped writer is natural here.
- `docs/Api.md` — new "Health endpoint" subsection (registration + JSON shape + the
  `AddHealthChecks().AddXxxHealthCheck()` composition from steps 07–08).
- `tests/SolTechnology.Core.API.Tests/` — **existing** project: **pure formatter** unit tests
  (multi-check `HealthReport` → expected JSON; status-code mapping). The formatter has no
  `HttpContext` dependency, so it is unit-testable here without a test host.
- `sample-tale-code-apps/DreamTravel/tests/Component/` — the **endpoint integration test**
  (`MapCoreHealthChecks` maps, returns 200/503 with the JSON body) goes here, consistent with the
  step-01 decision: `Core.Api.Tests` stays host-free (no `Microsoft.AspNetCore.TestHost`); host-level
  middleware/endpoint behaviour is verified against the real DreamTravel API host.

## Details
- **`MapCoreHealthChecks`** is the only ASP.NET-coupled piece in the whole health-check feature. It
  lives in `Core.Api` because that package already depends on the ASP.NET shared framework — no new
  ASP.NET surface is introduced anywhere else.
- **Formatter is pure** (`HealthReport` → JSON, no `HttpContext`), so it is independently testable and
  could be reused by a non-ASP.NET host; the endpoint extension is the thin ASP.NET adapter.
- **No `AddCoreHealthChecks()` wrapper.** Consumers compose with the framework directly:
  ```csharp
  builder.Services.AddHealthChecks()
      .AddSqlHealthCheck()            // Core.SQL (step 07)
      .AddRedisHealthCheck()          // Core.Cache (step 07)
      .AddServiceBusHealthCheck()     // Core.MessageBus (step 08)
      .AddUpstreamHttpHealthCheck<TReport>(...);  // Core.HTTP (step 08)

  app.MapCoreHealthChecks("/health"); // Core.Api (this step)
  ```
- This step is **independent** of steps 07–08: the endpoint renders whatever checks are registered,
  including none. Steps 07–08 add checks; this step renders them.

## Acceptance criteria
- `HealthReportJsonFormatter` emits valid JSON for a multi-check `HealthReport` (overall status +
  per-check detail), with **no `HttpContext` dependency**.
- `MapCoreHealthChecks(path)` maps an ASP.NET health endpoint that returns the formatter's JSON with
  the correct status code (200 Healthy/Degraded, 503 Unhealthy).
- No new package and no new test project are created; the formatter lands in `Core.Api` +
  `Core.Api.Tests`, and the endpoint integration test lands in `DreamTravel.Component`.
- No `AddCoreHealthChecks()` wrapper exists — consumers use the framework `AddHealthChecks()`.
- `docs/Api.md` documents the endpoint, the JSON shape, and the per-module composition.

## Open questions
- none — the foundation-package question is resolved (no foundation package; endpoint in `Core.Api`,
  per-module checks reference `Microsoft.Extensions.Diagnostics.HealthChecks` directly).

## Premortem mitigations (required — added by the `00` gate, 2026-06-24; updated 2026-06-25)
- **M4 (dependency hygiene, M):** make "the ASP.NET health endpoint lives **only** in `Core.Api`" an
  **executable assertion** — the step-21 build-hygiene guard asserts that `Core.SQL`, `Core.Cache`,
  `Core.MessageBus`, and `Core.HTTP` have **no** `FrameworkReference` to `Microsoft.AspNetCore.App`.
  With the foundation package gone, this is the executable form of Blocker-1: nothing but `Core.Api`
  may carry ASP.NET health surface.
- **M3 (correctness, H):** the caller-cancellation-rethrow test for the upstream base lands in step 08
  (the base now lives in `Core.HTTP`) — a cancelled probe must **not** be reported as `Unhealthy`.

