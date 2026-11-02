---
spec: 2026-11-02-core-2-0-tale
step: 12
status: to-do
---

# Step 12: XML docs shipped — Tale

## Summary
XML `<summary>` blocks are written but never shipped: only Logging sets `GenerateDocumentationFile`, so NuGet consumers get no IntelliSense (`P1-4`, `cqrs-tale#16`). With `TreatWarningsAsErrors`, enabling it raises `CS1591` for every undocumented public member. This step enables it per project for `SolTechnology.Core.Tale` and documents every public member. Core step 08 centralizes the switch after every library has shipped its docs. The step runs after this feature's 2.0 cleanup, so no docs are written for members 2.0 removes.

Former core step 55 (Tale part); split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.Tale/SolTechnology.Core.Tale.csproj` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `src/SolTechnology.Core.Tale/**/*.cs` — EDIT — `<summary>` on every public/protected member reported by `CS1591` (e.g. `Tale/ModuleInstaller.cs:21-25`, `TaleController.cs:279-306`; XML doc comments only in `ModuleInstaller.cs`)

## Changes
- English, one or two sentences per member, per [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md). No `<NoWarn>CS1591</NoWarn>` (`CLAUDE.md §2`).

## Acceptance criteria
- [ ] `dotnet build SolTechnology.Core.slnx` is green with `TreatWarningsAsErrors`.
- [ ] `dotnet pack` of the project produces a nupkg containing `lib/net10.0/<Assembly>.xml`.

## Open questions
- none

## Deviations
