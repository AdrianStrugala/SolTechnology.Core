---
adr: 008-testing-framework-companions
step: 05 of 11
status: reviewed
---

<!-- Reviewed: renumbered from to-do/04-api-testing-extensions.md. Version bump pinned to
     0.6.0 → 0.7.0; no test project. -->

# Step 05: Extend `SolTechnology.Core.API.Testing`

## Summary
Grow the existing `API.Testing` package with the reusable host-side helpers observed in MTS
(`IApplicationFixture.GetUserAuthClient` / `GetNoAuthClient`) and DreamTravel (configuration
override + service replacement). Keeps `APIFixture<TEntryPoint>` as the core, adds ergonomics so
apps stop hand-rolling auth clients and config builders. Separate PR because it only touches the
already-shipping package and depends on nothing from steps 03/04/06/07.

## Affected components
- `src/SolTechnology.Core.API.Testing/APIFixture.cs` — keep existing ctor; add optional auth-scheme parameter plumbing.
- `src/SolTechnology.Core.API.Testing/AuthClientExtensions.cs` — `CreateAuthorizedClient(scheme, token)` + `CreateAnonymousClient()` (generalised from MTS `IApplicationFixture`).
- `src/SolTechnology.Core.API.Testing/TestConfigurationBuilder.cs` — wrap `ConfigurationBuilder` + `AddJsonFile` + `AddInMemoryCollection` override pattern repeated in every app fixture.
- `src/SolTechnology.Core.API.Testing/SolTechnology.Core.API.Testing.csproj` — bump version **0.6.0 → 0.7.0** (MINOR, additive); add `SolTechnology.Core.Testing` project/package reference.

## Details
- Auth helpers must be scheme-agnostic (`AuthenticationHeaderValue(scheme, token)`); no app-specific schemes baked in.
- `TestConfigurationBuilder` returns `IConfiguration` consumable by the existing `APIFixture` ctor — no breaking change to current callers.
- Do not bundle deterministic-publisher shims here (those are app-specific, e.g. `SyncHangfireNotificationPublisher`); document the pattern in step 11 instead.
- **No test project.** Per ADR-008 there is intentionally no `tests/SolTechnology.Core.API.Testing.Tests`; validation is build-based (existing callers compile) plus a documented manual smoke. Nothing is added to `tests/`, so PR/CI builds are unaffected.

## Acceptance criteria
- Existing `APIFixture` callers compile unchanged.
- `CreateAuthorizedClient` sets the `Authorization` header; `CreateAnonymousClient` does not (documented manual smoke).
- `TestConfigurationBuilder` merges a JSON file with in-memory connection-string overrides (documented manual smoke).

## Open questions
- none

