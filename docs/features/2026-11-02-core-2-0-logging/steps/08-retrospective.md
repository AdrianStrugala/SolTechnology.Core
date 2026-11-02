---
spec: 2026-11-02-core-2-0-logging
step: 08
status: to-do
---

# Step 08: Retrospective

## Summary
This step closes the Logging feature with the [`implement-plan`](../../../../.github/skills/implement-plan/SKILL.md) retrospective transaction. It verifies the delivered behaviour, brings `docs/architecture/` to the current 2.0 state of Logging on `master`, completes the durable feature brief, and deletes this working folder. It runs only when every other step of this feature is `done`.

Former core step 71 (the part that covers this library); split per library on 2026-11-02.

## Affected components
- `docs/architecture/logging.md` — NEW — one registration path for both `AddSolLogging` overloads, correlation-ID sanitization, one Error entry per failure, CQRS-owned `ActivitySource` on `OperationsName` (P-14, P-15, `C-20`)
- `docs/architecture/README.md` — EDIT — index entry for the new page
- `docs/features/2026-11-02-core-2-0-logging.md` — EDIT — `Completion summary`, `Deviations`, `Follow-ups`, `status: completed`, `completed: YYYY-MM-DD`
- `docs/features/2026-11-02-core-2-0-logging/` — DELETE

## Changes
1. Review every step against delivered code. Run final validation: `dotnet build SolTechnology.Core.slnx -c Release`, `dotnet test tests/SolTechnology.Core.Logging.Tests`, `dotnet test tests/SolTechnology.Core.CQRS.Tests`, and the DreamTravel tests (`dotnet test` over `sample-tale-code-apps/DreamTravel/tests`).
2. Update each affected `docs/architecture/*.md` page above to current behaviour and rationale. The cross-module log contract summary (L-01…L-05, `[{Placeholder}]` templates) is added by core step 09 after every library has delivered its templates.
3. Complete the feature brief with outcomes, material deviations from each step, and follow-ups. The `Completion summary` must also record any deviation that changes a `C-nn`, `P-nn`, or `L-nn` row or a "Release-notes inputs" entry of the core brief ([`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md)); release step 05 ([`2026-11-02-org-transfer-and-2-0-release`](../../2026-11-02-org-transfer-and-2-0-release.md)) writes the 2.0 release notes and migration guide from the core brief plus every library brief's `Deviations` and `Completion summary`.
   Candidate follow-ups:
   - CQRS → Logging → ASP.NET Core shared-framework dependency split (`cqrs-tale#18`, second half).
4. Set feature status to `completed` (or `abandoned`) with `completed: YYYY-MM-DD` (`CLAUDE.md §2`: confirm with the user first).
5. Verify that no durable link points into this working folder: `grep -rn "2026-11-02-core-2-0-logging/" --include=*.md . | grep -v "^./docs/features/2026-11-02-core-2-0-logging/"` prints nothing.
6. Delete the working folder, then search Markdown for the deleted path and repair dangling links.

## Acceptance criteria
- [ ] Every acceptance criterion in this feature's brief is checked or recorded as a deviation.
- [ ] The documentation-cleanup skill passes for the touched `docs/architecture/` pages and the feature brief.
- [ ] The working folder no longer exists, and the step 5 grep prints nothing.

## Open questions
- none

## Deviations
