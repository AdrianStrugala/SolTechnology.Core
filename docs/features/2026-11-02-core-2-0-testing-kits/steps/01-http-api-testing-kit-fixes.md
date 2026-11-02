---
spec: 2026-11-02-core-2-0-testing-kits
step: 01
status: to-do
---

# Step 01: API and HTTP testing kits — dispose the test host; strict WireMock by default

## Summary
Two defects in the API and HTTP testing kits:

- `APIFixture` creates a local `WebApplicationFactory` that is never disposed, so hosts and their
  `IHostedService`s leak across fixtures (`http-api#10`, `APIFixture.cs:27,49-53`).
- `WireMockStartup` defaults to `AllowPartialMapping = true`, so requests with a wrong query or
  body still match and client bugs pass silently (`http-api#21`, `WireMockStartup.cs:36`, P-11).

Former core step 36; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.API.Testing/APIFixture.cs` — EDIT — store the factory in a field and dispose it in `Dispose()`
- `src/SolTechnology.Core.HTTP.Testing/WireMockStartup.cs` — EDIT — `AllowPartialMapping = false`; keep the opt-in through `Run(settings)`
- `tests/SolTechnology.Core.HTTP.Tests/` — EDIT — the tests that consume `WireMockStartup` still pass under strict matching (fix the stubs, not the default)
- `sample-tale-code-apps/DreamTravel/tests/Component/` — EDIT — only the stubs that relied on partial matching
- `tests/SolTechnology.Core.API.Tests/SolTechnology.Core.API.Tests.csproj` — EDIT — `ProjectReference` to `src/SolTechnology.Core.API.Testing`
- `tests/SolTechnology.Core.API.Tests/APIFixtureDisposalTests.cs` — NEW — the disposal test and its minimal test entry point (`APIFixture<TEntryPoint>` needs a `WebApplication` entry type in the test project)
- `tests/SolTechnology.Core.Tests/TestHostContainmentGuardTests.cs` — EDIT — `:19-27` add `"APIFixtureDisposalTests.cs"` to `ApprovedFiles` with the comment `// Verifies APIFixture disposal itself`

## Changes
- Test: `Fixture_Dispose_StopsHostedServices` in `tests/SolTechnology.Core.API.Tests/APIFixtureDisposalTests.cs` (a hosted service flips a flag on `StopAsync`). It instantiates `APIFixture`, which `TestHostContainmentGuardTests` forbids outside `ApprovedFiles`, hence the allowlist entry.
- No new test project (`*.Testing` tests remain a follow-up; Q6, user, 2026-11-02: no new test projects).

## Acceptance criteria
- [ ] `dotnet test tests/SolTechnology.Core.API.Tests`, `dotnet test tests/SolTechnology.Core.HTTP.Tests`, and `dotnet test tests/SolTechnology.Core.Tests` (containment guard) are green.
- [ ] `dotnet test sample-tale-code-apps/DreamTravel/tests/Component/DreamTravel.Component.Tests.csproj` is green.
- [ ] The core brief's P-11 row matches the delivered behaviour; release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) copies it into the 2.0 release notes.

## Open questions
- none

## Deviations
