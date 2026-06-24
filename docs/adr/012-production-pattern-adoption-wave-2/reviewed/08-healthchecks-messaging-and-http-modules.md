---
adr: 012-production-pattern-adoption-wave-2
step: 08 of 24
status: reviewed
---

# Step 08: A3.3 — Messaging + upstream health checks (`Core.MessageBus` + `Core.HTTP`)

## Summary
Contribute the remaining per-module health checks: a Service Bus broker check in `Core.MessageBus`
and a typed-client upstream check in `Core.HTTP` built on the cached `BaseUpstreamServiceHealthCheck`
from step 06. Grouped because both probe an external dependency (broker / HTTP upstream); separate
from the data-store checks (step 07).

> **ASP.NET-free (Blocker 1 resolution).** The foundation (`Core.HealthChecks`, step 06) is
> ASP.NET-free, so `Core.MessageBus` and `Core.HTTP` reference it and add `IHealthCheck`
> implementations **without** an ASP.NET `FrameworkReference` to `Microsoft.AspNetCore.App`.

## Affected components
- `src/SolTechnology.Core.MessageBus/HealthChecks/ServiceBusHealthCheck.cs` +
  `…Extensions.cs` — `IHealthCheck` probing broker reachability (e.g. management/peek of the
  configured entity) via the existing `Azure.Messaging.ServiceBus` client;
  `AddServiceBusHealthCheck(this IHealthChecksBuilder, …)`.
- `src/SolTechnology.Core.MessageBus/SolTechnology.Core.MessageBus.csproj` — `ProjectReference` to
  `Core.HealthChecks` (non-ASP.NET).
- `src/SolTechnology.Core.HTTP/HealthChecks/UpstreamHttpHealthCheck.cs` + `…Extensions.cs` — a
  concrete `BaseUpstreamServiceHealthCheck<TReport>` wiring for a registered typed client's
  `/health`; `AddUpstreamHttpHealthCheck<TReport>(this IHealthChecksBuilder, …)`.
- `src/SolTechnology.Core.HTTP/SolTechnology.Core.HTTP.csproj` — `ProjectReference` to
  `Core.HealthChecks` (non-ASP.NET).
- `tests/SolTechnology.Core.MessageBus.Tests/` + `tests/SolTechnology.Core.HTTP.Tests/` — **existing**
  projects: negative tests (unreachable broker / upstream → `Unhealthy`; bad payload → `Degraded`;
  cancel → rethrow).
- `docs/Bus.md` + `docs/Clients.md` — short "Health check" subsections. (**Note:** `docs/HTTP.md`
  does not exist in this repo — the HTTP module is documented in `docs/Clients.md`.)

## Details
- **MessageBus check:** broker-unreachable → `Unhealthy`. Use a lightweight liveness probe; do not
  consume real messages. `Core.MessageBus` has `TreatWarningsAsErrors=false` today
  (`SolTechnology.Core.MessageBus.csproj`) — keep the addition warning-clean regardless.
- **HTTP check:** the natural first consumer of `BaseUpstreamServiceHealthCheck<TReport>` — it calls
  a downstream `/health`, caches ~30 s, times out per call, deserialises `TReport`, and maps the
  taxonomy (connection→`Unhealthy`, timeout→`Unhealthy`, cancel→rethrow, bad payload→`Degraded`).
  This validates the foundation end-to-end.
- Both checks are opt-in `AddXxxHealthCheck()` chained onto `AddCoreHealthChecks()`.
- Reuse `Core.HTTP`'s `WireMock.Net` testing companion for the upstream-check tests where an HTTP
  fake is needed (per `package-management`).

## Acceptance criteria
- `AddServiceBusHealthCheck()` and `AddUpstreamHttpHealthCheck<TReport>()` chain onto the
  `Core.HealthChecks` builder.
- The HTTP check exercises the full `BaseUpstreamServiceHealthCheck` taxonomy (connection/timeout →
  `Unhealthy`, cancel → rethrow, bad payload → `Degraded`) with caching.
- The Service Bus check reports `Unhealthy` when the broker is unreachable.
- `Core.MessageBus` and `Core.HTTP` reference `Core.HealthChecks` with **no** ASP.NET
  `FrameworkReference`; both build green.
- `docs/Bus.md` and `docs/Clients.md` document the new checks.

## Open questions
- Service Bus liveness probe shape (management call vs receiver peek) — pick the cheapest that does
  not consume messages; flag the choice for the reviewer.

