---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 65
status: to-do
---

# Step 65: Guide conformance — Api, Authentication file names, HTTP.Testing

## Summary
Three Api and HTTP.Testing files exceed the size caps: `Api/ModuleInstaller.cs` (264),
`ApiProblemDetailsFactory.cs` (200), and `JsonResponseBuilderDecorator.cs` (304)
(`http-api#24`). Api types hand-write constructor capture and use `static class` + `this`
(`http-api#23,25`). Five file names in Authentication and HTTP.Testing do not match their type
names (Guide §10). This is a behaviour-preserving refactor; file names are aligned with type
names, types are unchanged.

## Affected components
- `src/SolTechnology.Core.API/ModuleInstaller.cs` — EDIT — `BuildInMemorySource` and `ConfigureSwaggerOptions` into their own files (NEW)
- `src/SolTechnology.Core.API/Exceptions/ApiProblemDetailsFactory.cs` — EDIT — below 150 lines (extract title/status resolution into an internal helper file)
- `src/SolTechnology.Core.API/Filters/ExceptionFilter.cs`, `src/SolTechnology.Core.API/Security/SecurityHeadersMiddleware.cs` — EDIT — primary constructors; `:52-61` comment into XML docs
- `src/SolTechnology.Core.API/**/*Extensions.cs` — EDIT — `extension` blocks
- `src/SolTechnology.Core.HTTP.Testing/WireMock/JsonResponseBuilderDecorator.cs` — EDIT — split below 150 lines
- RENAME files only (types unchanged), each a case-only rename:
  - `src/SolTechnology.Core.Authentication/APIKeyAuthenticationHandler.cs` → `ApiKeyAuthenticationHandler.cs`
  - `src/SolTechnology.Core.Authentication/APIKeyAuthenticationSchemeOptions.cs` → `ApiKeyAuthenticationSchemeOptions.cs`
  - `src/SolTechnology.Core.HTTP.Testing/Faker/FakeAPIBase.cs` → `FakeApiBase.cs`
  - `src/SolTechnology.Core.HTTP.Testing/Faker/IFakeAPIBuilderWithResponse.cs` → `IFakeApiBuilderWithResponse.cs`
  - `src/SolTechnology.Core.HTTP.Testing/Faker/IFakeAPIBuilderWithRequest.cs` → `IFakeApiBuilderWithRequest.cs`

## Changes
- Case-only renames on macOS/Windows (case-insensitive file systems) need two `git mv` calls: `git mv APIKeyAuthenticationHandler.cs APIKeyAuthenticationHandler.tmp.cs && git mv APIKeyAuthenticationHandler.tmp.cs ApiKeyAuthenticationHandler.cs`. Verify with `git ls-files`, not `ls`.
- No public or protected signature change. ModuleInstaller edits are mechanical (the premortem `di.md` checklist still applies).

## Acceptance criteria
- [ ] `find src/SolTechnology.Core.API src/SolTechnology.Core.HTTP.Testing src/SolTechnology.Core.API.Testing -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `git ls-files src | grep -E "APIKey|FakeAPI|IFakeAPI"` prints nothing.
- [ ] `dotnet test tests/SolTechnology.Core.API.Tests`, `dotnet test tests/SolTechnology.Core.Authentication.Tests`, and `dotnet test tests/SolTechnology.Core.HTTP.Tests` are green; DreamTravel component tests are green.
- [ ] Public API diff is empty.
- [ ] Premortem mitigations for `ModuleInstaller.cs` are applied.

## Open questions
- none

## Deviations
