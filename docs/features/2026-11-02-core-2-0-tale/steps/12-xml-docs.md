---
spec: 2026-11-02-core-2-0-tale
step: 12
status: done
---

# Step 12: XML docs shipped — Tale

## Summary
XML `<summary>` blocks are written but never shipped: only Logging sets `GenerateDocumentationFile`, so NuGet consumers get no IntelliSense (`P1-4`, `cqrs-tale#16`). With `TreatWarningsAsErrors`, enabling it raises `CS1591` for every undocumented public member. This step enables it per project for `SolTechnology.Core.Tale` and documents every public member. Core step 08 centralizes the switch after every library has shipped its docs. The step runs after this feature's 2.0 cleanup, so no docs are written for members 2.0 removes.

Former core step 55 (Tale part); split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.Tale/SolTechnology.Core.Tale.csproj` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `src/SolTechnology.Core.Tale/**/*.cs` — EDIT — `<summary>` on every public/protected member reported by `CS1591` (e.g. `Tale/ModuleInstaller.cs:21-25`, `TaleController.cs:279-306`; `ModuleInstaller.cs` gets XML doc comments only, no code change)

## Changes
- English, one or two sentences per member, per [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md). No `<NoWarn>CS1591</NoWarn>` (`CLAUDE.md §2`).

## Acceptance criteria
- [x] `dotnet build SolTechnology.Core.slnx` is green with `TreatWarningsAsErrors`.
- [x] `dotnet pack` of the project produces a nupkg containing `lib/net10.0/<Assembly>.xml`.

## Open questions
- none

## Deviations

### Verification record

**Original plan:** `GenerateDocumentationFile` on and every `CS1591` member documented.

**Actual implementation:** As planned: 33 `CS1591`/`CS1573` findings were fixed, plus one `CS1574`
(the `TaleOptions.TaleIdPrefix` comment still pointed at the pre-2.0 global `Auid`). The
`InMemoryTaleRepository` members use `<inheritdoc />` from `ITaleRepository`. `dotnet pack
src/SolTechnology.Core.Tale -c Release` produced `SolTechnology.Core.Tale.2.0.0.nupkg` with
`lib/net10.0/SolTechnology.Core.Tale.xml`, `docs/readme.md` and `docs/icon.png`.
