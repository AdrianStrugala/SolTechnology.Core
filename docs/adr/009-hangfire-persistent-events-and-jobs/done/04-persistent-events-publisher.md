---
adr: 009-hangfire-persistent-events-and-jobs
step: 04 of 10
status: done
---

# Step 04: Persistent events publisher + `AddPersistentEvents()`

## Summary
Ship the core feature: a `HangfireEventPublisher` that replaces the CQRS in-memory publisher and
enqueues **one Hangfire background job per event**, plus the `AddPersistentEvents()` opt-in and its
`PersistentEventsOptions`. The options class and the installer that consumes it ship together (never
split). The enqueued job re-enters the plugin and calls `IEventDispatcher.Dispatch` inside a fresh DI
scope — dispatching every handler for that event **once** — mirroring DreamTravel's
`HangfireNotificationPublisher.DispatchEvent`. This is the plugin's event **logic**, kept separate from
the project plumbing (step 03) and the recurring-jobs feature (step 05).

## Publisher shape — RESOLVED to mirror DreamTravel + maintainer decision B2 (review correction)
The original draft proposed `_backgroundJobClient.Enqueue<IEventDispatcher>(d => d.Dispatch(@event, …))`
with **no** `IServiceScopeFactory`, justified as "matching today's singleton `CQRSMediator`" and
"mirroring DreamTravel". The cross-check found **three problems**:

1. **`CQRSMediator` is registered scoped, not singleton** (`TryAddScoped<IMediator, CQRSMediator>()`).
   The "matching today's singleton" rationale is false.
2. **It does not mirror DreamTravel.** DreamTravel's `HangfireNotificationPublisher` holds
   `IServiceScopeFactory` + `IBackgroundJobClient`, enqueues its **own** instance method
   (`Enqueue(() => DispatchEvent(notification))`), and creates the scope itself inside `DispatchEvent`
   (`using var scope = _serviceScopeFactory.CreateScope()`). It does **not** enqueue
   `IEventDispatcher.Dispatch`.
3. **It contradicts B2** ("the persistent publisher resolves as a singleton **plus
   `IServiceScopeFactory`**, fresh scope per dispatch").

**Resolved design (faithful to DreamTravel + B2):**
- `HangfireEventPublisher` is registered **singleton**. The reason singleton is safe: it injects only
  `IBackgroundJobClient` (Hangfire-singleton), `IServiceScopeFactory`, and the bound options — it never
  captures a scoped service.
- `Publish<TEvent>` / `Publish(IEvent)` enqueue a call to the **plugin's own** dispatch method, e.g.
  `_backgroundJobClient.Enqueue<HangfireEventPublisher>(p => p.DispatchInScope(@event))`.
- `DispatchInScope(IEvent @event)` runs on the Hangfire server: `using var scope =
  _scopeFactory.CreateScope(); var dispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();
  dispatcher.Dispatch(@event, CancellationToken.None).GetAwaiter().GetResult();` — a fresh scope per
  dispatch, exactly as today.

This keeps the enqueued target **inside the plugin** (which can reference Hangfire), instead of
`IEventDispatcher.Dispatch` (which lives in `SolTechnology.Core.CQRS` and must stay Hangfire-free).
That matters for retry (below) and avoids depending on Hangfire activating a CQRS interface directly.

## Affected components
- `src/SolTechnology.Core.Hangfire/PersistentEventsOptions.cs` — **new** public options:
  `string QueueName` (default `"default"`), with an XML `<summary>`. **No `RetryAttempts` property** —
  the maintainer settled that persistent events do **not** auto-retry (see "Retry" below), so there is
  no retry knob to bind. Keep the class (one property today) as the minimal extension point ADR-009
  names; new knobs require an ADR.
- `src/SolTechnology.Core.Hangfire/HangfireEventPublisher.cs` — **new** `internal sealed class
  HangfireEventPublisher : IEventPublisher`, **singleton + `IServiceScopeFactory`** (see resolved
  design above). Enqueues its own `DispatchInScope` method per event, honouring `QueueName`.
  `DispatchInScope` is decorated `[AutomaticRetry(Attempts = 0)]` — **no automatic retry** (see
  "Retry" below).
- `src/SolTechnology.Core.Hangfire/ModuleInstaller.cs` — **new/expanded**:
  `public static IServiceCollection AddPersistentEvents(this IServiceCollection services,
  Action<PersistentEventsOptions>? configure = null)`. Binds options, registers
  `HangfireEventPublisher`, then **replaces** the CQRS default publisher with the **order-independent**
  mechanism fixed in step 02: `services.RemoveAll<IEventPublisher>(); services.AddSingleton<
  IEventPublisher, HangfireEventPublisher>();`. Does **not** call `AddHangfire`, `AddHangfireServer`,
  or `MapHangfireDashboard` — those stay app-owned (ADR-009 §Decision 5).

## App-side requirements the plugin CANNOT satisfy (review additions — document loudly here + step 07)
1. **A DI-aware Hangfire `JobActivator` is required.** When the job runs, Hangfire resolves the job
   target (`HangfireEventPublisher`) via `JobActivator.Current`. The default `Hangfire.Core` activator
   uses `Activator.CreateInstance` and **cannot** resolve a DI type with constructor dependencies. The
   app must supply a DI-backed activator — provided automatically by `Hangfire.AspNetCore` /
   `Hangfire.NetCore` via `AddHangfireServer()`, or manually via `GlobalConfiguration.UseActivator(...)`.
   (DreamTravel.Worker references `Hangfire.AspNetCore`, which is why the existing seam works.) The
   plugin references **`Hangfire.Core` only** — the *server-side* activator is an app concern. State
   this hard requirement.
2. **`IEvent` job arguments require type-aware serialisation.** The enqueued argument is typed as the
   **interface** `IEvent`. Newtonsoft cannot deserialise back to the concrete event type unless the
   app configured Hangfire's serializer to emit `$type` metadata — i.e. `UseRecommendedSerializerSettings()`
   (which sets `TypeNameHandling.Auto`) + `UseSimpleAssemblyNameTypeSerializer()`. DreamTravel.Sql does
   exactly this; without it, `DispatchInScope` fails **at job-execution time**, not at publish. Document
   this constraint and have step 09/08 exercise a real round-trip.

## Retry — RESOLVED: no automatic retry (maintainer decision 2026-06-10)
The configurable-`RetryAttempts` mechanism is **dropped**. The maintainer settled it: *"since this is
an event, let's not do automatic retries"* — resilience is a **handler-level** concern (a handler that
needs it wraps its own work, e.g. with Polly), not a plugin knob. This also closes the earlier blocker
cleanly: there is no compile-time-constant problem and no need to touch process-global
`GlobalJobFilters`.

- `DispatchInScope` is decorated `[AutomaticRetry(Attempts = 0)]` — a published event is dispatched
  **once**. On a thrown handler the job is marked **Failed** in the Hangfire dashboard (visible and
  manually re-queueable) rather than silently swallowed as in today's fire-and-forget `Task.Run`.
- **Durability is still the win:** if the process crashes *after* enqueue but *before* the job executes,
  the persisted job survives and runs on restart — the gap fire-and-forget cannot cover.
- **Idempotency still matters** even at `Attempts = 0`: a durable queue is **at-least-once** (a worker
  killed mid-`Dispatch`, or a manual dashboard re-queue, re-runs the job). Document the idempotency
  contract on that basis — not on automatic retry.

## Details
- **Per-event, not per-handler.** Enqueue exactly one job per published event (ADR-009 Alternative 1).
  The job body is `DispatchInScope` → `IEventDispatcher.Dispatch(@event, …)`; do not enumerate handlers
  at publish time.
- `AddPersistentEvents()` is **chained after `AddCQRS()`**: `services.AddCQRS().AddPersistentEvents()`.
  It assumes `IEventDispatcher` is registered (step 02). If `IEventDispatcher` is absent, fail fast with
  a clear `InvalidOperationException` naming `AddCQRS()` as the missing prerequisite (preferred over
  silently `TryAdd`-ing a dispatcher, so misconfiguration is loud).
- The app still owns `AddHangfire(...).UseXxxStorage(...)` and `AddHangfireServer()`; without a
  configured `IBackgroundJobClient`/storage, `Enqueue` throws at runtime. Note this loudly in the XML
  doc and (step 07) in `Hangfire.md`.

## Acceptance criteria
- `services.AddCQRS().AddPersistentEvents()` resolves `IEventPublisher` to `HangfireEventPublisher`,
  **regardless of call order** (order-independent `RemoveAll` + `Add`).
- Publishing an `IEvent` enqueues exactly **one** Hangfire job whose target re-enters the plugin and
  invokes `IEventDispatcher.Dispatch` in a fresh scope (verified by step 09 with a fake
  `IBackgroundJobClient`).
- `PersistentEventsOptions` is public; `HangfireEventPublisher` is internal.
- `QueueName` is honoured. `DispatchInScope` carries `[AutomaticRetry(Attempts = 0)]` — **no automatic
  retry**, and `PersistentEventsOptions` exposes **no** retry knob.
- No `AddHangfireServer` / storage / dashboard call exists in the plugin.
- The XML doc states the two app-side requirements (DI activator; type-aware serializer).
- `dotnet build SolTechnology.Core.slnx` is green.

## Open questions
- **Retry mechanism — RESOLVED (2026-06-10):** no automatic retry. `DispatchInScope` is
  `[AutomaticRetry(Attempts = 0)]`; retry is a handler-level concern. See "Retry" above. No spike, no
  configurable knob.
- **Per-event rate limit — RESOLVED (2026-06-10): out of scope.** The maintainer confirmed no rate limit
  is needed now. `Hangfire.Core` bounds **concurrency** app-side (`AddHangfireServer(o => o.WorkerCount =
  N)` + named queues); true **rate-limiting** (jobs-per-interval) is `Hangfire.Throttling`/Pro or
  app/infra and is **not** in scope for this `Hangfire.Core`-only plugin. Step 07 documents it as an app
  concern; no rate-limit surface is built.

