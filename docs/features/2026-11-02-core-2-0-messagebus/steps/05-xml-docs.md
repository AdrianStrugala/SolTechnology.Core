---
spec: 2026-11-02-core-2-0-messagebus
step: 05
status: to-do
---

# Step 05: XML docs shipped — MessageBus

## Summary
XML `<summary>` blocks are written but never shipped: only Logging sets `GenerateDocumentationFile`, so NuGet consumers get no IntelliSense (`P1-4`, `cqrs-tale#16`). With `TreatWarningsAsErrors`, enabling it raises `CS1591` for every undocumented public member. This step enables it per project for `SolTechnology.Core.MessageBus` and documents every public member. Core step 08 centralizes the switch after every library has shipped its docs. The step runs after this feature's 2.0 cleanup, so no docs are written for members 2.0 removes.

Former core step 57 (MessageBus part); split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.MessageBus/SolTechnology.Core.MessageBus.csproj` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `src/SolTechnology.Core.MessageBus/**/*.cs` — EDIT — `<summary>` on every public/protected member reported by `CS1591` (the review lists `QueueConfiguration`, `MessageBusConfiguration`, `IMessagePublisher`, `IMessageHandler`, and `ModuleInstaller.*` with no XML docs at all; XML doc comments only in `ModuleInstaller.cs`)

## Changes
- English, one or two sentences per member, per [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md). No `<NoWarn>CS1591</NoWarn>` (`CLAUDE.md §2`).

## Acceptance criteria
- [ ] `dotnet build SolTechnology.Core.slnx` is green with `TreatWarningsAsErrors`.
- [ ] `dotnet pack` of the project produces a nupkg containing `lib/net10.0/<Assembly>.xml`.

## Open questions
- none

## Deviations
