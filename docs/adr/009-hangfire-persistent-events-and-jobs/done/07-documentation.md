---
adr: 009-hangfire-persistent-events-and-jobs
step: 07 of 10
status: done
---

# Step 07: Documentation — `Hangfire.md` + parity

## Summary
Author the new module doc `docs/Hangfire.md` (the NuGet readme target referenced by step 03's
`.csproj`) and bring the rest of the docs into parity with the `IEvent` rename and the Scheduler
deprecation. **Docs only** — no code. Lands after the API it documents is final (≥ step 05) so the
examples match the shipped signatures. Per CLAUDE.md §1, **published ADRs are not edited** here
(e.g. `adr/007`) beyond an append-only note if truly required; this step touches consumer docs, not
historical decision records.

## Affected components
- `docs/Hangfire.md` — **new** module doc following the structure in
  [`ClaudeCodingGuide.md` §18](../../../ClaudeCodingGuide.md): purpose, install, `AddPersistentEvents()`
  usage (`services.AddCQRS().AddPersistentEvents()`), the **app-owned bootstrap** requirement
  (`AddHangfire` / `UseXxxStorage` / `AddHangfireServer` / `MapHangfireDashboard` — the plugin does
  **not** wire these), `IEvent` + `IEventHandler<T>` example (`CitySearched`), recurring jobs
  (`IJob` + `AddRecurringJob<TJob>(cron)`), and the **no-automatic-retry** semantics (events dispatch
  once; resilience is a handler concern). The doc must also state
  the two **app-side requirements** surfaced in step 04 (without which the feature fails at runtime):
  1. **A DI-aware Hangfire `JobActivator`** (supplied by `Hangfire.AspNetCore`/`Hangfire.NetCore` via
     `AddHangfireServer()`, or `GlobalConfiguration.UseActivator`). The plugin is `Hangfire.Core`-only;
     the server-side activator is the app's responsibility.
  2. **Type-aware job-argument serialisation** — `IEvent` is serialised through an interface parameter,
     so the app must configure `UseRecommendedSerializerSettings()` (`TypeNameHandling.Auto`) +
     `UseSimpleAssemblyNameTypeSerializer()`; otherwise the event cannot be deserialised when the job
     runs. Note the `Newtonsoft.Json` 13.0.4 pin / CVE-2024-21907 context here too.
  Also document: **no automatic retry** — persistent events dispatch **once**
  (`[AutomaticRetry(Attempts = 0)]`); a failed handler surfaces as a **Failed** job in the dashboard
  (visible / manually re-queueable) instead of being silently swallowed, and resilience is a
  **handler-level** concern (e.g. Polly inside the handler). Persistence still buys **durability** (a job
  enqueued before a crash runs after restart) and **at-least-once** delivery (crash-recovery or a manual
  re-queue can re-run a job) — so handlers must be **idempotent**. Document **rate limit** as **out of
  scope** for this `Hangfire.Core`-only plugin: concurrency is bounded app-side via
  `AddHangfireServer(o => o.WorkerCount = N)` + named queues (true jobs-per-interval rate-limiting needs
  `Hangfire.Throttling`/infra). Then the **`IEvent` payload guideline** — persisted events may be
  re-dispatched (at-least-once), so prefer small/immutable payloads carrying IDs and reload current state
  in the handler; call out `CitySearched` as the deliberate exception that
  carries its full payload (freshly-fetched city not yet in the store, no id to reload by).
- **Keep the plugin/API surface small (explicit ADR-009 goal).** State in `Hangfire.md` that the public
  surface is intentionally minimal — `AddPersistentEvents()`, `AddRecurringJob<TJob>(cron)`, `IEvent`,
  `IEventHandler<T>`, `IJob`, `PersistentEventsOptions` — and that new knobs require an ADR.
- `docs/CQRS.md` — update the **Notifications** section: `INotification` → `IEvent`,
  `INotificationHandler<T>` → `IEventHandler<T>` (currently around lines 90–104), and add a short
  pointer to `Hangfire.md` for the persistent variant. Keep the fire-and-forget description accurate
  for the default (in-memory) publisher.
- `docs/theDesign.md` — update the `SaveCitySearchJob : INotificationHandler<CitySearched>` example
  (around line 52) to `IEventHandler<CitySearched>`.
- `docs/Cron.md` — add a deprecation banner at the top: `SolTechnology.Core.Scheduler` is deprecated
  in favour of `SolTechnology.Core.Hangfire` recurring jobs (link to `Hangfire.md` + ADR-009). Do not
  delete the Scheduler content.
- `SolTechnology.Core.slnx` — add `docs/Hangfire.md` to the `/Docs/` solution folder block (the
  `<Folder Name="/Docs/">` block already exists; mirror the other `docs/*.md` entries).
- `README.md` (repo root) — **review-verified: it DOES carry a module table** (the `Scheduler` row is
  at line ~50, linking `docs/Cron.md` + a NuGet badge). Therefore:
  - **Add a `SolTechnology.Core.Hangfire` row** (link `docs/Hangfire.md` + NuGet badge), mirroring the
    existing rows.
  - **Annotate the `Scheduler` row as deprecated** (e.g. "_(deprecated — see Hangfire)_") so the table
    reflects step 06's obsoletion. This is the README half of the deprecation that step 06 deliberately
    left to the docs step.

## Details
- **Module/doc parity (ClaudeCodingGuide §18).** Every `src/SolTechnology.Core.*` package should have a
  matching `docs/<Module>.md`. This step creates the `Hangfire.md` half of that pair; run the
  [`documentation-cleanup`](../../../../.github/skills/documentation-cleanup/SKILL.md) skill to verify the
  module/doc index, links, and tables resolve.
- The ADR itself (`009-hangfire-persistent-events-and-jobs.md`) and its sibling step files are already
  the durable decision record — **do not duplicate** the rationale into `Hangfire.md`; link to the ADR.
- Markdown hygiene (CLAUDE.md §4): links with spaces use `[Text](<path>)`; verify every link resolves
  on disk; no issue-tracker IDs.
- If step 03 created a placeholder `Hangfire.md` to make `dotnet pack` pass, **replace** it wholesale
  here.
- Do **not** edit `adr/007-cqrs-production-hardening.md`'s `INotification` references — that is a
  historical record (CLAUDE.md §1).
- **ADR index parity (ADR-006 §2).** Flipping ADR-009's status row in `docs/adr/README.md` is **owned by
  the plan close-out**, not this step (maintainer decision 2026-06-10). Do **not** change it here.

## Acceptance criteria
- `docs/Hangfire.md` exists, follows §18 structure, documents the two app-side requirements (DI
  activator; type-aware serializer) and the "keep the surface small" goal, and its examples
  compile-match the step 04–05 signatures (`AddPersistentEvents`, `AddRecurringJob<TJob>`, `IEvent`,
  `IJob`).
- No `INotification` / `INotificationHandler` identifier remains in `docs/CQRS.md` or
  `docs/theDesign.md` (historical ADRs excluded).
- `docs/Cron.md` carries the Scheduler deprecation banner linking to `Hangfire.md` + ADR-009.
- `README.md` has a `Hangfire` module row **and** the `Scheduler` row marked deprecated.
- `docs/Hangfire.md` is registered in `SolTechnology.Core.slnx` `/Docs/`.
- `documentation-cleanup` reports module/doc parity and all links resolving.

## Open questions
- Should `Hangfire.md` include a full DreamTravel before/after migration snippet, or just link to the
  sample app? Default: link + a minimal snippet, full migration lives in step 08.
- **ADR-009 status row — RESOLVED (2026-06-10):** owned by the **plan close-out**, not this step. (Retry
  and rate limit are resolved on step 04: no automatic retry; rate limit out of scope.)


