---
spec: 2026-07-06-opentelemetry-support
step: 02
status: to-do
---

# Step 02: MessageBus telemetry

## Summary

Makes consumer-side message handling visible in distributed traces and adds
publish/handle counters. Cross-process propagation itself is delegated to the Azure SDK
(`Diagnostic-Id` application property + built-in `ActivitySource`) — this step adds only
the handler-execution child span and the module's own metrics, so traces no longer dead-end
at the Service Bus hop.

## Affected components

- `src/SolTechnology.Core.MessageBus/Telemetry/MessageBusTelemetry.cs` — NEW — source + meter
- `src/SolTechnology.Core.MessageBus/Publish/MessagePublisher.cs` — EDIT — publish counter
- `src/SolTechnology.Core.MessageBus/Receive/MessageBusReceiver.cs` — EDIT — handler span + counters
- `src/SolTechnology.Core.MessageBus/ModuleInstaller.cs` — EDIT — `TryAddSingleton`
- `src/SolTechnology.Core.MessageBus/SolTechnology.Core.MessageBus.csproj` — EDIT — minor version bump
- `tests/SolTechnology.Core.MessageBus.Tests` — EDIT — span + metric tests

## Changes

- Precondition task: confirm empirically (SDK docs or a minimal harness) that
  `ServiceBusSender.SendMessageAsync` on the pinned `Azure.Messaging.ServiceBus@7.20.1`
  stamps the `Diagnostic-Id` application property and that `ServiceBusProcessor` parents
  its process span from it. Record the result in `## Deviations` if it contradicts this
  plan.
- NEW `MessageBusTelemetry` (stable contract — MAJOR bump to change):
  - `ActivitySource` name `SolTechnology.Core.MessageBus`.
  - `Meter` name `SolTechnology.Core.MessageBus` via `IMeterFactory` (shape of
    `HttpClientMetrics`).
  - `Counter<long> soltechnology.core.messagebus.messages_published` — tag `message.type`.
  - `Counter<long> soltechnology.core.messagebus.messages_handled` — tags `message.type`,
    `outcome` = `completed` | `abandoned` | `deadlettered`.
  - `Histogram<double> soltechnology.core.messagebus.handle_duration` (unit `s`) — tag
    `message.type`.
- EDIT `MessagePublisher`: increment `messages_published` after successful send. Keep the
  existing `CorrelationId` application property (backward compat with old consumers).
- EDIT `MessageBusReceiver.HandleMessageAsync`:
  - Start `messagebus.handle {messageType.Name}` (`ActivityKind.Internal`) **before** the
    correlation block so `correlationIdService.GetOrGenerate()` picks up the trace id
    (`CorrelationId.Generate` already prefers `Activity.Current`).
  - Tags: `messaging.message.id`, `message.type`, `messaging.destination.name`
    (`args.EntityPath`), `messaging.servicebus.delivery_count`.
  - On success: `SetStatus(ActivityStatusCode.Ok)`, `outcome=completed`.
  - On dead-letter paths (empty body / deserialization / null payload):
    `outcome=deadlettered`, `SetStatus(Error, <reason>)`.
  - On handler exception: `AddException(ex)`, `SetStatus(Error)`, `outcome=abandoned`.
  - Record `handle_duration` in all outcomes.
- EDIT `ModuleInstaller`: `services.TryAddSingleton<MessageBusTelemetry>()`.
- Constructor additions on public types `MessagePublisher` / `MessageBusReceiver`
  (DI-constructed; direct instantiators break) — confirmed acceptable by premortem
  verdict before this step runs.
- csproj: minor version bump.
- Tests: `ActivityListener`-based — handler span emitted with correct tags/status for
  completed, abandoned, and dead-lettered paths ([TestCase] where the assert shape is
  identical); `MetricCollector<long>` for both counters; correlation id equals trace id
  when no `CorrelationId` property is present on the message.

## Acceptance criteria

- [ ] `dotnet build SolTechnology.Core.slnx` green.
- [ ] `dotnet test tests/SolTechnology.Core.MessageBus.Tests` green.
- [ ] Handler span parents from the SDK process span (asserted via `ActivityListener`
      parent-id check or documented deviation).
- [ ] Message without `CorrelationId` property logs a trace-derived correlation id.

## Open questions

- none

## Deviations

<!-- Empty at authoring time. Filled by implement-plan when reality diverges from the plan. -->
