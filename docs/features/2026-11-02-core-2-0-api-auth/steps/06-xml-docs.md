---
spec: 2026-11-02-core-2-0-api-auth
step: 06
status: to-do
---

# Step 06: XML docs shipped — API, Authentication

## Summary
XML `<summary>` blocks are written but never shipped: only Logging sets `GenerateDocumentationFile`, so NuGet consumers get no IntelliSense (`P1-4`, `cqrs-tale#16`). With `TreatWarningsAsErrors`, enabling it raises `CS1591` for every undocumented public member. This step enables it per project for `SolTechnology.Core.API`, `SolTechnology.Core.Authentication` and documents every public member. Core step 08 centralizes the switch after every library has shipped its docs. The step runs after this feature's 2.0 cleanup, so no docs are written for members 2.0 removes.

Former core step 56 (Api and Authentication parts); split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.API/SolTechnology.Core.API.csproj` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `src/SolTechnology.Core.Authentication/SolTechnology.Core.Authentication.csproj` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `src/SolTechnology.Core.API/**/*.cs`, `src/SolTechnology.Core.Authentication/**/*.cs` — EDIT — `<summary>` on every public/protected member reported by `CS1591` (XML doc comments only in both `ModuleInstaller.cs` files)

## Changes
- English, one or two sentences per member, per [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md). No `<NoWarn>CS1591</NoWarn>` (`CLAUDE.md §2`).

## Acceptance criteria
- [ ] `dotnet build SolTechnology.Core.slnx` is green with `TreatWarningsAsErrors`.
- [ ] `dotnet pack` of each of the 2 projects produces a nupkg containing `lib/net10.0/<Assembly>.xml`.

## Open questions
- none

## Deviations
