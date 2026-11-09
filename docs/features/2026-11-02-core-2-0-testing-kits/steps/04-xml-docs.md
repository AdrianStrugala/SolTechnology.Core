---
spec: 2026-11-02-core-2-0-testing-kits
step: 04
status: done
---

# Step 04: XML docs shipped — Testing, HTTP.Testing, API.Testing, SQL.Testing, Redis.Testing, Blob.Testing, ServiceBus.Testing

## Summary
XML `<summary>` blocks are written but never shipped: only Logging sets `GenerateDocumentationFile`, so NuGet consumers get no IntelliSense (`P1-4`, `cqrs-tale#16`). With `TreatWarningsAsErrors`, enabling it raises `CS1591` for every undocumented public member. This step enables it per project for `SolTechnology.Core.Testing`, `SolTechnology.Core.HTTP.Testing`, `SolTechnology.Core.API.Testing`, `SolTechnology.Core.SQL.Testing`, `SolTechnology.Core.Redis.Testing`, `SolTechnology.Core.Blob.Testing`, `SolTechnology.Core.ServiceBus.Testing` and documents every public member. Core step 08 centralizes the switch after every library has shipped its docs. The step runs after this feature's 2.0 cleanup, so no docs are written for members 2.0 removes. Regroup change (2026-11-02): former step 58 documented `Testing`, `SQL.Testing`, `Redis.Testing`, `Blob.Testing`, and `ServiceBus.Testing` without a per-project switch, because the central switch landed in the same step. The central switch now lands later (core step 08), so this step enables the switch per project for all seven kits.

Former core steps 56 (HTTP.Testing and API.Testing parts) and 58 (testing-kit documentation part); split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.Testing/SolTechnology.Core.Testing.csproj` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `src/SolTechnology.Core.HTTP.Testing/SolTechnology.Core.HTTP.Testing.csproj` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `src/SolTechnology.Core.API.Testing/SolTechnology.Core.API.Testing.csproj` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `src/SolTechnology.Core.SQL.Testing/SolTechnology.Core.SQL.Testing.csproj` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `src/SolTechnology.Core.Redis.Testing/SolTechnology.Core.Redis.Testing.csproj` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `src/SolTechnology.Core.Blob.Testing/SolTechnology.Core.Blob.Testing.csproj` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `src/SolTechnology.Core.ServiceBus.Testing/SolTechnology.Core.ServiceBus.Testing.csproj` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `src/SolTechnology.Core.Testing/**/*.cs`, `src/SolTechnology.Core.HTTP.Testing/**/*.cs`, `src/SolTechnology.Core.API.Testing/**/*.cs`, `src/SolTechnology.Core.SQL.Testing/**/*.cs`, `src/SolTechnology.Core.Redis.Testing/**/*.cs`, `src/SolTechnology.Core.Blob.Testing/**/*.cs`, `src/SolTechnology.Core.ServiceBus.Testing/**/*.cs` — EDIT — `<summary>` on every public/protected member reported by `CS1591`

## Changes
- English, one or two sentences per member, per [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md). No `<NoWarn>CS1591</NoWarn>` (`CLAUDE.md §2`).

## Acceptance criteria
- [x] `dotnet build SolTechnology.Core.slnx` is green with `TreatWarningsAsErrors`.
- [x] `dotnet pack` of each of the 7 projects produces a nupkg containing `lib/net10.0/<Assembly>.xml`.

## Open questions
- none

## Deviations

### Forwarding members use `<inheritdoc />`

**Original plan:** a `<summary>` on every public or protected member that `CS1591` reports.

**Actual implementation:** `CS1591` reported 9 members in Testing (which blocked the dependent kits' builds) and then 77
in the other kits; ServiceBus.Testing had none left after steps 02 and 03. The 45 members of
`JsonResponseBuilderDecorator` that only forward to the wrapped WireMock builder, and the interface implementations
`FakeApiBase.Register`, use `<inheritdoc />`; the two `WithBodyAsJson(object, ...)` overloads that serialize with
`System.Text.Json` get their own summary. Constructors document their parameters with the defaults the code uses.

Verification: the seven kits build in Release with no warnings and `dotnet build SolTechnology.Core.slnx -c Release` is
green (the remaining warnings are pre-existing, in DreamTravel, Tale.Tests and Logging.Tests); `dotnet pack` of each kit
produces `lib/net10.0/<Assembly>.xml`.
