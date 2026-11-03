---
spec: 2026-11-02-core-2-0-api-auth
step: 11
status: to-do
---

# Step 11: Retrospective

## Summary
This step closes the Api and Auth feature with the [`implement-plan`](../../../../.github/skills/implement-plan/SKILL.md) retrospective transaction. It verifies the delivered behaviour, brings `docs/architecture/` to the current 2.0 state of Api and Auth (`SolTechnology.Core.Auth`, renamed from `SolTechnology.Core.Authentication` by step 01, `C-28`) on `master`, completes the durable feature brief, and deletes this working folder. It runs only when every other step of this feature is `done`.

Former core step 71 (the part that covers this library); split per library on 2026-11-02. Renumbered from 10 on 2026-11-03 (new step 01, `C-28`).

## Affected components
- `docs/architecture/authentication.md` — EDIT — package `SolTechnology.Core.Auth` (formerly `SolTechnology.Core.Authentication`), entry `AddSolAuth(AuthConfiguration)` as the single authN + authZ entry (API-key scheme plus the global "require authenticated user" `AuthorizeFilter`; future roles and policies become `AddSolAuth` options); constant-time comparison (replaces "exact string equality"); empty configured key never authenticates (RM-19); `ApiKey` property. The page keeps its file name.
- `docs/architecture/naming-and-public-api.md` — EDIT — `:13` example `AddSolAuthentication` → `AddSolAuth`
- `docs/architecture/http-client.md` — EDIT — URI-free `HttpRequestFailedException` messages and fixed 5xx `ProblemDetails` titles (step 03, P-09)
- `docs/features/2026-11-02-core-2-0-api-auth.md` — EDIT — `Completion summary`, `Deviations`, `Follow-ups`, `status: completed`, `completed: YYYY-MM-DD`
- `docs/features/2026-11-02-core-2-0-api-auth/` — DELETE

## Changes
1. Review every step against delivered code. Run final validation: `dotnet build SolTechnology.Core.slnx -c Release`, `dotnet test tests/SolTechnology.Core.API.Tests`, `dotnet test tests/SolTechnology.Core.Auth.Tests`, `dotnet test tests/SolTechnology.Core.HTTP.Tests`, and the DreamTravel component and unit tests (`git ls-files 'sample-tale-code-apps/DreamTravel/tests/Component/*.csproj' 'sample-tale-code-apps/DreamTravel/tests/Unit/*.csproj' | xargs -n1 dotnet test`).
2. Update each affected `docs/architecture/*.md` page above to current behaviour and rationale. Record the S4/S5 rationale (CWE-209, timing leak) and the `C-28` rationale (one package and one `AddSolAuth` entry for authentication and the authorization that joins it later).
3. Complete the feature brief with outcomes, material deviations from each step, and follow-ups. The `Completion summary` must also record any deviation that changes a `C-nn`, `P-nn`, or `L-nn` row or a "Release-notes inputs" entry of the core brief ([`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md)); release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) writes the 2.0 release notes and migration guide from the core brief plus every library brief's `Deviations` and `Completion summary`.
   Candidate follow-ups:
   - JWT feature re-plan: `2026-07-06-jwt-bearer-authentication` steps 02 and 04 are superseded by steps 04 and 08 here (Q5), and its paths move to `src/SolTechnology.Core.Auth` / `tests/SolTechnology.Core.Auth.Tests` (`C-28`).
   - Release 05 deprecates `SolTechnology.Core.Authentication` on nuget.org with the alternative `SolTechnology.Core.Auth` (manual owner action, [`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)).
4. Set feature status to `completed` (or `abandoned`) with `completed: YYYY-MM-DD` (`CLAUDE.md §2`: confirm with the user first).
5. Verify that no durable link points into this working folder: `grep -rn "2026-11-02-core-2-0-api-auth/" --include=*.md . | grep -v "^./docs/features/2026-11-02-core-2-0-api-auth/"` prints nothing.
6. Delete the working folder, then search Markdown for the deleted path and repair dangling links.

## Acceptance criteria
- [ ] Every acceptance criterion in this feature's brief is checked or recorded as a deviation.
- [ ] The documentation-cleanup skill passes for the touched `docs/architecture/` pages and the feature brief.
- [ ] The working folder no longer exists, and the step 5 grep prints nothing.
- [ ] `git grep -n "SolTechnology.Core.Authentication" -- ':!docs/features' ':!nuget-stats.json'` prints only the three intended historical mentions of step 01: the `dontreadme.md` deprecated-package row, the `docs/Auth.md` migration note, and the "Formerly SolTechnology.Core.Authentication" sentence of `<Description>` in `src/SolTechnology.Core.Auth/SolTechnology.Core.Auth.csproj` (step 01's temporary architecture-page entries are gone).
- [ ] `git grep -nE "AddSolAuthentication|AuthenticationConfiguration\b" -- ':!docs/features'` prints only the `dontreadme.md` deprecated-package row and the `docs/Auth.md` migration note.

## Open questions
- none

## Deviations
