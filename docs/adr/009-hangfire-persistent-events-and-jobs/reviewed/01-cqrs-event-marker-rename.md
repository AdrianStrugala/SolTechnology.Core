---
adr: 009-hangfire-persistent-events-and-jobs
step: 01 of 10
status: reviewed
---

# Step 01: CQRS event marker split + `IEvent` rename

## Summary
Rename the notification contract to the domain term **event** and split the over-stuffed marker
file. `INotification` → `IEvent`, `INotificationHandler<T>` → `IEventHandler<T>` (breaking, no
`[Obsolete]` shim — pre-1.0 per ADR-009). The four markers currently share one file
(`ICommand.cs`); extract `IQuery<T>` into `IQuery.cs` and the event marker into `IEvent.cs`. This is
a single cohesive "rename the contract" PR. Because `SolTechnology.Core.slnx` includes the DreamTravel
projects, the **mechanical** marker references in DreamTravel travel with this PR to keep the build
green; the **behavioural** seam migration is deferred to step 08.

## Affected components
- `src/SolTechnology.Core.CQRS/ICommand.cs` — keep only `ICommand` + `ICommand<TResult>`; remove
  `IQuery<TResult>` and `INotification` (moved out).
- `src/SolTechnology.Core.CQRS/IQuery.cs` — **new**: `IQuery<TResult>` marker (XML doc preserved).
- `src/SolTechnology.Core.CQRS/IEvent.cs` — **new**: `IEvent` marker (renamed from `INotification`,
  XML doc updated to say "event").
- `src/SolTechnology.Core.CQRS/IEventHandler.cs` — **rename** of `INotificationHandler.cs`;
  `IEventHandler<in TEvent> where TEvent : IEvent`.
  - **Carry the existing XML doc verbatim in this step (rename only).** Note that the current doc
    ("Each handler runs on its own background task with a fresh DI scope") **misdescribes the actual
    dispatch** (one `Task.Run`, one shared scope, sequential `foreach`, per-handler try/catch — see
    `Internal/Mediator.cs:43-60`). **Do not "fix" behaviour here.** The doc is corrected to match the
    real semantics in step 02, where the dispatch actually moves. Flagged so the implementer does not
    silently encode the inaccurate "per-handler scope" contract.
- `src/SolTechnology.Core.CQRS/IMediator.cs` — `Publish<TNotification>` → `Publish<TEvent> where
  TEvent : IEvent`; non-generic `Publish(INotification)` → `Publish(IEvent)`; **method name stays
  `Publish`**; XML docs updated (event vocabulary). Same caveat as above: the "own background task /
  fresh DI scope" wording is corrected in step 02, not here.
- `src/SolTechnology.Core.CQRS/Internal/Mediator.cs` — generic constraint, `GetServices<IEventHandler<TEvent>>()`,
  the reflected non-generic `Publish` dispatch, and log message template renamed to event vocabulary.
- `src/SolTechnology.Core.CQRS/ModuleInstaller.cs` — handler scan `typeof(INotificationHandler<>)`
  → `typeof(IEventHandler<>)` (line 50).
- `tests/SolTechnology.Core.CQRS.Tests/TestFixtures.cs` — `TestNotification : IEvent`,
  `TestNotificationHandler`/`ThrowingNotificationHandler : IEventHandler<TestNotification>` (class
  names **kept**; only the implemented interface changes — lines 77/88/100).
- `tests/SolTechnology.Core.CQRS.Tests/Notifications/NotificationDispatcherTests.cs` — **likely no
  edit required.** Verified: this file references the concrete `TestNotification` type (whose name is
  kept), **not** the `INotification` identifier. It only needs touching if the implementer also
  renames the `Notifications/` folder/namespace (see Open questions). Keep test intent identical.
- DreamTravel **mechanical** rename (keep `slnx` green; no behavioural change):
  - `sample-tale-code-apps/DreamTravel/src/DreamTravel.Domain/Events/CitySearched.cs` — `: IEvent`.
  - `.../DreamTravel.Worker/EventHandlers/OnCitySearched/SaveCitySearchJob.cs` — `: IEventHandler<CitySearched>`.
  - `.../DreamTravel.Worker/EventHandlers/OnCitySearched/LogEventInfoJob.cs` — `: IEventHandler<CitySearched>`.
  - `.../DreamTravel.Infrastructure/Events/EventPublisher.cs` — `IHangfireNotificationPublisher`
    parameter/return types `INotification` → `IEvent` (the interface itself is **deleted in step 08**,
    here only the marker type is renamed). Confirmed: this file has `INotification` on lines 9, 10,
    24, 31.
  - `.../DreamTravel/tests/Component/SyncHangfireNotificationPublisher.cs` — `INotification` → `IEvent`
    (confirmed: `Publish(INotification)` / `DispatchEvent(INotification)`).

## Details
- Pure rename + file split. **No behavioural change** to dispatch (still `Task.Run` fan-out — the seam
  comes in step 02).
- Keep each new marker file single-purpose with its existing `<summary>` XML doc; do not merge docs.
- `ICommand` must still inherit `ICommand<Nothing>` — leave that line untouched in `ICommand.cs`.
- The `IMediator.Publish` method name is preserved.
- Search the whole solution for residual `INotification` / `INotificationHandler` identifiers before
  finishing (`grep` both). The full in-repo blast radius was cross-checked during review and is exactly
  the files listed above (`src/` 5 files, `tests/` 1 fixture, DreamTravel 5 files); docs are updated in
  step 07, not here, but **code** must be clean.

## Acceptance criteria
- No `INotification` or `INotificationHandler` identifier remains in `src/` or `tests/` or
  `sample-tale-code-apps/` (docs excluded — step 07).
- `IMediator` still exposes a method named `Publish` (generic + non-generic overloads).
- `dotnet build SolTechnology.Core.slnx` is green.
- `dotnet test tests/SolTechnology.Core.CQRS.Tests` is green (behaviour unchanged).
- DreamTravel still compiles; `IHangfireNotificationPublisher` still exists (deletion is step 08).

## Open questions
- Rename the `tests/.../Notifications/` folder to `Events/`? Defer to the implementer; cosmetic, must
  not change test behaviour. If renamed, do it as a pure folder/namespace move with no content change.

