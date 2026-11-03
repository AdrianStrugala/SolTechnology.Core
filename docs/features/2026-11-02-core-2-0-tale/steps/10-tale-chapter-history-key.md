---
spec: 2026-11-02-core-2-0-tale
step: 10
status: done
---

# Step 10: Tale — chapter history keyed by step index and full type name

## Summary
History is keyed by `ChapterId = GetType().Name`. A chapter read twice, or two chapters with the
same name in different namespaces, is silently skipped as already `Completed`, even in
non-persisted runs (`cqrs-tale#5`, `Orchestration/TaleEngine.cs:219-223`, `Chapter.cs:28`). The
key changes to `"{stepIndex}:{Type.FullName}"`. This changes a persisted contract (P-03):
in-flight tales saved by 1.x cannot resume. Premortem RM-3 (core step 00) replaces `Type.FullName` in
the key with a version-free type name (see Changes).

Former core step 42; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.Tale/Orchestration/TaleEngine.cs` — EDIT — history lookup by step index + version-free full type name
- `src/SolTechnology.Core.Tale/Models/ChapterInfo.cs` — EDIT — store `StepIndex` as `int?` (new property on an existing public model is additive; nullable per premortem RM-4)
- `src/SolTechnology.Core.Tale/Chapter.cs` — unchanged `ChapterId` (display only; XML doc updated to say so)
- `tests/SolTechnology.Core.Tale.Tests/TaleEngineTests.cs` — EDIT
- `docs/features/2026-11-02-core-2-0-flagship-hardening.md` — EDIT — premortem RM-5: release-notes input

## Changes
- On resume, a `TaleInstance` whose history lacks `StepIndex` fails with a descriptive `Error` ("created by 1.x; restart the tale"), and never mis-skips.
- Tests:
  - `Tell_SameChapterTwice_RunsBoth`.
  - `Tell_SameNameDifferentNamespaces_RunsBoth`.
  - `Resume_1xHistory_FailsWithDescriptiveError`.
- Premortem mitigations (core step 00, 2026-11-02):
  - RM-3 (scenario 3): the type part of the key carries no assembly information. `Type.FullName` of a closed generic embeds `Version=`/`PublicKeyToken=`, so a consumer redeploy would change the key of a paused tale and re-run a completed chapter. Use the same kind of version-free name as cache step 03 (namespace + name, generic arguments recursively without assembly). Test `HistoryKey_GenericChapter_HasNoAssemblyVersion`.
    - Delivery-order note (plan review, 2026-11-02): cache step 03 and hangfire step 04 ship after this feature, so this step implements the rule itself and does not wait for them.
  - RM-4 (scenario 4):
    - `ChapterInfo.StepIndex` is `int?`, so a 1.x entry deserializes as `null`, not `0`.
    - `Resume_1xHistory_FailsWithDescriptiveError` deserializes a `TaleInstance` JSON literal captured at `4097d47f` (status `WaitingForInput`, at least one `Completed` history entry). It reads the literal through `TaleJsonOptions.Default` and through the DreamTravel `SQLiteTaleRepository` read path, not from objects built with 2.x code.
    - The same fixture asserts that the numeric `TaleStatus` values are unchanged from `4097d47f`.
  - RM-5 (scenario 5): add to the core brief's "Release-notes inputs": "Cut Tale hosts over in one step. Stop every 1.x host before a 2.x host serves tale traffic. Do not roll back to 1.x after a 2.x host has written tale state: 2.x writes Tale-error discriminators, `StepIndex`, and `Version` that 1.x cannot read or preserve." The `docs/Tale.md` note is in step 13.

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.Tale.Tests` is green, and the two duplicate tests fail on `4097d47f`.
- [x] The core brief's P-03 row matches the delivered behaviour; release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) copies it into `docs/migration-2.0.md`.
- [x] Premortem mitigations for the persisted contract are applied.
- [x] RM-3: `HistoryKey_GenericChapter_HasNoAssemblyVersion` is green; the key contains none of `Version=`, `Culture=`, or `PublicKeyToken=`.
- [x] RM-4: `ChapterInfo.StepIndex` is `int?`; `Resume_1xHistory_FailsWithDescriptiveError` reads a `4097d47f` JSON literal and is green; the `TaleStatus` numeric assertion is green.
- [x] RM-5: the core brief's "Release-notes inputs" contains the single-cutover and no-rollback note.

## Open questions
- none

## Deviations

### The type part is stored in a second new property, `ChapterInfo.ChapterTypeName`

**Original plan:** History lookup by step index + version-free full type name; `ChapterInfo`
stores `StepIndex` (`int?`).

**Actual implementation:** The lookup needs the type part persisted too, so `ChapterInfo` gained
`ChapterTypeName` (`string?`, additive) next to `StepIndex`; the key is
`"{StepIndex}:{ChapterTypeName}"`. The name is the open generic's `FullName` plus the arguments in
brackets, recursively (for example ``Ns.GenericChapter`1[System.Collections.Generic.List`1[Ns.Input]]``),
built by the internal `ChapterTypeName` helper. An `Otherwise` chapter gets its own step index. The
1.x check lives in `TaleManager.ResumeStory`, which returns the descriptive error before any write,
so the stored 1.x tale is left as it was.

### The `4097d47f` fixture is the AUID step 03 capture; the AUID guard test became the 1.x test

**Original plan:** `Resume_1xHistory_FailsWithDescriptiveError` deserializes a `TaleInstance` JSON
literal captured at `4097d47f`, through `TaleJsonOptions.Default` and through the DreamTravel
`SQLiteTaleRepository` read path; the duplicate tests fail on `4097d47f`.

**Actual implementation:** The literal is the one AUID step 03 captured from `master` just before
the namespace move (status `WaitingForInput`, one `Completed` history entry; its record states the
output equals `4097d47f`, whose Tale sources were unchanged then). The AUID guard test
`ResumeStory_TaleInstanceJsonPersistedBy1x_CompletesWithSameTaleId` asserted that this 1.x tale
completes, which P-03 deliberately ends, so it was replaced by
`PauseResumeIntegrationTests.Resume_1xHistory_FailsWithDescriptiveError` on the same literal: the
`Auid` values still parse, `StepIndex` reads as `null`, the numeric `TaleStatus` values are
unchanged, and the resume fails with the 1.x message. The SQLite read path is covered by the
component test `SampleOrderWorkflowSQLiteTests.Resume_1xHistory_FailsWithDescriptiveError`, which
inserts the literal's columns into the table and reads them with `SQLiteTaleRepository`. The two
duplicate tests and the 1.x test failed on `master` before the change (`8bf4ac2b`). Running the
whole suite during this step exposed a resume race in step 09, fixed in a separate commit (see
step 09's deviations). `RM-5` is in the core brief's release-notes inputs; P-03 there and in this
feature's brief describes the delivered key.
