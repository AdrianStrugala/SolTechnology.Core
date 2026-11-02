---
spec: 2026-11-02-core-2-0-hangfire
step: 03
status: to-do
---

# Step 03: Hangfire — restore ambient correlation state; async, cancellable event dispatch

## Summary
Two execution-context defects in Hangfire job execution:

- `CorrelationIdJobFilter.OnCreating` and `OnPerforming` are synchronous, so the `AsyncLocal`
  writes from `GetOrGenerate()`/`Set()` stay on Hangfire's long-lived threads. On the
  recurring-scheduler thread the first generated ID is reused for every later trigger, and
  `OnPerformed` never restores the previous value (`infra#8`,
  `Filters/CorrelationIdJobFilter.cs:22,37-42,49-55`).
- `HangfireEventPublisher.DispatchInScope` blocks a worker with `.GetAwaiter().GetResult()` and
  passes `CancellationToken.None`, so server shutdown cannot cancel handlers (`infra#9`,
  `HangfireEventPublisher.cs:33-37`).

`HangfireEventPublisher` is `internal`, so no public symbol changes. The persisted job signature
changes (P-05). RQ10 (user, 2026-11-02): the 1.x one-argument `DispatchInScope(IEvent)` overload
stays for 2.x, so event jobs enqueued by 1.x still run after the deploy and no queue drain is
needed.

Former core step 32; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.Hangfire/Filters/CorrelationIdJobFilter.cs` — EDIT
- `src/SolTechnology.Core.Hangfire/HangfireEventPublisher.cs` — EDIT — NEW `public async Task DispatchInScope(IEvent @event, CancellationToken cancellationToken)`; the enqueue expression at `:44` targets it and passes `CancellationToken.None` as the placeholder Hangfire replaces with the shutdown token; the existing one-argument `DispatchInScope(IEvent)` stays as a legacy overload (public method on the internal type, as Hangfire requires) that forwards with `CancellationToken.None`, carries `// Legacy 1.x job signature; remove in 3.0.`, and is never used for new enqueues
- `tests/SolTechnology.Core.Hangfire.Tests/Filters/CorrelationIdJobFilterTests.cs` — EDIT
- `tests/SolTechnology.Core.Hangfire.Tests/HangfireEventPublisherTests.cs` — EDIT
- `tests/SolTechnology.Core.Hangfire.Tests/HangfirePersistedIdentityTests.cs` — NEW — premortem RM-7: pins the persisted job identity
- `docs/features/2026-11-02-core-2-0-flagship-hardening.md` — EDIT — premortem RM-6: release-notes input

## Changes
- `OnCreating`: `Get() ?? CorrelationId.Generate()` stamped on the job parameter without writing ambient state.
- `OnPerforming`: save the previous value in `context.Items` and set the job's ID. `OnPerformed`: restore the previous value.
- Tests:
  - `OnCreating_TwoJobsSameThread_GetDistinctIds`.
  - `OnPerformed_RestoresPreviousAmbientId`.
  - `DispatchInScope_ShutdownToken_CancelsHandler`.
  - `DispatchInScope_Legacy1xJob_StillRuns`: a job created with the 1.x one-argument expression, serialized through Hangfire's `InvocationData` and performed after the change, invokes the handler.
  - `Publish_NewEvent_EnqueuesTwoArgumentOverload`.
- The 2.0 migration guide (release step 05, [`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) notes that 1.x event jobs keep running in 2.x and that the legacy overload is removed in 3.0 (P-05). The removal is a retrospective follow-up.
- Premortem mitigations (core step 00, 2026-11-02):
  - RM-6 (scenario 6): add to the core brief's "Release-notes inputs": "Deploy Hangfire servers (workers) before processes that only enqueue. A 1.x server cannot run event jobs enqueued by 2.x; they fail and are retried until a 2.x server takes them." The `docs/Hangfire.md` note is in step 06.
  - RM-7 (scenario 7): `HangfirePersistedIdentityTests` asserts:
    - `typeof(HangfireEventPublisher).FullName == "SolTechnology.Core.Hangfire.HangfireEventPublisher"`;
    - the simple assembly name `SolTechnology.Core.Hangfire`;
    - public `DispatchInScope(IEvent)` and `DispatchInScope(IEvent, CancellationToken)` exist;
    - `` typeof(RecurringJobRunner<>).FullName == "SolTechnology.Core.Hangfire.RecurringJobRunner`1" `` with a public `RunAsync(CancellationToken)`;
    - the correlation job-parameter key used by `CorrelationIdJobFilter` equals its `4097d47f` value.

    Hangfire stores these names in every enqueued, scheduled, and recurring job.

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.Hangfire.Tests` is green, and the distinct-ID test fails on `4097d47f`.
- [ ] `grep -n "GetAwaiter().GetResult()" src/SolTechnology.Core.Hangfire -r` prints nothing.
- [ ] `DispatchInScope_Legacy1xJob_StillRuns` is green.
- [ ] Premortem mitigations for the persisted job signature (P-05) are applied.
- [ ] RM-6: the core brief's "Release-notes inputs" contains the server-before-client deploy note.
- [ ] RM-7: `HangfirePersistedIdentityTests` is green.

## Open questions
- none (RQ10 resolved by the user on 2026-11-02: keep the legacy overload for 2.x)

## Deviations
