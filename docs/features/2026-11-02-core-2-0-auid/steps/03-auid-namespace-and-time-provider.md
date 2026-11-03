---
spec: 2026-11-02-core-2-0-auid
step: 03
status: done
---

# Step 03: AUID — `SolTechnology.Core.AUID` namespace; no process-wide `TimeProvider`

## Summary
`Auid`, `AuidTypeConverter`, and `AuidJsonConverter` live in the global namespace, which pollutes
every consumer (`logging-cache#12`, `Auid.cs:14,441`, `AuidJsonConverter.cs:8`).
`Auid.TimeProvider` is a mutable process-wide static that any library can change and that makes
parallel tests interfere (`logging-cache#10`, `:40`, Guide §9.6). This step implements `C-08` and
`C-09`.

Former core step 44; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.AUID/Auid.cs` — EDIT — `namespace SolTechnology.Core.AUID;`; delete `TimeProvider`; NEW `New(string? code, TimeProvider timeProvider)` and `New<T>(TimeProvider timeProvider)`; `AuidTypeConverter` sealed
- `src/SolTechnology.Core.AUID/AuidJsonConverter.cs` — EDIT — namespace; sealed
- `src/SolTechnology.Core.Tale/**`, `src/SolTechnology.Core.*/**` — EDIT — `using SolTechnology.Core.AUID;` where `Auid` is used (`grep -rln "\bAuid\b" src`)
- `sample-tale-code-apps/DreamTravel/**` — EDIT — same usings
- `tests/SolTechnology.Core.AUID.Tests/AuidNewTests.cs` — EDIT — the overflow test uses `FakeTimeProvider` through the overload
- `CLAUDE.md` — EDIT — `§11` AUID gotcha row mentions the namespace

## Changes
- `Auid.New(code)` keeps using `TimeProvider.System`.
- The overflow test keeps the Polish joke message verbatim: its comment quoting "Papież Polak robi BUM" stays, and any new message assertion uses the unchanged `Auid.cs:186` text (Guide §9.10; user, 2026-11-03).
- Persisted-contract check (premortem trigger): `Auid` values inside persisted Tale contexts serialize through `AuidJsonConverter` as strings, so the namespace move does not change stored JSON. Verify that no persisted field stores an assembly-qualified type name containing `Auid` (`grep -rn "AssemblyQualifiedName\|FullName" src/SolTechnology.Core.Tale`); if one does, stop and record it for the migration guide.
- `docs/AUID.md` usings are updated in step 05.

## Acceptance criteria
- [x] `dotnet build SolTechnology.Core.slnx` is green, including DreamTravel.
- [x] `dotnet test tests/SolTechnology.Core.AUID.Tests` and `dotnet test tests/SolTechnology.Core.Tale.Tests` are green.
- [x] `grep -n "static TimeProvider" src/SolTechnology.Core.AUID/Auid.cs` prints nothing.
- [x] A Tale instance JSON captured at `4097d47f` that contains an `Auid` still deserializes (test in `tests/SolTechnology.Core.Tale.Tests`).
- [x] Premortem mitigations for the persisted contract are applied.

## Open questions
- none (`C-08`, `C-09` confirmed by the user on 2026-11-02)

## Deviations

### Tale JSON captured from the pre-step code, not from a `4097d47f` checkout

**Original plan:** The guard test deserializes a Tale instance JSON captured at `4097d47f`.

**Actual implementation:** Checking out `4097d47f` (worktree or `git archive`) was not permitted in this session, so the JSON was captured from `master` right before this step's code change. That output is the same as at `4097d47f`: `git diff --stat 4097d47f -- src/SolTechnology.Core.Tale` touches only the csproj, `AuidJsonConverter.Write` is unchanged, and steps 01–02 changed only culture handling and invalid-input handling. The captured paused `OrderProcessingTale` instance holds the `Auid` twice (`TaleId` and `Context.TaleInstanceId`). `PauseResumeIntegrationTests.ResumeStory_TaleInstanceJsonPersistedBy1x_CompletesWithSameTaleId` stores it, resumes it to `Completed`, and checks both IDs. It passed before and after the namespace move. Persisted-contract check: `grep -rn "AssemblyQualifiedName\|FullName" src/SolTechnology.Core.Tale` prints nothing, so there was nothing to record for the migration guide.

### `New(string? code, TimeProvider)` with a null code uses `XXX`

**Original plan:** NEW `Auid.New(string? code, TimeProvider timeProvider)`; the behaviour for a null code was not specified.

**Actual implementation:** The confirmed signature has no `[CallerFilePath]` parameter, so a null code cannot be inferred from the caller file. It falls back to `XXX`, the same code `New()` uses when the caller path is empty. Both new overloads throw `ArgumentNullException` for a null provider. Explicit-code validation moved into a private `CreateFromCode` (now with braces) that both `New(string?, …)` overloads share.

### Test package and extra test

**Original plan:** The overflow test uses `FakeTimeProvider` through the overload.

**Actual implementation:** `SolTechnology.Core.AUID.Tests` references `Microsoft.Extensions.TimeProvider.Testing` (versionless; 10.7.0 is already in `Directory.Packages.props`), and the package-management canonical table lists the new consumer. The overflow test keeps its comments, including the "Papież Polak robi BUM" quote, and now asserts the `InvalidOperationException` with that message. A new `New_WithTimeProvider_UsesItsClock` covers both overloads and the null-code fallback. Usings were added in 9 Tale source files, 4 Tale test files, and 7 DreamTravel files; the DreamTravel OpenAPI snapshot did not change.
