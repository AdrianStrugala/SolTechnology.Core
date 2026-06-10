---
adr: 009-hangfire-persistent-events-and-jobs
step: 09 of 10
status: reviewed
---

# Step 09: `SolTechnology.Core.Hangfire.Tests`

## Summary
Add the test project that proves the plugin's two behaviours in isolation, using fakes for Hangfire's
`IBackgroundJobClient` and `IRecurringJobManager` (no real storage/server). Covers: the publisher
enqueues exactly one job per event whose body re-enters the plugin and calls `IEventDispatcher.Dispatch`;
`AddPersistentEvents()` swaps the CQRS `IEventPublisher` **regardless of call order**;
`AddRecurringJob<TJob>(cron)` registers the job and the registrar calls `AddOrUpdate` with a stable id.
Test-only — no production code — kept separate so the feature steps (04–05) stay focused.

## Test stack — VERIFIED (mirror `tests/SolTechnology.Core.CQRS.Tests` exactly)
Review-verified against `tests/SolTechnology.Core.CQRS.Tests/SolTechnology.Core.CQRS.Tests.csproj`:
**NUnit 4.3.2 + NUnit3TestAdapter 4.6.0 + Microsoft.NET.Test.Sdk 17.12.0 + FluentAssertions 6.12.2 +
NSubstitute 5.3.0 + AutoFixture 4.18.1 + AutoFixture.AutoNSubstitute 4.18.1 + coverlet.collector 6.0.4**,
`<IsPackable>false</IsPackable>`. **No xUnit, no Moq.** Use `[TestFixture]`/`[Test]`, FluentAssertions
`.Should()`, and NSubstitute `Substitute.For<>()`.

## Affected components
- `tests/SolTechnology.Core.Hangfire.Tests/SolTechnology.Core.Hangfire.Tests.csproj` — **new**. Copy the
  package list above from the sibling CQRS test project (do **not** invent a stack). Reference
  `SolTechnology.Core.Hangfire` and `SolTechnology.Core.CQRS`.
- `tests/SolTechnology.Core.Hangfire.Tests/PersistentEventsTests.cs` — **new**:
  - `AddCQRS().AddPersistentEvents()` resolves `IEventPublisher` to `HangfireEventPublisher` (assert the
    runtime type), **and** `AddPersistentEvents().AddCQRS()`-style reverse ordering still wins (proves
    the order-independent `RemoveAll` + `Add` from steps 02/04).
  - Publishing an `IEvent` calls `IBackgroundJobClient.Create`/`Enqueue` **exactly once** with a job
    whose target re-enters the plugin and invokes `IEventDispatcher.Dispatch` (use a substitute
    `IBackgroundJobClient` and assert on the captured `Job` — target type/method).
  - The publisher creates a **fresh scope per dispatch** via `IServiceScopeFactory` (B2) — assert the
    scoped `IEventDispatcher` is resolved per call (e.g. spy scope factory), not captured at
    construction.
  - `QueueName` is honoured; `RetryAttempts` wiring matches whichever mechanism step 04's spike shipped
    (update this assertion to the final mechanism — do not assert a mechanism that was not built).
- `tests/SolTechnology.Core.Hangfire.Tests/RecurringJobTests.cs` — **new**:
  - `AddRecurringJob<TJob>("0 0 * * *")` registers `TJob` in DI and appends a recurring-job descriptor;
    `AddRecurringJob` itself makes **no** `AddOrUpdate` call (assert — the registrar owns timing, B1).
  - Driving `RecurringJobRegistrar.StartAsync` then calls `IRecurringJobManager.AddOrUpdate` with id
    `nameof(TJob)` and the given cron (substitute `IRecurringJobManager`).
  - The runner resolves `TJob` from a fresh scope and calls `Execute` (assert via a spy `IJob`).
- `tests/SolTechnology.Core.Hangfire.Tests/TestFixtures.cs` — **new**: a sample `IEvent`
  (`TestEvent`), a spy `IJob` (`TestJob`), and any shared fakes.
- `SolTechnology.Core.slnx` — register the new test project under the `/Tests/` folder block (the
  `<Folder Name="/Tests/">` block already exists).

## Details
- **No real Hangfire.** Do not stand up storage or a server. Substitute `IBackgroundJobClient` and
  `IRecurringJobManager`; assert on the captured job/recurring-job definitions. This keeps the suite a
  fast unit test.
- Follow the `// Arrange` / `// Act` / `// Assert` convention (ClaudeCodingGuide §8) used across the repo.
- **Auto-discovery confirmed:** `.github/runTests.ps1` is
  `ForEach ($folder in (Get-ChildItem -Path tests -Directory)) { dotnet test --no-build $folder.FullName }`
  — it walks every directory under `tests/`, so the new project is picked up automatically once it is in
  `.slnx` and built. (`--no-build` means the `.slnx` build must run first.)
- Invoke the [`test-writing`](../../../../.github/skills/test-writing/SKILL.md) skill if any layout
  question remains, but the stack is already pinned above — do not re-derive it.

## Acceptance criteria
- `tests/SolTechnology.Core.Hangfire.Tests` exists, registered in `.slnx`, using the **verified** stack
  above (identical to `tests/SolTechnology.Core.CQRS.Tests`).
- Tests prove: publisher swap (both call orders), one-job-per-event re-entering the plugin and targeting
  `Dispatch`, fresh-scope-per-dispatch, options honoured, recurring-job registration with stable id +
  cron, runner invokes `IJob.Execute`.
- `dotnet test tests/SolTechnology.Core.Hangfire.Tests` is green.
- `.github/runTests.ps1` includes and passes the new project.

## Open questions
- Whether to assert Hangfire job serialisation of a real `IEvent` payload (interface round-trip with
  `TypeNameHandling.Auto`) here or treat it as an integration concern. Default: assert the enqueued
  `Job` shape here; leave the real serialiser round-trip to the DreamTravel component tests (step 08),
  which run with the app's `UseRecommendedSerializerSettings()`.

