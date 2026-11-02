---
spec: 2026-11-02-core-2-0-testing-kits
step: 06
status: to-do
---

# Step 06: Guide conformance — testing kits

## Summary
Three testing-kit files exceed the size caps: `HTTP.Testing/WireMock/JsonResponseBuilderDecorator.cs`
(304), `ServiceBus.Testing/ServiceBusFixture.cs` (230), and `SQL.Testing/SQLFixture.cs` (159)
(`http-api#24`, `infra#24`). Three HTTP.Testing file names do not match their type names (Guide
§10), and the kits hand-write constructors, use `static class` + `this`, and have unbraced `if`s
and essay comments (`logging-cache#26`). This is a behaviour-preserving refactor; file names are
aligned with type names, types are unchanged.

Former core steps 65 (HTTP.Testing part), 68 (`ServiceBus.Testing` part), and 69 (testing-kit part) plus the testing-kit-scoped part of former step 70; split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.HTTP.Testing/WireMock/JsonResponseBuilderDecorator.cs` — EDIT — split below 150 lines
- `src/SolTechnology.Core.ServiceBus.Testing/ServiceBusFixture.cs` — EDIT — split below 150
- `src/SolTechnology.Core.SQL.Testing/SQLFixture.cs` — EDIT — below 150 lines (extract engine-specific container setup into an internal file, NEW)
- `src/SolTechnology.Core.Testing/Containers/ContainerLifecycleHelper.cs` (`:15-19`), `src/SolTechnology.Core.Testing/Assertions/ResultAssertions.cs` (`:74`) — EDIT — `extension` blocks, braces, comments
- RENAME files only (types unchanged), each a case-only rename:
  - `src/SolTechnology.Core.HTTP.Testing/Faker/FakeAPIBase.cs` → `FakeApiBase.cs`
  - `src/SolTechnology.Core.HTTP.Testing/Faker/IFakeAPIBuilderWithResponse.cs` → `IFakeApiBuilderWithResponse.cs`
  - `src/SolTechnology.Core.HTTP.Testing/Faker/IFakeAPIBuilderWithRequest.cs` → `IFakeApiBuilderWithRequest.cs`

## Changes
- Case-only renames on macOS/Windows (case-insensitive file systems) need two `git mv` calls (`git mv FakeAPIBase.cs FakeAPIBase.tmp.cs && git mv FakeAPIBase.tmp.cs FakeApiBase.cs`). Verify with `git ls-files`, not `ls`.
- No public or protected signature change; no log-template change.
- Behaviour-preserving refactor; use the [`refactor`](../../../../.github/skills/refactor/SKILL.md) skill.
- Log templates: the scoped `[{Placeholder}]` check below is this library's part of former step 70 (L-03). Fix any unbracketed placeholder it finds and add the property name to the core brief's L-03 entry.

## Acceptance criteria
- [ ] `find src/SolTechnology.Core.Testing src/SolTechnology.Core.HTTP.Testing src/SolTechnology.Core.API.Testing src/SolTechnology.Core.SQL.Testing src/SolTechnology.Core.Redis.Testing src/SolTechnology.Core.Blob.Testing src/SolTechnology.Core.ServiceBus.Testing -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `git ls-files src | grep -E "FakeAPI|IFakeAPI"` prints nothing.
- [ ] `pwsh ./.github/runTests.ps1` is green (Docker).
- [ ] `dotnet test sample-tale-code-apps/DreamTravel/tests/Component/DreamTravel.Component.Tests.csproj` is green (former step 65 criterion for the HTTP.Testing renames and the `JsonResponseBuilderDecorator` split).
- [ ] `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.Testing src/SolTechnology.Core.HTTP.Testing src/SolTechnology.Core.API.Testing src/SolTechnology.Core.SQL.Testing src/SolTechnology.Core.Redis.Testing src/SolTechnology.Core.Blob.Testing src/SolTechnology.Core.ServiceBus.Testing --include=*.cs` prints nothing (the former step 70 repository-wide check, scoped to this library; tune the regex in the PR; `[LoggerMessage]` templates included).
- [ ] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).

## Open questions
- none

## Deviations
