---
spec: 2026-11-02-core-2-0-testing-kits
step: 06
status: done
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
- [x] `find src/SolTechnology.Core.Testing src/SolTechnology.Core.HTTP.Testing src/SolTechnology.Core.API.Testing src/SolTechnology.Core.SQL.Testing src/SolTechnology.Core.Redis.Testing src/SolTechnology.Core.Blob.Testing src/SolTechnology.Core.ServiceBus.Testing -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [x] `git ls-files src | grep -E "FakeAPI|IFakeAPI"` prints nothing.
- [x] `pwsh ./.github/runTests.ps1` is green (Docker).
- [x] `dotnet test sample-tale-code-apps/DreamTravel/tests/Component/DreamTravel.Component.Tests.csproj` is green (former step 65 criterion for the HTTP.Testing renames and the `JsonResponseBuilderDecorator` split).
- [x] `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.Testing src/SolTechnology.Core.HTTP.Testing src/SolTechnology.Core.API.Testing src/SolTechnology.Core.SQL.Testing src/SolTechnology.Core.Redis.Testing src/SolTechnology.Core.Blob.Testing src/SolTechnology.Core.ServiceBus.Testing --include=*.cs` prints nothing (the former step 70 repository-wide check, scoped to this library; tune the regex in the PR; `[LoggerMessage]` templates included).
- [x] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).

## Open questions
- none

## Deviations

### Splits use partial files and one collaborator

**Original plan:** split `JsonResponseBuilderDecorator` (304) and `ServiceBusFixture` (230) below 150; take
`SQLFixture` (159) below 150 by extracting the engine-specific container setup into an internal file.

**Actual implementation:** after step 04's XML docs the decorator had 359 lines. It is a `partial` class across
`JsonResponseBuilderDecorator.cs` (pipeline, callbacks, delays, transformers: 123), `.Body.cs` (127) and `.Headers.cs`
(headers, status codes, proxy: 122), the Guide §9.9 partial-file form; the type is unchanged. `ServiceBusFixture`
(already 179 after step 02 moved the reuse helpers into `ReusedEmulator`) hands the emulator, sidecar and network to
the new internal `OwnedEmulator` (76) and drops to 127. In `SQLFixture` the engine selection is a four-line switch, so
extracting it would not reach the cap; the fluent configuration methods move into the partial
`SQLFixture.Configuration.cs` (59) instead, and `SQLFixture.cs` has 101 lines.

### Conformance fixes beyond the two named files

**Original plan:** `ContainerLifecycleHelper.cs` (`:15-19`) and `ResultAssertions.cs` (`:74`) get extension blocks, braces
and comments; the kits hand-write constructors and use `static class` + `this`.

**Actual implementation:** `ContainerLifecycleHelper` has no extension methods; its five-line AMQP comment is one line
now. `ResultAssertions` and `InMemorySinkAssertions` (Testing) and `AuthClientExtensions` (API.Testing) move into
`extension` blocks. `ResultAssertions.ShouldBeSuccess<T>(this Result<T>)` stays a classic method with a one-line why,
because its type parameter is unconstrained (Guide §9.7 exception). `FormatError` gets braces. Primary constructors
replace the hand-written ones in `SQLFixture`, `RedisFixture`, `AzuriteFixture`, `ServiceBusFixture`,
`BogusCustomization<T>` and `JsonResponseBuilderDecorator`; the constructor parameter docs move to the type. The
three-line comments in `FakeApiBase` and `ServiceBusInstanceBuilder` are one line, and `AzuriteFixture`'s comment no
longer mentions a static flag that does not exist.

### MessageBus `ModuleInstallerTests` split (messagebus follow-up absorbed)

`tests/SolTechnology.Core.MessageBus.Tests/ModuleInstallerTests.cs` had 222 lines. The three endpoint-registration
tests (`WithEndpoints_*`, `WithQueueReceiver_*`) and their message and handler types move to
`ModuleInstallerEndpointTests.cs` (103); `ModuleInstallerTests.cs` keeps registration, validation and the shared
client (135). Test bodies are unchanged.

### Public API diff

`apicompat --strict-mode` (`Microsoft.DotNet.ApiCompat.Tool` 10.0.403, both directions) compared the Release
assemblies of the seven kits before and after this step: HTTP.Testing, SQL.Testing, Redis.Testing, Blob.Testing and
ServiceBus.Testing show no difference. Testing and API.Testing show only the compiler-generated `extension` marker types
(`CP0001`) and, for `ShouldBeFailure<TError>` (`where TError : Error`), `CP0021` "notnull", which Guide §9.7 accepts. The
`[{Placeholder}]` grep prints nothing: the kits have no logger calls.

Verification: the size, file-name and template greps print nothing. `dotnet test SolTechnology.Core.slnx -c Release`
(the projects `runTests.ps1` walks, plus the DreamTravel test projects in the solution) passes 922 tests with 11
skipped and none failed (baseline before this feature: 917 passed, 11 skipped); the DreamTravel component tests pass
31/31. `docker ps -a` shows no container or network left by the run.
