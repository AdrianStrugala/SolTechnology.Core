---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 08
status: done
---

# Step 08: XML docs — one central `GenerateDocumentationFile` switch

## Summary
Every library feature enables `GenerateDocumentationFile` per project in its XML-docs step. This
step moves the 20 per-project settings (19 added by the library features plus Logging's existing
one) into `src/Directory.Build.props`, so a new package cannot ship without docs. It edits
`Directory.Build.props` (premortem gate).

Former core step 58 (central-switch part); split per library on 2026-11-02. The testing-kit documentation part is testing-kits step 04.

## Affected components
- `src/Directory.Build.props` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in the main `PropertyGroup`
- `src/SolTechnology.Core.Logging/SolTechnology.Core.Logging.csproj` — EDIT — delete `:15`
- The 19 csprojs edited by the library XML-docs steps — EDIT — delete the per-project line
- `src/SolTechnology.Core.Guards/*.csproj`, `src/SolTechnology.Core.Scheduler/*.csproj` — EDIT — `<GenerateDocumentationFile>false</GenerateDocumentationFile>` (the projects stay, Q1)

## Changes
- Prerequisite: the XML-docs steps of every library feature are merged: result step 03, auid step 04, cqrs step 04, tale step 12, http step 07, api-auth step 07, cache step 05, sql step 05, blob step 03, messagebus step 05, hangfire step 05, testing-kits step 04.
- `grep -rn "GenerateDocumentationFile" src --include=*.csproj` afterwards lists only the two Guards/Scheduler opt-outs.

## Acceptance criteria
- [x] `dotnet build SolTechnology.Core.slnx` is green.
- [x] Every `./artifacts/*.nupkg` from the CI pack step contains a `lib/net10.0/*.xml` (`for f in artifacts/*.nupkg; do unzip -l $f | grep -q "\.xml" || echo $f; done` prints nothing).
- [x] Premortem mitigations for `Directory.Build.props` are applied.

## Open questions
- none

## Deviations

### Verified with a local pack instead of the CI artifacts

**Original plan:** check the `./artifacts/*.nupkg` produced by the CI pack step for any `.xml`.

**Actual implementation:** the CI pack loop was reproduced locally (`dotnet pack -c Release` for
every `src` project whose `IsPackable` is not `false`), and the check was made stricter: each of
the 20 packages contains `lib/net10.0/<PackageId>.xml`, and each still has its `.snupkg` (RM-18).
`dotnet build SolTechnology.Core.slnx -c Release --no-incremental` gave the same 104 warnings
before and after (identical set, no `CS1591`). Test projects, DreamTravel, Guards, and Scheduler
evaluate `GenerateDocumentationFile` as `false`. The premortem has no `RM-n` row for this step;
the `Directory.Build.props` mitigations RM-1 (lockstep targets) and RM-18 (`IncludeSymbols`) are
untouched. Logging keeps its own `CS1591` `NoWarn`; removing it is outside this step.
