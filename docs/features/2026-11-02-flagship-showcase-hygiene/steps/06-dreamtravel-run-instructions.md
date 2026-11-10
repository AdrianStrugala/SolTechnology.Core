---
spec: 2026-11-02-flagship-showcase-hygiene
step: 06
status: in-progress
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
- Premortem SRM-11 (step 00, 2026-11-10, scenario 11: `dotnet run --project src/DreamTravel.Aspire` does not build `DreamTravelDatabase`, so the dacpac hook at `DreamTravel.Aspire/Program.cs:99-103` throws and the API waits forever): the README builds first (`dotnet build` of the DreamTravel projects, which writes `artifacts/dacpac/DreamTravelDatabase.dacpac`), then runs Aspire. Prerequisites name Docker Desktop with amd64 emulation on Apple silicon (the SQL Server image is amd64) and free ports 1404, 7474, and 7687; the README points to the dashboard login link printed to the console. The manual check starts from a fresh `git clone` into a new folder, so no `artifacts/` exists.
- Premortem SRM-12 (scenario 12: `HTTPClients:Google:BaseAddress` is `http://localhost:2137/google/`, which only the component tests' WireMock serves): the two-minute smoke uses Google-free calls — `GET /healthz` and `GET /api/statistics/countries` with headers `X-API-KEY: <SECRET>` and `X-API-VERSION: 2.0` (the committed local key, decision B). An optional section shows how to point Google at the real API with an own key through environment variables (`HTTPClients__Google__BaseAddress`, `HTTPClients__Google__Options__Key`), never committed.
- Premortem SRM-10 (scenario 10, decision under the owner's pre-approval): `DreamTravel.Ui` cannot talk to the API today — it reads `Result<T>` envelopes, calls `/api/v2/...` routes while the API versions by header, uses `ApiBaseUrl` `https://localhost:2021`, and ships `<SECRET>` as the Google Maps key. The README does not present the UI as working: it says the UI needs a Google Maps key and is being aligned with the API (follow-up recorded in step 08). The architecture section still lists it.
- Premortem SRM-13 (scenario 13): the "Chronicle step reports" section (`README.md:179-205`) stays as is; the rewrite only moves headings around it.

## Acceptance criteria
- [ ] Following the README on a fresh clone starts the Aspire dashboard and the API, and the two Google-free smoke calls return 200 (manual check, recorded with date and platform; SRM-11, SRM-12).
- [ ] SRM-10: the README makes no claim that the UI works without a Google Maps key and the follow-up.
- [ ] SRM-13: `git diff <base> -- sample-tale-code-apps/DreamTravel/README.md` removes no line from the "Chronicle step reports" section (`<base>` = the commit before this step).
- [ ] `grep -niE "Quartz|Service Bus|Blob Storage" sample-tale-code-apps/DreamTravel/README.md` matches only accurate statements.

## Open questions
- none

## Deviations
