---
spec: 2026-11-02-core-2-0-http
step: 06
status: done
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
- [x] `grep -nE "\b(Try)?(Get|Post|Put|Patch|Delete)Async\b" src/SolTechnology.Core.HTTP/RequestBuilder*.cs` prints nothing (no declaration keeps the suffix).
- [x] `grep -rnE "\.Try(Get|Post|Put|Patch|Delete)Async[<(]" src sample-tale-code-apps tests --include=*.cs` prints nothing (`Try*` verbs have no BCL namesake).
- [x] Remaining `GetAsync(`/`PostAsync(` call sites compile only against `HttpClient`, because the old `RequestBuilder` names no longer exist; the green build below is the mechanical proof.
- [x] `dotnet build SolTechnology.Core.slnx` is green; `dotnet test tests/SolTechnology.Core.HTTP.Tests` is green.

## Open questions
- none (optional `C-12` confirmed by the user on 2026-11-02)

## Deviations

### Test names and untouched files

**Original plan:** Rename the 15 verbs and their call sites in `src/SolTechnology.Core.HTTP.Testing/**`, `tests/SolTechnology.Core.HTTP.Tests/**`, DreamTravel `GeolocationDataClients`, and the `command-query-event-tale` skill if it shows `RequestBuilder` calls.

**Actual implementation:** HTTP.Testing and the skill have no `RequestBuilder` call, so they are unchanged. The DreamTravel UI (`GraphService.cs`) and four component-test files also call `RequestBuilder` verbs and were renamed. Test methods named after a verb dropped the suffix too (`Get_…`, `Post_…`, `TryGet_…`), including step 03's `TryGetAsync_HttpClientTimeout_ReturnsTimeoutError` and `TryGetAsync_CallerCancelled_Throws`, now `TryGet_…`. `docs/Clients.md` and `docs/HTTP-Production-Checklist.md` are renamed in step 08.
