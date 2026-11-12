---
status: in-progress
created: 2026-11-12
approved: 2026-11-12
plan-review: waived (plan agreed with the user in conversation - planner, 2026-11-12)
premortem: waived (instruction files and a docs script only, no CLAUDE.md section 4 trigger - planner, 2026-11-12)
code-review: pending
completed:
---

# Explicit gates and feature-end verification

> Historical delivery record. It may not describe the current system.

## Goal

Make the gates of the delivery workflow explicit fields of the feature brief, accept a step on
its own tests, and run the full verification once, at the end of the feature.

## Context

Before this change the workflow ran the full `verify.ps1` and dispatched a fresh agent on every
step, and a `verifier` agent judged each step of package code. Every cold-started step agent
re-read the protocol, the guides, the brief, and the sources. Premortem and retrospective were
step files, and step status lived both in each step file and in the brief's `## Steps` table.
The workflow had never run end to end.

## Scope

Decisions, all confirmed by the user on 2026-11-12:

1. `approved:` stays an explicit field; no implementation without it.
2. `code-review` and `verifier` merge into one feature-end gate. It runs in a fresh context and
   absorbs the verifier's duties: run the full gate, prove every acceptance criterion, check that
   no test was weakened or disabled. The `verifier` agent is deleted.
3. A step is accepted on its own tests and acceptance criteria; the implementer sets it `done`.
   The full `verify.ps1` and the code review run once, before the retrospective.
4. One commit per step stays (cheap, gives rollback points).

Out of scope: a change to any package under `src/`.

## Implementation plan

Work on `feature/agent-delivery-workflow`.

| Step | Outcome | Status |
|---|---|---|
| 01 | `delivery-workflow.md`: gate fields `plan-review`, `premortem`, `code-review` next to `approved`; premortem and retrospective are no longer step files; step status lives only in the brief's `## Implementation plan` table (`Step / Outcome / Status`), step files carry no status and the brief does not link to them; step acceptance on step-scoped tests; full gate and code review once before the retrospective | done |
| 02 | `code-review` skill becomes the feature-end gate (decision 2); `verifier.agent.md` deleted; `CLAUDE.md` section 4 and `deliver` updated | done |
| 03 | `implement-plan`: step-scoped tests only, sets the row `done`, resolves the premortem gate from frontmatter, retrospective as completion transaction with two added duties (re-read all tests of the feature as one set and cut dead or duplicate ones; reconcile every premortem mitigation) | done |
| 04 | `implementation-planning`: further intake round only when an answer or a discovery creates a new decision; product decisions (public API shape, naming, breaking-change tolerance) always asked, technical ones resolved by the planner; steps carry contracts, not code; an analysis checklist (public API and semver, `ModuleInstaller`, `Directory.Build.props`, persisted contracts, NuGet dependencies, DreamTravel impact) | done |
| 05 | `plan-reviewer`: critique, one question round, revises in place, sets `plan-review: done`; mechanical checks stay in the script | done |
| 06 | `deliver`: one implementer per feature, continued across steps (not a fresh agent per step); fresh context stays for plan review, premortem, and code review; commit per step; three fix rounds on the feature-end gate | done |
| 07 | `checkDocs.ps1`: lint the new shape (gate field = one vocabulary value plus date, table statuses, step file to row parity by number, no status in step files, no link into `steps/`); new check "retrospective overdue" (every row `done`, `code-review: done`, working folder still exists); `completed` implies no working folder | done |
| 08 | Migrate the open briefs `2026-07-06-jwt-bearer-authentication` and `2026-11-02-org-transfer-and-2-0-release` and their working folders to the new shape; update `AIDocsGuide.md` regression prompts and `ai-assisted-development.md` | done |

## Acceptance criteria

- `./.github/checkDocs.ps1` exits 0 after step 08.
- No file references the `verifier` agent, a `00-` premortem step, or a retrospective step file.
- `checkDocs.ps1` fails on each seeded error: unknown status, prose gate field, row without step
  file, step file with a status, link into the working folder, a `00-` step file, overdue
  retrospective.
- The `deliver` skill names two planned stops: intake questions and plan approval.

## Blockers

## Completion summary

## Deviations

## Follow-ups

- Run `deliver` end to end once; the regression prompts have never been run.
- Fix `NU1902` (`Microsoft.AspNetCore.WebUtilities` in `SolTechnology.Core.HTTP.Testing`) with the
  `dependency-audit` skill; it blocks a green `verify.ps1`.
- Premortem owed before merge for `Directory.Build.props` and DreamTravel `ModuleInstaller.cs`.
