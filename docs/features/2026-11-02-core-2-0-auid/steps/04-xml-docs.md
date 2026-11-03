---
spec: 2026-11-02-core-2-0-auid
step: 04
status: done
---

# Step 04: XML docs shipped — AUID

## Summary
XML `<summary>` blocks are written but never shipped: only Logging sets `GenerateDocumentationFile`, so NuGet consumers get no IntelliSense (`P1-4`, `cqrs-tale#16`). With `TreatWarningsAsErrors`, enabling it raises `CS1591` for every undocumented public member. This step enables it per project for `SolTechnology.Core.AUID` and documents every public member. Core step 08 centralizes the switch after every library has shipped its docs. The step runs after this feature's 2.0 cleanup, so no docs are written for members 2.0 removes.

Former core step 55 (AUID part); split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.AUID/SolTechnology.Core.AUID.csproj` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `src/SolTechnology.Core.AUID/**/*.cs` — EDIT — `<summary>` on every public/protected member reported by `CS1591`

## Changes
- English, one or two sentences per member, per [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md). No `<NoWarn>CS1591</NoWarn>` (`CLAUDE.md §2`).

## Acceptance criteria
- [x] `dotnet build SolTechnology.Core.slnx` is green with `TreatWarningsAsErrors`.
- [x] `dotnet pack` of the project produces a nupkg containing `lib/net10.0/<Assembly>.xml`.

## Open questions
- none

## Deviations

None material. `CS1591` reported only the four `AuidJsonConverter` overrides; they got summaries, and the `TryParse(string?, …)` return description now names the 2001–2137 range from step 01. `dotnet pack src/SolTechnology.Core.AUID -c Release` produced `SolTechnology.Core.AUID.2.0.0.nupkg` with `lib/net10.0/SolTechnology.Core.AUID.xml`, `docs/readme.md` and `docs/icon.png`.
