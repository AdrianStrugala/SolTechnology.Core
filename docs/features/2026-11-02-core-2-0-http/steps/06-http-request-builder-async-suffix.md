---
spec: 2026-11-02-core-2-0-http
step: 06
status: to-do
---

# Step 06: HTTP — drop `Async` from `RequestBuilder` verbs (optional)

## Summary
Guide §10 forbids the `Async` suffix on project-owned methods that have no sync overload.
`RequestBuilder` exposes `GetAsync`/`PostAsync`/`PutAsync`/`PatchAsync`/`DeleteAsync` (plus the
`<TResponse>` overloads) and `TryGetAsync`/…/`TryDeleteAsync<TResponse>`. These are the most-used
calls in every typed client, so the rename has the highest consumer churn of the plan. It ships
only if `C-12` is confirmed.

Former core step 46; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.HTTP/RequestBuilder.cs` — EDIT — 15 method renames
- `src/SolTechnology.Core.HTTP.Testing/**` — EDIT — call sites, if any
- `tests/SolTechnology.Core.HTTP.Tests/**` — EDIT — call sites
- `sample-tale-code-apps/DreamTravel/src/DataLayer/DreamTravel.GeolocationDataClients/**` — EDIT — call sites
- `.github/skills/command-query-event-tale/SKILL.md` — EDIT — only if it shows `RequestBuilder` calls

## Changes
- Pure rename. No `[Obsolete]` forwarders (`naming-and-public-api.md`: no compatibility forwarders).

## Acceptance criteria
- [ ] `grep -nE "\b(Try)?(Get|Post|Put|Patch|Delete)Async\b" src/SolTechnology.Core.HTTP/RequestBuilder*.cs` prints nothing (no declaration keeps the suffix).
- [ ] `grep -rnE "\.Try(Get|Post|Put|Patch|Delete)Async[<(]" src sample-tale-code-apps tests --include=*.cs` prints nothing (`Try*` verbs have no BCL namesake).
- [ ] Remaining `GetAsync(`/`PostAsync(` call sites compile only against `HttpClient`, because the old `RequestBuilder` names no longer exist; the green build below is the mechanical proof.
- [ ] `dotnet build SolTechnology.Core.slnx` is green; `dotnet test tests/SolTechnology.Core.HTTP.Tests` is green.

## Open questions
- none (optional `C-12` confirmed by the user on 2026-11-02)

## Deviations
