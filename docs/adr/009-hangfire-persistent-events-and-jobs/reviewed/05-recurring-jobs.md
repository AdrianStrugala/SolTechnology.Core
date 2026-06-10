 ---
adr: 009-hangfire-persistent-events-and-jobs
step: 05 of 10
status: reviewed
---

# Step 05: Recurring jobs — `IJob` + `AddRecurringJob<TJob>(cron)`

## Summary
Ship the recurring-jobs feature: a first-class `IJob` abstraction (separate from `IEvent` per
ADR-009 Decision 4) and an `AddRecurringJob<TJob>(cron)` registration that hands the job to Hangfire's
`IRecurringJobManager`. This replaces the imperative `recurringJobManager.AddOrUpdate(...)` calls seen
in `DreamTravel.Worker/Program.cs` (lines 62-63) with a typed, DI-resolved job. Pure plugin logic on
`Hangfire.Core` only; kept separate from the event publisher (step 04) because jobs and events have
different lifecycles.

## Affected components
- `src/SolTechnology.Core.Hangfire/IJob.cs` — **new** public abstraction:
  `Task Execute(CancellationToken cancellationToken);`. XML `<summary>`: "A unit of work run on a
  cron schedule by Hangfire's recurring-job server. Distinct from `IEvent` — a job is *pull/scheduled*,
  an event is *push/reactive*."
- `src/SolTechnology.Core.Hangfire/RecurringJobRunner.cs` — **new** `internal` adapter that
  `IRecurringJobManager` can target by `MethodInfo`: resolves `TJob` from a fresh DI scope and calls
  `Execute`. Generic-by-type so a single registered runner serves all `IJob` implementations, or one
  runner per job type — pick the shape that keeps the Hangfire job id stable across deploys and document.
  (Same app-side requirement as step 04: a DI-aware `JobActivator` must resolve the runner at execution
  time — cross-reference step 04's "App-side requirements".)
- `src/SolTechnology.Core.Hangfire/ModuleInstaller.cs` — **add**
  `public static IServiceCollection AddRecurringJob<TJob>(this IServiceCollection services, string
  cronExpression) where TJob : class, IJob`. **Encodes maintainer decision B1: defer registration to
  an `IHostedService`, keeping the `AddRecurringJob<TJob>(cron)` one-liner; do NOT add a post-build
  `app.UseRecurringJob<TJob>()` surface.** `IRecurringJobManager.AddOrUpdate` needs `JobStorage`
  configured, which is not guaranteed at `ConfigureServices` time, so:
  - `AddRecurringJob<TJob>` registers `TJob` (scoped) and **appends a descriptor**
    `{ Type = typeof(TJob), Cron = cronExpression }` to an options list (e.g. `RecurringJobOptions`);
    it makes **no** `IRecurringJobManager` call and **no** `BuildServiceProvider()` call.
  - `src/SolTechnology.Core.Hangfire/RecurringJobRegistrar.cs` — **new** `internal sealed class
    RecurringJobRegistrar : IHostedService` (registered once, idempotently via `TryAddEnumerable`).
    In `StartAsync` it resolves `IRecurringJobManager` (constructor-injected from the built host) and
    drains the descriptor list, calling `AddOrUpdate(nameof(TJob), runnerExpression, cron)` for each.

## Why `IHostedService` (review correction to the rationale)
The original draft justified `IHostedService` over `IStartupFilter` with "a Worker app has no HTTP
pipeline". That reasoning is **factually shaky** — `DreamTravel.Worker` *is* a `WebApplication`
(`WebApplication.CreateBuilder`, `MapHangfireDashboard`, `MapGraphQL`), so it *does* have a pipeline.
Keep the **conclusion** (`IHostedService`) but on the correct rationale: **`IHostedService` runs in
both HTTP and non-HTTP hosts; `IStartupFilter` only runs inside an HTTP request pipeline, so it is the
narrower, more fragile choice.** This also mirrors the existing `ScheduledJob : IHostedService` pattern
and removes the `BuildServiceProvider()` mid-registration anti-pattern in `Scheduler/ModuleInstaller.cs`
rather than copying it.

## Details
- **`IJob` is NOT an `IEvent`.** Do not route jobs through `IEventPublisher`/`IEventDispatcher`. The
  two abstractions stay independent.
- `cronExpression` is a raw Hangfire/Cron string (`Cron.Daily`, `"0 0 * * *"`). Accept a `string`;
  optionally also accept a `Func<string>` overload — keep the primary surface a single `string`.
- The job id must be **stable** (`typeof(TJob).Name`) so re-registration on each deploy updates rather
  than duplicates the recurring job — matches Hangfire's `AddOrUpdate` idempotency.
- **`IRecurringJobManager` is only registered if the app called `AddHangfire(...)`.** If it is absent,
  `RecurringJobRegistrar.StartAsync` should **fail fast at startup** with a message naming the missing
  `AddHangfire`/storage bootstrap (preferred over a silent no-op where the schedule never registers).
  App still owns `AddHangfireServer()` (the process that actually fires recurring jobs) and storage.
  Document that without a running Hangfire server the job is persisted but never fires.
- This is the path that lets `DreamTravel.Worker/Program.cs` drop its inline
  `recurringJobManager.AddOrUpdate("LogFromJob", …)` and `FetchTrafficJob.Register()` calls — but that
  migration is **not** in this step. It is optional follow-up noted in step 08's open questions.

## Acceptance criteria
- `services.AddRecurringJob<MyJob>("0 0 * * *")` compiles, registers `MyJob`, and appends a recurring-job
  descriptor; running `RecurringJobRegistrar.StartAsync` then calls `IRecurringJobManager.AddOrUpdate`
  with id `nameof(MyJob)` and the given cron (verified in step 09 with a fake `IRecurringJobManager`).
- `IJob` is public; `RecurringJobRunner` and `RecurringJobRegistrar` are internal.
- No `BuildServiceProvider()` call inside `AddRecurringJob` **or** `RecurringJobRegistrar` (the registrar
  receives `IRecurringJobManager` via constructor injection from the built host).
- `dotnet build SolTechnology.Core.slnx` is green.

## Open questions
- Single generic runner vs per-type runner — pick the one giving stable job ids and minimal Hangfire
  closure; record the choice.
- Cron input type: `string` only vs add a `Func<string>` overload. Default to `string`.
- **Per-job rate limit — RESOLVED (2026-06-10): out of scope.** The maintainer confirmed no rate limit
  is needed now (same answer as step 04). `Hangfire.Core` bounds **concurrency** app-side (worker count
  / named queues); true **rate-limiting** (jobs-per-interval) is `Hangfire.Throttling`/infra and is not
  built. No rate-limit surface ships for recurring jobs.

