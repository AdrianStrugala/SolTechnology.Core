---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 55
status: to-do
---

# Step 55: XML docs shipped — Core, CQRS, Tale, AUID

## Summary
XML `<summary>` blocks are written but never shipped: only Logging sets
`GenerateDocumentationFile`, so NuGet consumers get no IntelliSense (`P1-4`, `cqrs-tale#16`).
With `TreatWarningsAsErrors`, enabling it raises `CS1591` for every undocumented public member.
This step enables it per project for the first four packages and documents every public member.
Step 58 centralizes the switch. The step runs after phase F, so no docs are written for members
2.0 removes.

## Affected components
- `src/SolTechnology.Core/SolTechnology.Core.csproj` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `src/SolTechnology.Core.CQRS/SolTechnology.Core.CQRS.csproj` — EDIT — same
- `src/SolTechnology.Core.Tale/SolTechnology.Core.Tale.csproj` — EDIT — same
- `src/SolTechnology.Core.AUID/SolTechnology.Core.AUID.csproj` — EDIT — same
- `src/SolTechnology.Core/**/*.cs`, `src/SolTechnology.Core.CQRS/**/*.cs`, `src/SolTechnology.Core.Tale/**/*.cs`, `src/SolTechnology.Core.AUID/**/*.cs` — EDIT — `<summary>` on every public/protected member reported by `CS1591` (e.g. `Result.cs:11-43`, `Error.cs:24-26`, `ICommandHandler.cs:8`, `Tale/ModuleInstaller.cs:21-25`, `TaleController.cs:279-306`)

## Changes
- English, one or two sentences per member, per [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md). No `<NoWarn>CS1591</NoWarn>` (`CLAUDE.md §2`).

## Acceptance criteria
- [ ] `dotnet build SolTechnology.Core.slnx` is green with `TreatWarningsAsErrors`.
- [ ] `dotnet pack` of each of the 4 projects produces a nupkg containing `lib/net10.0/<Assembly>.xml`.

## Open questions
- none

## Deviations
