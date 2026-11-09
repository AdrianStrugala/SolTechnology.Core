---
spec: 2026-11-02-core-2-0-testing-kits
step: 01
status: done
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
- [x] `dotnet test tests/SolTechnology.Core.API.Tests`, `dotnet test tests/SolTechnology.Core.HTTP.Tests`, and `dotnet test tests/SolTechnology.Core.Tests` (containment guard) are green.
- [x] `dotnet test sample-tale-code-apps/DreamTravel/tests/Component/DreamTravel.Component.Tests.csproj` is green.
- [x] The core brief's P-11 row matches the delivered behaviour; release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) copies it into the 2.0 release notes.

## Open questions
- none

## Deviations

### No HTTP.Tests consumer; a P-11 regression test instead

**Original plan:** the HTTP.Tests tests that consume `WireMockStartup` keep passing under strict matching.

**Actual implementation:** no HTTP.Tests test used `WireMockStartup`, so HTTP.Tests gains a `ProjectReference` to
`SolTechnology.Core.HTTP.Testing` and `WireMockStartupTests`, which pins P-11: with the default settings a request that
misses one query matcher, or a parameter, gets 404, and a full match gets 200. The three cases fail with
`AllowPartialMapping = true`.

### DreamTravel distance-matrix stubs relied on partial matching

**Original plan:** fix only the DreamTravel stubs that relied on partial matching.

**Actual implementation:** under strict matching `FindsOptimalPath` failed with a 500, because both distance-matrix
requests got 404. WireMock splits query values on commas, so `WithParam("origins", "51.1,17.0|...")` never matched the
request's split values, and partial matching had hidden it. `GoogleFakeApi` now matches the comma-split coordinates;
the default stays strict. The release notes get a hint for consumers (umbrella "Release-notes inputs").

### The disposal test boots its own entry point

**Original plan:** a minimal test entry point in `APIFixtureDisposalTests.cs`.

**Actual implementation:** `DisposalTestEntryPoint.Main` builds a bare `WebApplication`, and API.Tests sets
`GenerateProgramFile=false` so it is the assembly entry point `WebApplicationFactory` boots. It sets the content root to
the output folder, because the factory's solution-relative fallback (`<repo>/SolTechnology.Core.API.Tests`) does not
exist. `APIFixture` keeps the root `WebApplicationFactory` in a field; disposing it also disposes the factory derived by
`WithWebHostBuilder` and stops the host. `Dispose` is idempotent. The test fails against the old fixture
(`IsStopped` stays `false`).

Verification: `dotnet test` of API.Tests 63/63, HTTP.Tests 158/158, SolTechnology.Core.Tests 17/17, and the DreamTravel
component tests 31/31 (`-c Release`); Release builds of API.Testing, HTTP.Testing and both test projects have no
warnings.
