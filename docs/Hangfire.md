# SolTechnology.Core.Hangfire

Hangfire-backed persistent event dispatch and recurring jobs for `SolTechnology.Core.CQRS`.
Events survive process restarts via Hangfire storage; recurring jobs run on cron schedules
with typed, DI-resolved handlers. Intentionally minimal public surface —
new knobs require an [ADR](adr/009-hangfire-persistent-events-and-jobs.md).

## Features

- **Persistent events** — `IEvent` instances are enqueued as Hangfire background jobs.
  A crash between publish and dispatch does not lose the event.
- **Recurring jobs** — `IJob` implementations run on cron schedules, registered with a
  one-liner and resolved from DI at execution time.
- **Plugin model** — opt in with `AddPersistentEvents()` after `AddCQRS()`. Does not
  replace in-memory dispatch unless explicitly installed.

## Registration

The plugin offers two independent features — use either or both:

```csharp
// Persistent events only (requires AddCQRS first)
builder.Services.AddCQRS(assemblies: typeof(Program).Assembly);
builder.Services.AddPersistentEvents();

// Recurring jobs only (no AddCQRS dependency)
builder.Services.AddRecurringJob<MyDailyJob>(Cron.Daily);

// Both together
builder.Services.AddCQRS(assemblies: typeof(Program).Assembly);
builder.Services.AddPersistentEvents();
builder.Services.AddRecurringJob<MyDailyJob>(Cron.Daily);
```

| Method | Requires `AddCQRS()` | What it does |
|--------|---------------------|--------------|
| `AddPersistentEvents()` | ✅ yes | Replaces in-memory event publisher with Hangfire-backed durable dispatch |
| `AddRecurringJob<TJob>(cron)` | ❌ no | Registers a typed job on a cron schedule via Hangfire |

## Configuration

```csharp
builder.Services.AddPersistentEvents(o => o.QueueName = "events");
```

| Option | Default | Description |
|--------|---------|-------------|
| `QueueName` | `"default"` | Hangfire queue used for event dispatch jobs. |

## Usage — Persistent Events

Define an event and its handler(s) in your CQRS layer:

```csharp
public class CitySearched : IEvent
{
    public City City { get; set; }
}

public class SaveCitySearchHandler(ICityDomainService cityDomainService) : IEventHandler<CitySearched>
{
    public async Task Handle(CitySearched @event, CancellationToken cancellationToken)
    {
        await cityDomainService.Save(@event.City);
    }
}
```

Publish from any handler via `IMediator`:

```csharp
mediator.Publish(new CitySearched { City = result });
```

With `AddPersistentEvents()` installed, the event is enqueued as a Hangfire job and
dispatched in a fresh DI scope when the server picks it up.

## Usage — Recurring Jobs

```csharp
public class FetchTrafficJob(ITrafficClient client) : IJob
{
    public async Task Execute(CancellationToken cancellationToken)
    {
        await client.FetchLatest(cancellationToken);
    }
}

// Registration (Program.cs)
builder.Services.AddRecurringJob<FetchTrafficJob>("0 */6 * * *"); // every 6 hours
```

The job id is stable (`typeof(TJob).Name`) — re-registration on deploy updates rather
than duplicates the schedule.

## App-Side Requirements

The plugin depends on `Hangfire.Core` only. **The app must provide:**

1. **A DI-aware Hangfire `JobActivator`** — supplied by `Hangfire.AspNetCore` (via
   `AddHangfireServer()`) or `GlobalConfiguration.UseActivator(...)`. Without it,
   Hangfire cannot resolve the plugin's internal dispatcher/runner types.

2. **Type-aware job-argument serialisation** — `IEvent` is serialised through an
   interface-typed parameter. Configure:
   ```csharp
   builder.Services.AddHangfire(config => config
       .UseSqlServerStorage(connectionString)
       .UseRecommendedSerializerSettings()           // TypeNameHandling.Auto
       .UseSimpleAssemblyNameTypeSerializer());
   ```
   Without this, the event cannot be deserialised when the job runs.

3. **Hangfire server** — `builder.Services.AddHangfireServer()` so jobs are actually
   processed. Without it, jobs are persisted but never fire.

> The `Newtonsoft.Json` 13.0.4 pin (CVE-2024-21907) is satisfied transitively by the
> plugin's own package reference.

## Retry & Resilience

Persistent events dispatch **once** (`[AutomaticRetry(Attempts = 0)]`). A failed handler
surfaces as a **Failed** job in the Hangfire dashboard — visible and manually re-queueable.

Resilience is a **handler-level** concern (e.g. Polly inside the handler). The plugin
does not add automatic retries.

Persistence buys **durability** (a job enqueued before a crash runs after restart) and
**at-least-once** delivery (crash-recovery or manual re-queue). Handlers must be
**idempotent**.

## Rate Limiting

Out of scope for this `Hangfire.Core`-only plugin. Concurrency is bounded app-side via
`AddHangfireServer(o => o.WorkerCount = N)` and named queues. True jobs-per-interval
rate limiting requires `Hangfire.Throttling` or infrastructure-level controls.

## Event Payload Guideline

Persisted events may be re-dispatched (at-least-once). Prefer small, immutable payloads
carrying IDs — reload current state inside the handler. `CitySearched` carries its full
payload as a deliberate exception (freshly fetched city not yet in the store, no id to
reload by).

## Public API Surface

Intentionally minimal:

| Symbol | Kind |
|--------|------|
| `AddPersistentEvents()` | Extension method |
| `AddRecurringJob<TJob>(cron)` | Extension method |
| `IJob` | Interface |
| `PersistentEventsOptions` | Options class |

`IEvent`, `IEventHandler<T>`, `IEventPublisher`, `IEventDispatcher` live in
`SolTechnology.Core.CQRS` — see [CQRS docs](CQRS.md).

## Testing

In component/integration tests, **do not call `AddPersistentEvents()`**. The default
in-memory `IEventPublisher` from `AddCQRS()` dispatches events in-process (fire-and-forget)
— no Hangfire infrastructure needed, no test doubles required.

```csharp
// Test fixture — just use AddCQRS(), no publisher swap needed
builder.Services.AddCQRS(assemblies: typeof(Program).Assembly);
// Events dispatch in-memory; handlers run in background Task.Run with a fresh scope.
```

If your tests assert on handler side-effects, the in-memory publisher's `Task.Run` dispatch
is fast enough for most scenarios. For strict synchronous guarantees, assert with a short
poll/retry — not a custom publisher.

## See Also

- [ADR-009 — Persistent events and recurring jobs](adr/009-hangfire-persistent-events-and-jobs.md)
- [CQRS](CQRS.md) — commands, queries, events (in-memory default)
- [Cron / Scheduler](Cron.md) _(deprecated)_
