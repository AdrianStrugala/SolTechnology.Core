---
spec: 2026-11-02-core-2-0-messagebus
step: 05
status: done
---

# Step 05: XML docs shipped — MessageBus

## Summary
XML `<summary>` blocks are written but never shipped: only Logging sets `GenerateDocumentationFile`, so NuGet consumers get no IntelliSense (`P1-4`, `cqrs-tale#16`). With `TreatWarningsAsErrors`, enabling it raises `CS1591` for every undocumented public member. This step enables it per project for `SolTechnology.Core.MessageBus` and documents every public member. Core step 08 centralizes the switch after every library has shipped its docs. The step runs after this feature's 2.0 cleanup, so no docs are written for members 2.0 removes.

Former core step 57 (MessageBus part); split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.MessageBus/SolTechnology.Core.MessageBus.csproj` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `src/SolTechnology.Core.MessageBus/**/*.cs` — EDIT — `<summary>` on every public/protected member reported by `CS1591` (the review lists `QueueConfiguration`, `MessageBusConfiguration`, `IMessagePublisher`, `IMessageHandler`, and `ModuleInstaller.*` with no XML docs at all; `ModuleInstaller.cs` gets XML doc comments only, no code change)

## Changes
- English, one or two sentences per member, per [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md). No `<NoWarn>CS1591</NoWarn>` (`CLAUDE.md §2`).

## Acceptance criteria
- [x] `dotnet build SolTechnology.Core.slnx` is green with `TreatWarningsAsErrors`.
- [x] `dotnet pack` of the project produces a nupkg containing `lib/net10.0/<Assembly>.xml`.

## Open questions
- none

## Deviations

None. `CS1591` listed 14 members (`MessageBusConfiguration`, `.Queues`, `QueueConfiguration` and its two properties,
`IMessagePublisher`, `IMessageHandler<TMessage>` and `Handle`, `ModuleInstaller` and its five methods); each now has a
`<summary>`. Two existing summaries were corrected to the 2.0 behaviour: `ConnectionString` is required (P-24, no
"Managed Identity (future)") and `CreateResources` tolerates concurrent creation. `dotnet pack -c Release` produces
`SolTechnology.Core.MessageBus.2.0.0.nupkg` with `lib/net10.0/SolTechnology.Core.MessageBus.xml` and the package icon
from `docs/icons/SolTechnology.Core.MessageBus.png`. The full suite was green after one re-run of
`SolTechnology.Core.SQL.Tests`, whose SQL Server container failed with `FAIL_PAGE_ALLOCATION` under emulation.

