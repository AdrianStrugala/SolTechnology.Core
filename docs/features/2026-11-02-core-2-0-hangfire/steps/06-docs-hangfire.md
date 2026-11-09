---
spec: 2026-11-02-core-2-0-hangfire
step: 06
status: done
---

# Step 06: Module docs — `docs/Hangfire.md`

## Summary
`docs/Hangfire.md` ships as the NuGet README and uses names that no longer exist:
`AddPersistentEvents`, `AddRecurringJob`, `AddCQRS`, `UseSolTechnologyFilters` (`infra#18-19`). It
contradicts `architecture/background-processing.md` about failed handlers, and invents a
`[SmartRetry]` attribute and a 10-step backoff (`infra#10`). Every C# snippet must compile against
the 2.0 API.

Former core step 61 (Hangfire part); split per library on 2026-11-02.

## Affected components
- `docs/Hangfire.md` — EDIT — `:14,23-38,122-123,162-169,197-201,223-230`; `preventOverlap` works (step 02); `IJob` returns `Result` (step 04, `C-19` confirmed 2026-11-02); job IDs `FullName`, `jobId`, `timeZone`, automatic removal of legacy `Type.Name` IDs (step 04, P-06); 1.x event jobs keep running through the legacy overload until 3.0 (step 03, P-05)

## Changes
- Verification method: copy every C# block into a throwaway console project (not committed) that references `SolTechnology.Core.Hangfire` and `SolTechnology.Core.CQRS`, then run `dotnet build`. Record the result in this step's Deviations if a snippet must stay illustrative (mark it with `// illustrative` in the doc).
- Follow [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md).
- Premortem mitigations (core step 00, 2026-11-02), documentation part, all in `docs/Hangfire.md`:
  - RM-6 (scenario 6): deploy Hangfire servers before processes that only enqueue.
  - RM-8 (scenario 8): the per-application tracked-ID scope for services that share one storage, and deleting the 2.x recurring IDs after a rollback to 1.x.
  - RM-9 (scenario 9): the retry semantics delivered in step 04. A thrown exception is not retried. A recoverable `Result` is retried up to the configured count. Or, if step 04 recorded a deviation, state the explicit count.

## Acceptance criteria
- [x] `grep -nE "\bAdd(MessageBus|ServiceBusHealthCheck|PersistentEvents|RecurringJob|CQRS|SQL|SqlHealthCheck)\b|WithPublisher|WithReceiver|UseSolTechnologyFilters|SmartRetry\(|UntilConditionMet" docs/Hangfire.md` prints nothing.
- [x] Snippets compile in the throwaway project (the PR author states it in the PR description).
- [x] RM-6, RM-8, RM-9: `docs/Hangfire.md` contains the deploy-order note, the shared-storage and rollback note, and the retry semantics that match step 04's tests.

## Open questions
- none

## Deviations

### Page rewritten to the canonical module-page structure

**Original plan:** Edit the listed lines of `docs/Hangfire.md` for the 2.0 names and behaviour.

**Actual implementation:** The page is rewritten in the `PublicDocumentationGuide` structure (Features, Registration,
Configuration, Usage, Testing, Conventions, What ships in DI), as Bus.md and Blob.md were for 2.0, because most sections
described 1.x names or behaviour that never existed (the `[SmartRetry]` attribute, the 10-step back-off, failed
handlers surfacing as failed jobs). The "Rate Limiting", "Worker count", "Database migration" and "Decision note:
Delay-queue vs Hangfire scheduled jobs" sections are reduced to Conventions bullets; their rationale is not 2.0
behaviour and the retrospective decides whether `architecture/background-processing.md` needs it. The page states
the delivered retry matrix (step 04), the overlap lock and its `Deleted` outcome (step 02, Hangfire marks a run a
filter cancels as `Deleted`), shutdown requeueing (step 03 and its follow-up fix), the per-application id scope and the
rollback note (RM-8), the deploy-order note (RM-6), and the 1.x upgrade table (P-05, P-06, `C-19`). The NuGet
`<Description>` of `SolTechnology.Core.Hangfire.csproj` also named `AddPersistentEvents()`; it now names
`AddSolPersistentEvents()`.

Verification: the acceptance-criterion grep prints nothing. All six C# blocks were copied into a throwaway web project
under the git-ignored `.scratch/` folder (not committed) that references `SolTechnology.Core.Hangfire`,
`SolTechnology.Core.CQRS`, `Hangfire.AspNetCore`, `Hangfire.SqlServer`, NUnit, NSubstitute and FluentAssertions;
statements were wrapped in a method and `IEmailSender`/`ITrafficClient` were declared as stand-ins. `dotnet build -c
Release` succeeded with no warnings; no snippet is marked `// illustrative`.
