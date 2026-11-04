---
spec: 2026-11-02-core-2-0-http
step: 07
status: done
---

# Step 07: XML docs shipped — HTTP

## Summary
XML `<summary>` blocks are written but never shipped: only Logging sets `GenerateDocumentationFile`, so NuGet consumers get no IntelliSense (`P1-4`, `cqrs-tale#16`). With `TreatWarningsAsErrors`, enabling it raises `CS1591` for every undocumented public member. This step enables it per project for `SolTechnology.Core.HTTP` and documents every public member. Core step 08 centralizes the switch after every library has shipped its docs. The step runs after this feature's 2.0 cleanup, so no docs are written for members 2.0 removes.

Former core step 56 (HTTP part); split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.HTTP/SolTechnology.Core.HTTP.csproj` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `src/SolTechnology.Core.HTTP/**/*.cs` — EDIT — `<summary>` on every public/protected member reported by `CS1591` (`ModuleInstaller.cs` gets XML doc comments only, no code change)

## Changes
- English, one or two sentences per member, per [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md). No `<NoWarn>CS1591</NoWarn>` (`CLAUDE.md §2`).

## Acceptance criteria
- [x] `dotnet build SolTechnology.Core.slnx` is green with `TreatWarningsAsErrors`.
- [x] `dotnet pack` of the project produces a nupkg containing `lib/net10.0/<Assembly>.xml`.

## Open questions
- none

## Deviations

### Property comments became XML docs

**Original plan:** Add `<summary>` to every member `CS1591` reports.

**Actual implementation:** `HttpPolicyConfiguration`'s undocumented properties carried multi-line `//` rationale comments; they became one- or two-sentence `<summary>` blocks with the defaults, and the history ("the previous 10/3 …") was dropped (Guide §9.10). `ServiceCallErrorMapper`'s ambiguous `HttpClient.SendAsync` cref (`CS0419`) names the overload. `dotnet pack` puts `lib/net10.0/SolTechnology.Core.HTTP.xml` (48 848 bytes) in `SolTechnology.Core.HTTP.2.0.0.nupkg`.
