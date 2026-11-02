---
spec: 2026-11-02-core-2-0-sql
step: 08
status: to-do
---

# Step 08: Retrospective

## Summary
This step closes the SQL feature with the [`implement-plan`](../../../../.github/skills/implement-plan/SKILL.md) retrospective transaction. It verifies the delivered behaviour, brings `docs/architecture/` to the current 2.0 state of SQL on `master`, completes the durable feature brief, and deletes this working folder. It runs only when every other step of this feature is `done`.

Former core step 71 (the part that covers this library); split per library on 2026-11-02.

## Affected components
- `docs/architecture/infrastructure-modules.md` — NEW (or EDIT if blob or messagebus created it first) — SQL section: `IUnitOfWork.Begin`, transient-only retry, safe DACPAC defaults, internal implementations (A4, `C-15`–`C-17`)
- `docs/architecture/README.md` — EDIT — index entry for the new page (only when this step creates the page)
- `docs/features/2026-11-02-core-2-0-sql.md` — EDIT — `Completion summary`, `Deviations`, `Follow-ups`, `status: completed`, `completed: YYYY-MM-DD`
- `docs/features/2026-11-02-core-2-0-sql/` — DELETE

## Changes
1. Review every step against delivered code. Run final validation: `dotnet build SolTechnology.Core.slnx -c Release`, `dotnet test tests/SolTechnology.Core.SQL.Tests` (Docker), and the DreamTravel component and unit tests (`git ls-files 'sample-tale-code-apps/DreamTravel/tests/Component/*.csproj' 'sample-tale-code-apps/DreamTravel/tests/Unit/*.csproj' | xargs -n1 dotnet test`).
2. Update each affected `docs/architecture/*.md` page above to current behaviour and rationale. Copy the shipped A4 rationale from the core brief.
3. Complete the feature brief with outcomes, material deviations from each step, and follow-ups. The `Completion summary` must also record any deviation that changes a `C-nn`, `P-nn`, or `L-nn` row or a "Release-notes inputs" entry of the core brief ([`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md)); release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) writes the 2.0 release notes and migration guide from the core brief plus every library brief's `Deviations` and `Completion summary`.
   Candidate follow-ups:
   - Async `CreateConnection`.
   - Remove the `System.Security.Cryptography.Pkcs 10.0.13` override once `Microsoft.Data.SqlClient` depends on a patched Pkcs (already recorded in the core brief; confirm it is still needed).
4. Set feature status to `completed` (or `abandoned`) with `completed: YYYY-MM-DD` (`CLAUDE.md §2`: confirm with the user first).
5. Verify that no durable link points into this working folder: `grep -rn "2026-11-02-core-2-0-sql/" --include=*.md . | grep -v "^./docs/features/2026-11-02-core-2-0-sql/"` prints nothing.
6. Delete the working folder, then search Markdown for the deleted path and repair dangling links.

## Acceptance criteria
- [ ] Every acceptance criterion in this feature's brief is checked or recorded as a deviation.
- [ ] The documentation-cleanup skill passes for the touched `docs/architecture/` pages and the feature brief.
- [ ] The working folder no longer exists, and the step 5 grep prints nothing.

## Open questions
- none

## Deviations
