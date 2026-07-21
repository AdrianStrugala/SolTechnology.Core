---
status: completed
created: 2026-06-09
completed: 2026-06-10
---

# Hangfire Persistent Events and Recurring Jobs

> Historical delivery record. It may not describe the current system.

## Goal

Add opt-in Hangfire persistence for CQRS events and recurring jobs without coupling CQRS to
Hangfire deployment infrastructure.

## Context

In-memory events could be lost on process shutdown, while applications needed scheduled jobs with
stable registration and operational visibility. DreamTravel already had a bespoke Hangfire
publisher that re-resolved the mediator inside a background job, proving the pattern but forcing
each application to own it.

The feature had to solve three connected problems:

- make persistent dispatch reusable;
- keep `SolTechnology.Core.CQRS` independent of Hangfire;
- replace infrastructure-oriented notification terminology with domain event terminology and
  cohesive marker files.

The solution was constrained to .NET and Hangfire. Storage, server, dashboard, and serializer setup
remained application concerns. Automatic retry and library-level rate limiting were explicitly out
of scope; handlers retained responsibility for resilience and idempotency.

## Original decision

### CQRS event contract

Rename `INotification` and `INotificationHandler<T>` to `IEvent` and `IEventHandler<T>` before 1.0,
without a compatibility shim. Keep `IMediator.Publish` and add two public boundaries:

- `IEventPublisher` controls how a published event leaves the mediator;
- `IEventDispatcher` resolves and invokes the event's handlers.

The default in-memory publisher and dispatcher preserved fire-and-forget behavior. `TryAdd`
registration allowed a transport plugin to replace only the publisher.

### Hangfire plugin

Introduce `SolTechnology.Core.Hangfire` referencing `Hangfire.Core` only. Its publisher enqueued one
job per event. The job opened a fresh scope and invoked the CQRS dispatcher for all handlers.
Persistent event runners used zero automatic retries; persistence protected queued work across
process restarts while manual requeue and crash recovery still required idempotent handlers.

The plugin also introduced a separate `IJob` abstraction and typed recurring-job registration.
Recurring jobs were deliberately not modeled as events because commands to run on a schedule and
facts that fan out have different semantics.

### Application ownership

Consumers remained responsible for storage packages, `AddHangfire`, server startup, dashboard
mapping, and serialization. The plugin only registered event and recurring-job behavior.

### Scheduler migration

The older Scheduler API was marked obsolete in favor of Hangfire recurring jobs, and an orphan
Jobs artifact folder was removed.

## Alternatives considered

### One Hangfire job per event

Selected because it matched the proven DreamTravel design and represented one published event as
one persisted unit. The tradeoff was that manual requeue could repeat handlers that had already
succeeded.

### One Hangfire job per handler

Rejected because publishing would need handler enumeration, storage and dashboard volume would
grow with handler count, and the plugin would reach further into CQRS registration internals.

### Put Hangfire directly in CQRS

Rejected because command-only consumers would receive scheduler and serialization dependencies.

### Use one generic message marker for events and jobs

Rejected because it obscured different fan-out, scheduling, and lifecycle semantics.

### Use a broker or outbox

Deferred. The publisher/dispatcher seam kept future transports possible without making them part of
this delivery.

## Scope

- Replace `IEventPublisher` with a Hangfire-backed publisher when explicitly registered.
- Add recurring-job registration and correlation/Result-aware filters.
- Leave storage, server, dashboard, and serializer setup application-owned.
- Disable automatic retries in the library runners.
- Rename CQRS notification markers to event markers.
- Deprecate the older Scheduler mechanism and migrate DreamTravel.

## Implementation plan

Deliver the package, CQRS integration seam, event publisher, recurring-job registrar, filters,
tests, and documentation.

## Acceptance criteria

- Events are enqueued before dispatch.
- Recurring jobs register with stable IDs.
- CQRS has no Hangfire dependency.
- Applications retain control of Hangfire infrastructure.
- CQRS uses replaceable publisher and dispatcher interfaces without a Hangfire reference.
- Existing in-memory behavior remains the default when the plugin is absent.
- Scheduler consumers receive a migration path to typed recurring jobs.
- The new dependency does not reintroduce a vulnerable Newtonsoft.Json floor.

## Expected consequences

### Positive

- Any CQRS application can opt into persistent enqueueing without changing handlers.
- CQRS remains transport-neutral and dependency-light.
- Event terminology reads as a domain concept.
- Future brokers or outboxes can use the same publication boundary.
- Recurring background work converges on one mechanism.

### Negative

- The event-marker rename was a breaking pre-1.0 migration.
- Applications still need to understand and configure Hangfire infrastructure.
- Per-event dispatch requires handler idempotency when a whole event is requeued.
- Hangfire introduced a new external package and a transitive Newtonsoft.Json vulnerability floor
  that had to be pinned at the plugin boundary.

## Completion summary

The CQRS marker rename and dispatch seams, Hangfire plugin, persistent publisher, typed recurring
jobs, Scheduler deprecation, DreamTravel migration, consumer documentation, and plugin tests
shipped. The dependency boundary pinned a non-vulnerable Newtonsoft.Json version. DreamTravel's
worker enabled persistence after CQRS registration, while its API host retained the default
in-memory publisher.

Correlation and Result-aware filters were added to the module's later production surface. Current
behavior lives in
[`../architecture/background-processing.md`](../architecture/background-processing.md).

## Deviations

- CQRS swallows handler exceptions, so failed event handlers normally do not create failed
  Hangfire jobs.
- `preventOverlap` is stored but is not currently enforced.
- Registration and filter names changed during the 1.0 API rename.
- `AddPersistentEvents()` remained in DreamTravel Worker's composition root rather than its
  Infrastructure installer because CQRS had to be registered first.
- Removing the bespoke publisher also removed an unnecessary Queries-to-Infrastructure project
  reference.

## Follow-ups

- Treat per-handler failure visibility and overlap prevention as separate corrective features.
- Introduce another durable transport only through the CQRS publisher boundary.
- Keep Hangfire storage and dashboard packages application-owned.
