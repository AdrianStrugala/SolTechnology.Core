---
adr: 009-hangfire-persistent-events-and-jobs
step: 08 of 10
status: done
---

# Step 08: DreamTravel migration to the new seam

## Summary
Retire DreamTravel's bespoke `IHangfireNotificationPublisher` and route its events through the shipped
seam: handlers call `IMediator.Publish`, and the app opts into durability with
`services.AddCQRS().AddPersistentEvents()`. This is the **behavioural** migration that proves the new
API end-to-end (step 01 only did the mechanical marker rename; the bespoke publisher still existed).
Application/logic change — kept separate from the core library steps and from the docs step.

> DreamTravel is the **reference implementation** for the two app-side requirements step 04 documents:
> `DreamTravel.Worker` references `Hangfire.AspNetCore` (DI-aware `JobActivator`), and
> `DreamTravel.Sql` configures `UseRecommendedSerializerSettings()` + `UseSimpleAssemblyNameTypeSerializer()`
> (type-aware `IEvent` serialisation). Both are already in place — confirm they remain after the
> `Hangfire.Core` reference shuffle below.

## Affected components
- `sample-tale-code-apps/DreamTravel/src/Infrastructure/DreamTravel.Infrastructure/Events/EventPublisher.cs`
  — **delete** `IHangfireNotificationPublisher` + `HangfireNotificationPublisher` (the capability now
  lives in `SolTechnology.Core.Hangfire`).
- `.../DreamTravel.Infrastructure/ModuleInstaller.cs` — remove
  `services.AddTransient<IHangfireNotificationPublisher, HangfireNotificationPublisher>()` (line 10). If
  `InstallInfrastructure` becomes empty, keep the method (it is a stable seam) or fold the
  `AddPersistentEvents()` call here — decide and document where the opt-in lives.
- `.../DreamTravel.Infrastructure/DreamTravel.Infrastructure.csproj` — replace the direct
  `Hangfire.Core` (+ `Newtonsoft.Json` 13.0.4 pin) references with a `ProjectReference` to
  `SolTechnology.Core.Hangfire` **if** the plugin now supplies them transitively; keep only what
  Infrastructure still uses directly. Verify the CVE pin is still satisfied transitively (the plugin
  pins 13.0.4, so it should be).
- `.../DreamTravel.Queries/FindCityByName/FindCityByNameHandler.cs` — drop the
  `IHangfireNotificationPublisher` ctor param (line 14); inject `IMediator` and call
  `mediator.Publish(new CitySearched { City = result })` (replaces the line-30
  `hangfireNotificationPublisher.Publish(...)`).
- `.../DreamTravel.Queries/FindCityByCoordinates/FindCityByCoordinatesHandler.cs` — same change
  (ctor param at line 11).
- `.../DreamTravel.Worker/Program.cs` — add `builder.Services.AddPersistentEvents();` after
  `AddCQRS(...)` (line 51). The Worker already owns the Hangfire bootstrap (`AddHangfireServer()` line
  55, `MapHangfireDashboard` line 68) and storage (`DreamTravel.Sql/ModuleInstaller.cs`
  `AddHangfire(...).UseSqlServerStorage(...)`), so no new storage wiring is needed — confirm the
  publisher resolves the app's `IBackgroundJobClient`.
- `.../DreamTravel.Api/Program.cs` (or wherever the API composes its services) — ensure the API host
  also calls `AddPersistentEvents()` if it publishes events; otherwise events from the API fall back to
  in-memory. Decide which hosts get persistence and document.
- `.../DreamTravel/tests/Component/SyncHangfireNotificationPublisher.cs` — replace with a
  test-only synchronous `IEventPublisher` (see "Determinism" below). The determinism trick stays, the
  type it overrides changes (`IEventPublisher`, not `IHangfireNotificationPublisher`).
- `.../DreamTravel/tests/Component/ComponentTestsFixture.cs` — swap
  `RemoveAll<IHangfireNotificationPublisher>()` + `AddSingleton<…, SyncHangfireNotificationPublisher>()`
  (lines 53-54) for the equivalent `RemoveAll<IEventPublisher>()` + the new sync test publisher.

## Determinism in component tests (review correction)
The original draft said the new test double must "dispatch in-process and **block**, exactly as
`SyncHangfireNotificationPublisher` does today". That is **internally inconsistent**: today's
`SyncHangfireNotificationPublisher.DispatchEvent` resolves `IMediator` and calls
`mediator.Publish(notification)` — which is the **fire-and-forget** in-memory dispatch (`Task.Run`) and
does **not** block. So "today" does not actually block.

For a deterministic test double, **do better than today**: implement `IEventPublisher` so it resolves
`IEventDispatcher` from a fresh scope and **awaits** it synchronously —
`scope.ServiceProvider.GetRequiredService<IEventDispatcher>().Dispatch(@event, CancellationToken.None)
.GetAwaiter().GetResult();` — so handlers have completed before `Publish` returns. State explicitly
whether the suite relies on true blocking (await `Dispatch`) or on the existing
poll/delay-after-publish style, and pick the blocking variant if any assertion currently races.

## Details
- The two query handlers currently depend on `IHangfireNotificationPublisher`; after this step they
  depend on `IMediator` (already available in the CQRS package they reference). No new package needed in
  `DreamTravel.Queries`.
- Optional cleanup (note, do not force): `DreamTravel.Worker/Program.cs` still has imperative
  `recurringJobManager.AddOrUpdate("LogFromJob", …)` and `FetchTrafficJob.Register()`. These can move to
  `AddRecurringJob<TJob>(cron)` (step 05) to dogfood the new jobs API. If done, it is a **separate
  commit** within this PR or a follow-up — keep the event migration and the job migration reviewable
  apart.
- Re-run the DreamTravel build + component/E2E tests: `cd sample-tale-code-apps/DreamTravel &&
  dotnet build` then the component suite.

## Acceptance criteria
- `IHangfireNotificationPublisher` / `HangfireNotificationPublisher` no longer exist anywhere in
  DreamTravel.
- `FindCityByNameHandler` and `FindCityByCoordinatesHandler` publish via `IMediator.Publish`.
- The DreamTravel host(s) that need durability call `AddPersistentEvents()`; storage/server bootstrap
  is unchanged.
- Component tests pass with the new synchronous `IEventPublisher` test double (handlers complete before
  assertions — at least as deterministic as before).
- `cd sample-tale-code-apps/DreamTravel && dotnet build` is green; component + E2E suites pass.

## Open questions
- Where does the `AddPersistentEvents()` opt-in live — `InstallInfrastructure` vs each host's
  `Program.cs`? Pick the spot that keeps the API and Worker consistent.
- Migrate the imperative recurring jobs to `AddRecurringJob<TJob>` now (dogfood) or defer? Default:
  defer to a follow-up to keep this PR focused on events.
- Does `DreamTravel.Infrastructure` still need a direct `Hangfire.Core` reference after delegating to
  the plugin, or can it rely on the transitive one? Confirm the CVE pin remains satisfied.

## Retrospective — Implementation Deviations

### 1. `AddPersistentEvents()` lives in Worker's `Program.cs`, not `InstallInfrastructure`
**Original plan:** suggested placing the opt-in inside `InstallInfrastructure` or deciding between
the installer and each host's `Program.cs`.
**Actual implementation:** `AddPersistentEvents()` requires `AddCQRS()` to be called first (fail-fast
guard). In both hosts, `InstallInfrastructure()` is called before `AddCQRS()`. Moving
`AddPersistentEvents()` to `Program.cs` after `AddCQRS()` avoids reordering the install chain.
`InstallInfrastructure()` is kept as an empty stable seam.

### 2. API host does not call `AddPersistentEvents()`
**Original plan:** noted the API should also call `AddPersistentEvents()` if it publishes events.
**Actual implementation:** the API has no `Hangfire` infrastructure (no `AddHangfire`/`AddHangfireServer`,
no `IBackgroundJobClient`). Events published in the API use the default in-memory `IEventPublisher`
from CQRS (fire-and-forget, same as before). Only the Worker — which owns the Hangfire server and
storage — opts into persistence.

### 3. Removed `DreamTravel.Infrastructure → DreamTravel.Queries` project reference
**Original plan:** did not explicitly mention removing this reference.
**Actual implementation:** query handlers no longer depend on `DreamTravel.Infrastructure.Events`
(they use `IMediator` from CQRS). The project reference was removed to enforce the dependency
direction rule (LogicLayer must not depend on Infrastructure through a direct reference to the
namespace that was deleted).
