---
adr: 012-production-pattern-adoption-wave-2
step: 08 of 24
status: done
---

# Step 08: A3.3 — Messaging + upstream health checks (`Core.MessageBus` + `Core.HTTP`)

## Summary
Contribute the remaining per-module health checks: a Service Bus broker check in `Core.MessageBus`
and a typed-client **upstream** check in `Core.HTTP`. Because there is no foundation package, the
cached upstream base — `BaseUpstreamServiceHealthCheck<TReport>` + its options — **lives in
`Core.HTTP`** (its natural home: it probes a downstream `/health` over `HttpClient`). The Service Bus
check lives in `Core.MessageBus`. Each check sits **next to the implementation** it probes.

> **No foundation package (2026-06-25 decision).** `Core.MessageBus` and `Core.HTTP` reference the
> framework-agnostic `Microsoft.Extensions.Diagnostics.HealthChecks` **directly** — **not** a
> `Core.HealthChecks` package and **not** the ASP.NET variant. No `FrameworkReference` to
> `Microsoft.AspNetCore.App`. The ASP.NET health **endpoint** lives only in `Core.Api` (step 06).

## Affected components
- `src/SolTechnology.Core.MessageBus/HealthChecks/ServiceBusHealthCheck.cs` +
  `…Extensions.cs` — `IHealthCheck` probing broker reachability (e.g. management/peek of the
  configured entity) via the existing `Azure.Messaging.ServiceBus` client;
  `AddServiceBusHealthCheck(this IHealthChecksBuilder, …)`.
- `src/SolTechnology.Core.MessageBus/SolTechnology.Core.MessageBus.csproj` — add `PackageReference`
  `Microsoft.Extensions.Diagnostics.HealthChecks` (`10.0.x`, from step 07's canonical-versions row).
  **Non-ASP.NET.**
- `src/SolTechnology.Core.HTTP/HealthChecks/BaseUpstreamServiceHealthCheck.cs` +
  `…/HealthChecksOptions.cs` — the **cached upstream base** (moved here from the dropped foundation):
  calls a downstream `/health`, caches the result (~30 s, `TimeProvider`-sourced), applies a per-call
  timeout independent of the probe, deserialises a typed `TReport`, and maps the exception taxonomy
  (connection→`Unhealthy`, timeout→`Unhealthy`, **caller-cancellation→rethrow**, bad payload→`Degraded`).
- `src/SolTechnology.Core.HTTP/HealthChecks/UpstreamHttpHealthCheckExtensions.cs` — concrete
  `AddUpstreamHttpHealthCheck<TReport>(this IHealthChecksBuilder, …)` wiring for a registered typed
  client's `/health`.
- `src/SolTechnology.Core.HTTP/SolTechnology.Core.HTTP.csproj` — add `PackageReference`
  `Microsoft.Extensions.Diagnostics.HealthChecks` (`10.0.x`). **Non-ASP.NET.**
- `tests/SolTechnology.Core.MessageBus.Tests/` + `tests/SolTechnology.Core.HTTP.Tests/` — **existing**
  projects: negative tests (unreachable broker / upstream → `Unhealthy`; bad payload → `Degraded`;
  cancel → rethrow; cached result not re-hitting upstream within the window).
- `docs/Bus.md` + `docs/Clients.md` — short "Health check" subsections (live next to each module's
  doc). (**Note:** `docs/HTTP.md` does not exist — the HTTP module is documented in `docs/Clients.md`.)

## Details
- **MessageBus check:** broker-unreachable → `Unhealthy`. Use a lightweight liveness probe; do not
  consume real messages. `Core.MessageBus` has `TreatWarningsAsErrors=false` today
  (`SolTechnology.Core.MessageBus.csproj`) — keep the addition warning-clean regardless.
- **HTTP check + base:** `Core.HTTP` now **owns** `BaseUpstreamServiceHealthCheck<TReport>` (it is an
  HTTP-client concern — it calls a downstream `/health` over `HttpClient`). The base caches ~30 s,
  times out per call, deserialises `TReport`, and maps the taxonomy (connection→`Unhealthy`,
  timeout→`Unhealthy`, cancel→rethrow, bad payload→`Degraded`). `AddUpstreamHttpHealthCheck<TReport>`
  is the concrete registration. This is the **only** check carrying the full typed-report taxonomy;
  the SQL/Cache/Bus checks are plain connectivity pings.
- Both checks are opt-in `AddXxxHealthCheck()` chained onto the framework `AddHealthChecks()` builder.
- Reuse `Core.HTTP`'s `WireMock.Net` testing companion for the upstream-check tests where an HTTP
  fake is needed (per `package-management`).

## Acceptance criteria
- `AddServiceBusHealthCheck()` and `AddUpstreamHttpHealthCheck<TReport>()` chain onto the framework
  `IHealthChecksBuilder`.
- The HTTP check exercises the full `BaseUpstreamServiceHealthCheck` taxonomy (connection/timeout →
  `Unhealthy`, cancel → rethrow, bad payload → `Degraded`) with caching, all within `Core.HTTP`.
- The Service Bus check reports `Unhealthy` when the broker is unreachable.
- `Core.MessageBus` and `Core.HTTP` reference `Microsoft.Extensions.Diagnostics.HealthChecks` with
  **no** ASP.NET `FrameworkReference`; both build green.
- `docs/Bus.md` and `docs/Clients.md` document the new checks.

## Open questions
- Service Bus liveness probe shape (management call vs receiver peek) — pick the cheapest that does
  not consume messages; flag the choice for the reviewer.

