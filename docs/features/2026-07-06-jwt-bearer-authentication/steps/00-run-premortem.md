---
spec: 2026-07-06-jwt-bearer-authentication
step: 00
status: to-do
---

# Step 00: Run premortem (gate)

## Summary

Premortem is mandatory: the feature touches the public NuGet API surface and
`ModuleInstaller.cs` of `SolTechnology.Core.Authentication` (CLAUDE.md §3 gate). Execute
the [premortem skill](../../../../.github/skills/premortem/SKILL.md) in a session that did
not author this plan; record the full output here and the verdict in `summary.md`
`premortem:`. No `src/` code is touched by this step.

## Brief

- **Modules touched:** `src/SolTechnology.Core.Authentication` (public API +
  `ModuleInstaller.cs`), `tests/SolTechnology.Core.Authentication.Tests`, `docs/Auth.md`.
- **API delta (additive, MINOR):**
  - NEW public class `JwtAuthenticationConfiguration` (`Authority`, `Audience`,
    `RequireHttpsMetadata`).
  - NEW public method `ModuleInstaller.AddSolJwtAuthentication(IServiceCollection,
    JwtAuthenticationConfiguration)`.
  - NEW dependency `Microsoft.AspNetCore.Authentication.JwtBearer@10.0.9` — first external
    `PackageReference` of this package; lands on all consumers
    (~1,862 downloads, [nuget-stats.json](../../../../nuget-stats.json)).
  - Internal: shared global `AuthorizeFilter` registration across both installer methods.
  - Behaviour change inside existing API: constant-time API-key comparison (step 02).
- **Checklists:** per-module review templates were removed from the repo in commit
  `ec7a1810` (2026-06-19, docs consolidation) — no module checklist applies; review
  against `ClaudeCodingGuide.md` only.
- **Suggested failure areas to probe:** default-scheme collision when both installers run;
  double `AuthorizeFilter`; `RequireHttpsMetadata=false` leaking into production guidance;
  JwtBearer package version drifting from the `net10.0` framework line; Keycloak container
  flakiness in CI.
- **Steps under review:** [01](01-jwt-bearer-scheme.md), [02](02-api-key-hardening.md),
  [03](03-keycloak-integration-tests.md), [04](04-documentation.md),
  [05](05-retrospective.md).

## Acceptance criteria

- [ ] Premortem output (scenario table, accepted risks, verdict) recorded in this file.
- [ ] `summary.md` `premortem:` field updated to the verdict in the same change.

## Open questions

- none

## Deviations

<!-- Empty at authoring time. Filled by implement-plan when reality diverges from the plan. -->
