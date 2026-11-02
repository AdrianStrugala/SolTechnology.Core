---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 58
status: to-do
---

# Step 58: XML docs shipped — testing kits, then one central switch

## Summary
This step documents the remaining packable projects (the `*.Testing` kits). It then moves
`GenerateDocumentationFile` from the 14 per-project settings added in steps 55–57, plus Logging's existing one, into
`src/Directory.Build.props`, so a new package cannot ship without docs. It edits
`Directory.Build.props` (premortem gate).

## Affected components
- `src/SolTechnology.Core.Testing/**/*.cs`, `src/SolTechnology.Core.SQL.Testing/**/*.cs`, `src/SolTechnology.Core.Redis.Testing/**/*.cs`, `src/SolTechnology.Core.Blob.Testing/**/*.cs`, `src/SolTechnology.Core.ServiceBus.Testing/**/*.cs` — EDIT — `<summary>` blocks
- `src/Directory.Build.props` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in the main `PropertyGroup`
- `src/SolTechnology.Core.Logging/SolTechnology.Core.Logging.csproj` — EDIT — delete `:15`
- The 14 csprojs edited in steps 55–57 — EDIT — delete the per-project line
- `src/SolTechnology.Core.Guards/*.csproj`, `src/SolTechnology.Core.Scheduler/*.csproj` — EDIT — `<GenerateDocumentationFile>false</GenerateDocumentationFile>` (the projects stay, Q1)

## Changes
- `grep -rn "GenerateDocumentationFile" src --include=*.csproj` afterwards lists only the two Guards/Scheduler opt-outs.

## Acceptance criteria
- [ ] `dotnet build SolTechnology.Core.slnx` is green.
- [ ] Every `./artifacts/*.nupkg` from the CI pack step contains a `lib/net10.0/*.xml` (`for f in artifacts/*.nupkg; do unzip -l $f | grep -q "\.xml" || echo $f; done` prints nothing).
- [ ] Premortem mitigations for `Directory.Build.props` are applied.

## Open questions
- none

## Deviations
