---
spec: 2026-11-02-flagship-showcase-hygiene
step: 06
status: to-do
---

# Step 06: DreamTravel — run-in-two-minutes README and an honest architecture section

## Summary
The DreamTravel README has three problems (`repo#B7`, `B8`):

- Its 490 lines contain no run instructions; "Getting started" appears only under future work.
- `initWorkspace.bat` starts docker-compose, but the app runs through Aspire.
- It claims Azure Service Bus, Blob Storage, and Quartz.NET, none of which are wired; the Worker
  uses Hangfire.

The README also documents the `PackageReference` equivalents of the `ProjectReference`s (Q4, user, 2026-11-02: no `UseNuGet`
switch; the README documents the equivalents instead).

## Affected components
- `sample-tale-code-apps/DreamTravel/README.md` — EDIT — NEW "Run in 2 minutes" section (prerequisites: .NET 10 SDK, Docker; no secret setup, the local-only dev credentials stay committed per decision B, user, 2026-11-02; `dotnet run --project src/DreamTravel.Aspire`; URLs; API key); `:21,80,87-88,104-105,175` architecture rewritten from actual project references; `:487` future-work item removed; NEW "Using the NuGet packages" with the `PackageReference` lines
- `sample-tale-code-apps/DreamTravel/initWorkspace.bat` — DELETE
- `sample-tale-code-apps/DreamTravel/docker-compose.yml` — EDIT or DELETE — keep only if the README references it

## Changes
- No core prerequisite: since decision B (user, 2026-11-02) core step 08 ([`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md)) no longer adds `dotnet user-secrets` commands or edits `docker-compose.yml`.
- Verify the instructions on a clean clone (macOS or Windows) and note the platform in the PR.

## Acceptance criteria
- [ ] Following the README on a clean clone starts the Aspire dashboard and the API (manual check, recorded).
- [ ] `grep -niE "Quartz|Service Bus|Blob Storage" sample-tale-code-apps/DreamTravel/README.md` matches only accurate statements.

## Open questions
- none

## Deviations
