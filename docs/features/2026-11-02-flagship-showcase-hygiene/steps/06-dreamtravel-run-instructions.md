---
spec: 2026-11-02-flagship-showcase-hygiene
step: 06
status: to-do
---

# Step 06: DreamTravel — run-in-two-minutes README and an honest architecture section

## Summary
The DreamTravel README has three problems (`repo#B7`, `B8`):

- Its 519 lines (490 at planning; Chronicle added the "Chronicle step reports" section at
  `:179-205`) contain no run instructions; "Getting started" appears only under future work.
- `initWorkspace.bat` starts docker-compose, but the app runs through Aspire.
- It claims Azure Service Bus, Blob Storage, and Quartz.NET, none of which are wired; the Worker
  uses Hangfire.

The README also documents the `PackageReference` equivalents of the `ProjectReference`s (Q4, user, 2026-11-02: no `UseNuGet`
switch; the README documents the equivalents instead).

## Affected components
- `sample-tale-code-apps/DreamTravel/README.md` — EDIT — NEW "Run in 2 minutes" section (prerequisites: .NET 10 SDK, Docker; no secret setup, the local-only dev credentials stay committed per decision B, user, 2026-11-02; `dotnet run --project src/DreamTravel.Aspire`; URLs; API key); `:21,80,87-88,104-105,175` architecture rewritten from actual project references; `:516` future-work item "Getting started tutorial" removed (`:487` at planning); NEW "Using the NuGet packages" with the `PackageReference` lines; the "Chronicle step reports" section (`:179-205`, owned by the completed Chronicle feature) stays as is
- `sample-tale-code-apps/DreamTravel/initWorkspace.bat` — DELETE (no reference outside DreamTravel, verified 2026-11-10)
- `sample-tale-code-apps/DreamTravel/docker-compose.yml` — DELETE (decision 2026-11-10, recorded under the owner's pre-approval: it is stale, since it mounts `./import` while Aspire binds `Data/`, and it binds the same host ports 1404, 7474, and 7687 as the Aspire resources; nothing outside DreamTravel references it). `src/Infrastructure/DreamTravelDatabase/Dockerfile`, used only by this compose file, stays (core 05 decision B: local dev credentials unchanged); step 08 records it as a follow-up

## Changes
- No core prerequisite: since decision B (user, 2026-11-02) core 05 ([`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md); former core step 08) no longer adds `dotnet user-secrets` commands or edits `docker-compose.yml`.
- Verify the instructions on a clean clone (macOS or Windows) and note the platform in the PR.

## Acceptance criteria
- [ ] Following the README on a clean clone starts the Aspire dashboard and the API (manual check, recorded).
- [ ] `grep -niE "Quartz|Service Bus|Blob Storage" sample-tale-code-apps/DreamTravel/README.md` matches only accurate statements.

## Open questions
- none

## Deviations
