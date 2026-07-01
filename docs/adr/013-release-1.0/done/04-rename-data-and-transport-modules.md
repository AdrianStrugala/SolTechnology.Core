---
adr: 013-release-1.0
step: 04 of 11
status: done
---

<!-- Reviewed (2026-06-30): added the two missing health checks (MessageBus AddServiceBusHealthCheck,
     HTTP AddUpstreamHttpHealthCheck<TCheck>) to the rename scope and prefixed all four with Sol
     (B1 + answer 2); locked With* unprefixed and UseSolTechnologyFilters→UseSolFilters (answer 2);
     added the repo-wide symbol-string sweep with the load-bearing Hangfire throw-string/test pair
     called out (M1); switched DreamTravel call sites to symbol greps and fixed the Worker blast
     radius — Worker also calls AddPersistentEvents and AddRecurringJob (M2). -->

# Step 04: Rename wave 2 — data + transport modules (CQRS, SQL, BlobStorage, Cache, HTTP, MessageBus, Hangfire)

## Summary
Rename the registration surface of the data- and transport-layer modules to `AddSol*`. These modules
have no Core-internal callers (their only consumers are the sample app and tests, plus
`Hangfire`→`CQRS` which stays internally consistent because both rename together), so the wave is
self-contained. Pure DI plumbing — one PR, updating every call site to stay green.

## Affected components
- `src/SolTechnology.Core.CQRS/ModuleInstaller.cs` — EDIT — `AddCQRS` → `AddSolCQRS`.
- `src/SolTechnology.Core.SQL/ModuleInstaller.cs` + `SQL/HealthChecks/SqlHealthCheckExtensions.cs` — EDIT — `AddSQL` → `AddSolSQL`; `AddSqlHealthCheck` → `AddSolSqlHealthCheck`.
- `src/SolTechnology.Core.BlobStorage/ModuleInstaller.cs` — EDIT — `AddBlobStorage` → `AddSolBlobStorage`.
- `src/SolTechnology.Core.Cache/ModuleInstaller.cs` + `Cache/HealthChecks/RedisHealthCheckExtensions.cs` — EDIT — 6 `Add*` + `AddRedisHealthCheck` → `AddSolRedisHealthCheck`.
- `src/SolTechnology.Core.HTTP/ModuleInstaller.cs` + `HTTP/HealthChecks/UpstreamHttpHealthCheckExtensions.cs` — EDIT — `AddHTTPClient` (2 overloads) → `AddSolHTTPClient`; `AddUpstreamHttpHealthCheck<TCheck>` → `AddSolUpstreamHttpHealthCheck<TCheck>`.
- `src/SolTechnology.Core.MessageBus/ModuleInstaller.cs` + `MessageBus/HealthChecks/ServiceBusHealthCheckExtensions.cs` — EDIT — `AddMessageBus` → `AddSolMessageBus`; `AddServiceBusHealthCheck` → `AddSolServiceBusHealthCheck`; `With*` stay unprefixed.
- `src/SolTechnology.Core.Hangfire/ModuleInstaller.cs` — EDIT — `AddPersistentEvents`, `AddRecurringJob<T>`, `UseSolTechnologyFilters` → `UseSolFilters`.
- `sample-tale-code-apps/DreamTravel/**` — EDIT — `DreamTravel.Api/Program.cs`, `DreamTravel.Worker/Program.cs`, `DreamTravel.Sql/ModuleInstaller.cs`, `DreamTravel.GeolocationDataClients/ModuleInstaller.cs` (locate every call site by symbol grep, not line number).
- `tests/SolTechnology.Core.{CQRS,SQL,MessageBus,Cache,Hangfire,HTTP}.Tests/**` — EDIT — call sites + assertion strings.

## Changes
- Rename map:
  - `AddCQRS` → `AddSolCQRS`
  - `AddSQL` → `AddSolSQL`; `AddSqlHealthCheck` → `AddSolSqlHealthCheck`
  - `AddBlobStorage` → `AddSolBlobStorage`
  - `AddLocalCache` / `AddDistributedCache` / `AddLocalLock` / `AddDistributedLock` /
    `AddLocalIdempotency` / `AddDistributedIdempotency` → `AddSol…` (all six);
    `AddRedisHealthCheck` → `AddSolRedisHealthCheck`
  - `AddHTTPClient<…>` (both overloads) → `AddSolHTTPClient<…>`;
    `AddUpstreamHttpHealthCheck<TCheck>` → `AddSolUpstreamHttpHealthCheck<TCheck>`
  - `AddMessageBus` → `AddSolMessageBus`; `AddServiceBusHealthCheck` → `AddSolServiceBusHealthCheck`
  - `AddPersistentEvents` → `AddSolPersistentEvents`; `AddRecurringJob<T>` → `AddSolRecurringJob<T>`;
    `UseSolTechnologyFilters` → `UseSolFilters`
- **Resolved at step 00, applied here:**
  - **All four** health-check builder extensions (`AddSqlHealthCheck`, `AddRedisHealthCheck`,
    `AddServiceBusHealthCheck`, `AddUpstreamHttpHealthCheck<TCheck>`) take the `Sol` prefix (answer 2 /
    B1 — `AddServiceBusHealthCheck` and `AddUpstreamHttpHealthCheck<TCheck>` were missing from the
    original scope).
  - MessageBus `WithTopicPublisher` / `WithTopicReceiver` / `WithQueuePublisher` / `WithQueueReceiver`
    stay **unprefixed** (answer 2 — fluent continuations).
  - `Hangfire.UseSolTechnologyFilters` → `UseSolFilters` (answer 2).
- **Symbol-string sweep (M1) — authoritative repo-wide for the symbols renamed here.** Renaming the
  symbol does not touch `<c>`/`<see cref>` XML-doc, comments, or `throw`/log strings. **Load-bearing
  pair — must change together or a test fails at runtime:**
  - `Hangfire/ModuleInstaller.cs:27` throws `"AddPersistentEvents() requires AddCQRS() to be called first."`
    → `"AddSolPersistentEvents() requires AddSolCQRS() to be called first."`
  - `tests/SolTechnology.Core.Hangfire.Tests/ModuleInstallerTests.cs` — the call `services.AddPersistentEvents()`
    → `services.AddSolPersistentEvents()`, the call `services.AddCQRS(...)` → `services.AddSolCQRS(...)`,
    **and** the assertion `.WithMessage("*AddCQRS()*")` (line 25) → `.WithMessage("*AddSolCQRS()*")`.
  - Other known XML-doc hits: `CQRS/IEventPublisher.cs:5` (`<c>AddPersistentEvents()</c>`),
    `HTTP/Handlers/CorrelationPropagatingHandler.cs:8` (`<c>AddHTTPClient&lt;,&gt;</c>`),
    `Hangfire/PersistentEventsOptions.cs`.
  - Verify with `grep -rn "AddCQRS\|AddSQL\|AddBlobStorage\|AddLocalCache\|AddDistributedCache\|AddLocalLock\|AddDistributedLock\|AddLocalIdempotency\|AddDistributedIdempotency\|AddHTTPClient\|AddMessageBus\|AddPersistentEvents\|AddRecurringJob\|UseSolTechnologyFilters\|AddSqlHealthCheck\|AddRedisHealthCheck\|AddServiceBusHealthCheck\|AddUpstreamHttpHealthCheck" src tests sample-tale-code-apps`
    returning only `Sol`-prefixed names.
- **DreamTravel call sites (M2) — by symbol, not line number.** `DreamTravel.Api/Program.cs` and
  `DreamTravel.Worker/Program.cs` are both edited here (and again in steps 03/05), so line numbers
  drift. The **Worker** in particular calls `AddLocalCache`, `AddCQRS`, **`AddPersistentEvents`**, and
  **`AddRecurringJob<FetchTrafficJob>`** — all four must be renamed (the earlier "Worker lines 51/54"
  note missed `AddPersistentEvents` and `AddRecurringJob`). Also update
  `DreamTravel.Sql/ModuleInstaller.cs` and `DreamTravel.GeolocationDataClients/ModuleInstaller.cs`.
- No `[Obsolete]` shims.

## Acceptance criteria
- [ ] No public `AddCQRS` / `AddSQL` / `AddBlobStorage` / `AddLocalCache` (+5 Cache siblings) /
      `AddHTTPClient` / `AddMessageBus` / `AddPersistentEvents` / `AddRecurringJob` /
      `UseSolTechnologyFilters` remains in `src/`.
- [ ] All four health checks are `AddSolSqlHealthCheck` / `AddSolRedisHealthCheck` /
      `AddSolServiceBusHealthCheck` / `AddSolUpstreamHttpHealthCheck<TCheck>`; `With*` stay unprefixed.
- [ ] The Hangfire throw string and `Hangfire.Tests/ModuleInstallerTests.cs` (calls + `.WithMessage`)
      are updated together; `SolTechnology.Core.Hangfire.Tests` passes.
- [ ] The grep above returns only `Sol`-prefixed names across `src tests sample-tale-code-apps`.
- [ ] `dotnet build SolTechnology.Core.slnx` green; the six named test projects pass; DreamTravel
      `Api` + `Worker` compile.

## Open questions
- none — health-check prefixing, `With*`, and `UseSolTechnologyFilters` are resolved at step 00.

## Retrospective — Implementation Deviations
- **Rename executed semantically.** All 18 public symbols were renamed with the language-server
  rename (crefs + in-slnx call sites incl. DreamTravel `Api`/`Worker`/`Sql`/`Geo` updated
  automatically). The HTTP `AddHTTPClient` has two overloads (`<TIClient,TClient>` and
  `<…,TOptions>`) — each is a distinct symbol, so both were renamed; DreamTravel's Geo client uses
  the 3-arg overload.
- **Extra internal rename.** The private helper `AddHTTPClientInternal` → `AddSolHTTPClientInternal`
  so the acceptance grep (`AddHTTPClient` substring) returns clean; not a public change.
- **Load-bearing pair updated together.** `Hangfire/ModuleInstaller.cs` throw string
  `"AddPersistentEvents() requires AddCQRS()…"` → `"AddSolPersistentEvents() requires AddSolCQRS()…"`
  and the matching `Hangfire.Tests` assertion `.WithMessage("*AddCQRS()*")` → `"*AddSolCQRS()*"`.
- **Test names renamed to match (step-03 precedent).** All affected test method names and the CQRS
  test class `AddCQRSTests` → `AddSolCQRSTests` (file `git mv`'d) were renamed so the acceptance grep
  across `tests/` is clean.
- **XML-doc/comment sweep (M1).** 13 `<c>`/`<see cref>`/comment references across
  Api/HTTP/Hangfire/MessageBus/Cache/CQRS updated to the `Sol`-prefixed names.
- **`docs/` intentionally untouched** — per the plan, doc symbol references are swept in step 11; the
  step-04 acceptance grep scope is `src tests sample-tale-code-apps` only.
- **Validation:** `dotnet build SolTechnology.Core.slnx -c Release` green (0 errors); test projects
  CQRS 24, SQL 1, MessageBus 9, Cache 15, Hangfire 32, HTTP 79 — all pass; DreamTravel `Api` +
  `Worker` build green.

