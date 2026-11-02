---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 57
status: to-do
---

# Step 57: XML docs shipped — Cache, MessageBus, SQL, Blob, Hangfire

## Summary
This step continues step 55 for the infrastructure packages. The review lists types with no XML
docs at all: `QueueConfiguration`, `MessageBusConfiguration`, `IMessagePublisher`,
`IMessageHandler`, MessageBus `ModuleInstaller.*`, `SQLConfiguration`, `ISQLConnectionFactory`,
`BlobConfiguration`, `IBlobConnectionFactory`, `SerializationFormat`, and
`BlobContainerClientExtensions` (`infra` semver notes).

## Affected components
- `src/SolTechnology.Core.Cache/SolTechnology.Core.Cache.csproj` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `src/SolTechnology.Core.MessageBus/SolTechnology.Core.MessageBus.csproj` — EDIT — same
- `src/SolTechnology.Core.SQL/SolTechnology.Core.SQL.csproj` — EDIT — same
- `src/SolTechnology.Core.Blob/SolTechnology.Core.Blob.csproj` — EDIT — same
- `src/SolTechnology.Core.Hangfire/SolTechnology.Core.Hangfire.csproj` — EDIT — same
- `src/SolTechnology.Core.Cache/**/*.cs`, `src/SolTechnology.Core.MessageBus/**/*.cs`, `src/SolTechnology.Core.SQL/**/*.cs`, `src/SolTechnology.Core.Blob/**/*.cs`, `src/SolTechnology.Core.Hangfire/**/*.cs` — EDIT — `<summary>` blocks

## Changes
- Same rules as step 55.

## Acceptance criteria
- [ ] `dotnet build SolTechnology.Core.slnx` is green.
- [ ] `dotnet pack` of each of the 5 projects contains the `.xml` doc file.

## Open questions
- none

## Deviations
