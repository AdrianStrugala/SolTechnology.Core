---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 71
status: to-do
---

# Step 71: Retrospective

## Summary
This step closes the feature with the [`implement-plan`](../../../../.github/skills/implement-plan/SKILL.md)
retrospective transaction. It verifies the delivered behaviour, brings `docs/architecture/` to the
current 2.0 state of `master`, completes the durable feature brief, and deletes this working folder. It runs
only when every other step is `done`, or was removed by the plan-reviewer.

## Affected components
- `docs/architecture/authentication.md` — EDIT — constant-time comparison; `ApiKey` property
- `docs/architecture/background-processing.md` — EDIT — `preventOverlap` implemented; correlation restore; `IJob` contract; job IDs; event dispatch cancellation
- `docs/architecture/cqrs.md` — EDIT — missing-handler error; failed `Result` logging; dispatch delegates
- `docs/architecture/tale-framework.md` — EDIT — persistence scope; per-run state; history key; optimistic concurrency; HTTP opt-in; chapter cancellation
- `docs/architecture/http-client.md` — EDIT — unsafe-verb retry rule on exceptions; URI-free messages; factory-based health check; namespace
- `docs/architecture/testing.md` — EDIT — DI resolution tests per module; Ryuk; strict WireMock; Blob has no test project (accepted risk AR-1)
- `docs/architecture/package-release.md` — EDIT — version 2.0.0 on `master` for all packages (not yet published; release step 05 of [`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md) publishes it); tag-only publishing via `nuget-release`; CI fails on any test-project failure; Guards/Scheduler kept as deprecated, non-packable projects; `GenerateDocumentationFile` central. Trusted Publishing, authorship, repository URL, and the nuget.org organization are left to that feature's retrospective
- `docs/architecture/naming-and-public-api.md` — EDIT — 2.0 `Async`-suffix outcome; internal implementation types; additive `TaleJsonOptions` and `MaxDeliveryCount` (`C-26`, `C-27`)
- `docs/architecture/core-result-and-errors.md` — NEW — `Result`/`Result<T>` immutability, `Error` open polymorphism with nearest-ancestor fallback, Tale discriminators through `TaleJsonOptions.Default` (A1, P-01, `C-02`)
- `docs/architecture/infrastructure-modules.md` — NEW — SQL (`IUnitOfWork.Begin`, transient-only retry, safe DACPAC defaults, internal implementations; A4, `C-15`–`C-17`), MessageBus (factory-registered broker, keyed shared client, settlement and `MaxDeliveryCount`; A5, P-17, `C-14`, `C-27`), Blob (single-call metadata upload, AR-1; `C-18`)
- `docs/architecture/cache.md` — NEW — type-namespaced keys, validated `InstanceName`, single-flight local cache, local lock expiry (P-07, `C-13`)
- `docs/architecture/logging.md` — NEW — one registration path for both `AddSolLogging` overloads, correlation-ID sanitization, one Error entry per failure, `[{Placeholder}]` templates, CQRS-owned `ActivitySource` on `OperationsName` (P-14, P-15, L-01…L-05, `C-20`)
- `docs/architecture/auid.md` — NEW — namespace, culture-invariant format, range-checked parse, `TimeProvider` overloads, collision bound and ordering (`C-08`, `C-09`, P-08)
- `docs/architecture/README.md` — EDIT — index entries for the five new pages
- `docs/features/2026-11-02-core-2-0-flagship-hardening.md` — EDIT — `Completion summary`, `Deviations`, `Follow-ups`, `status: completed`, `completed: YYYY-MM-DD`
- `docs/features/2026-11-02-core-2-0-flagship-hardening/` — DELETE

## Changes
1. Review every step against delivered code. Run final validation: `dotnet build SolTechnology.Core.slnx -c Release`, `pwsh ./.github/runTests.ps1`, the DreamTravel tests, and the `NU190[1-4]|NU1605` grep.
2. Update each affected `docs/architecture/*.md` page above, and create the five new pages, to current behaviour and rationale. Copy the A1–A7, A9, and A10 rationale that shipped from the brief into those pages (A8 belongs to the release feature); the brief's design-alternatives table stays as the historical record.
3. Complete the feature brief with outcomes, material deviations from each step, and follow-ups. The `Completion summary` must also record, because the split-off features read them after this working folder is deleted:
   - the owner-confirmed dates from step 09 (revocation of both zip keys, the four history Google keys, and the `talecodeservicebus` SAS key) — release step 01 pre-checks them;
   - the owner-confirmed date that environment `nuget-release` exists with required reviewers and the `v*` tag rule (step 03) — release steps 01 and 04 re-check it after the transfer;
   - any deviation that changes a `C-nn`, `P-nn`, or `L-nn` row or the "Release-notes inputs" section — release step 05 writes the 2.0 release notes and migration guide from them.

   Candidate follow-ups:
   - AOT annotations.
   - CQRS → Logging dependency split.
   - Async `CreateConnection`.
   - `IncludeResponseBodyInException` per client.
   - A `*.Testing` test project.
   - A Blob test project covering steps 16 and 50 (AR-1, Q6).
   - Guards/Scheduler: reference-only, unmaintained; known defects and eventual removal; Guards.Tests out of the slnx and CI, still on xUnit (AR-2, Q1, RQ4).
   - Remove the `System.Security.Cryptography.Pkcs` override from `SolTechnology.Core.SQL` and `SQL.Testing` when `Microsoft.Data.SqlClient` depends on a patched Pkcs (step 01, RQ1).
   - Remove the legacy one-argument `HangfireEventPublisher.DispatchInScope(IEvent)` overload in 3.0 (step 32, RQ10).
   - The outdated-package sweep.
   - AUID monotonic counter.
   - JWT feature re-plan: `2026-07-06-jwt-bearer-authentication` steps 02 and 04 are superseded by steps 07 and 59 here (Q5).
4. Set feature status to `completed` (or `abandoned`) with `completed: YYYY-MM-DD` (`CLAUDE.md §2`: confirm with the user first).
5. Verify that no durable link points into this working folder: `grep -rn "2026-11-02-core-2-0-flagship-hardening/" --include=*.md . | grep -v "^./docs/features/2026-11-02-core-2-0-flagship-hardening/"` prints nothing.
6. Delete the working folder, then search Markdown for the deleted path and repair dangling links.

## Acceptance criteria
- [ ] Every feature-level acceptance criterion in the brief is checked or recorded as a deviation.
- [ ] The documentation-cleanup skill passes for `docs/architecture/` and the feature brief.
- [ ] The working folder no longer exists, and the step 5 grep prints nothing.

## Open questions
- none

## Deviations
