---
name: implement-plan
description: Execute one temporary implementation step from a dated feature plan. Enforces review and premortem gates, updates feature and step statuses, records deviations, and closes delivery by synchronizing current architecture before collapsing the working folder.
---

# Implement Plan

Execute one step from `docs/features/YYYY-MM-DD-<name>/steps/`. The durable record is the sibling
`docs/features/YYYY-MM-DD-<name>.md`; the working folder is deleted after retrospective.

Read [`docs/architecture/delivery-workflow.md`](../../../docs/architecture/delivery-workflow.md)
before touching plan files.

## Procedure

### 1. Locate the step and check gates

Open the dated feature brief and `summary.md`. If the user did not name a step, enumerate `steps/`
and select the lowest-numbered file with `status: to-do`.

Read the complete selected step. Treat `Affected components`, `Changes`, and `Acceptance criteria`
as the execution contract. Never select steps only from links in `summary.md`; a late-added file may
be missing there.

Enforce these preconditions:

- Step `00` cannot run while `review: pending`.
- Implementation cannot run until premortem is `go`, `go-with-mitigations`, `waived(...)`, or
  user-authorized `skipped(...)`; when step `00` exists, it must be `done`.
- A retrospective cannot run until every other step is `done`.
- Missing or malformed gate fields block execution.
- A step with unresolved `Open questions` cannot run.

Only the user may skip a required gate. Record their reason verbatim.
When an open question blocks implementation, invoke [`roast-me`](../roast-me/SKILL.md) and stop;
do not implement against an assumption.

### 2. Start the step

Set the step to `status: in-progress` and mirror its row in `summary.md`. Set the durable feature
brief to `status: in-progress` before the first production-code edit. These changes happen together.

Load every related skill required by the step. Follow `CLAUDE.md §0`, the relevant Coding Guide
sections, and current architecture pages.

### 3. Implement and verify

Implement exactly one step. Do not start the next step in the same invocation.

- Run diagnostics after edits.
- Build the relevant solution.
- Run affected tests.
- Verify every acceptance criterion.
- Confirm every listed affected component changed as planned or has a recorded deviation.
- Record material deviations in `## Deviations` without rewriting `Summary`, `Affected components`,
  `Changes`, or `Acceptance criteria`; those sections preserve the original plan.

Use this format for each material deviation:

```markdown
### <Short title>

**Original plan:** <What the step required.>

**Actual implementation:** <What changed and the technical reason.>
```

Do not record trivial path corrections or incidental bug fixes that do not change the approach.

When delivery is blocked after work began, set feature `status: blocked`, add `## Blockers` to the
feature brief, and keep the step in the accurate execution state.

### 4. Complete an ordinary step

Set the step to `status: done` and mirror `done` in `summary.md`. Keep the feature
`status: in-progress`. Report verification and yield.

### 5. Run premortem step `00`

Invoke the [`premortem`](../premortem/SKILL.md) skill in a session that did not author the plan.
Record the full output in step `00` and the verdict in `summary.md`.

- `Go`: set step `00` to `done`.
- `Go with mitigations`: fold mitigations into named steps before setting `00` to `done`.
- `No-Go`: leave `00` open, set `premortem: no-go (YYYY-MM-DD)`, set feature `status: blocked`,
  record blockers, return the plan to the planning agent, and stop.

Premortem is documentation-only. Do not edit production code or continue to step `01` in the same
invocation.

### 6. Run retrospective

The retrospective is one transaction: review, synchronize, consolidate, verify, then delete.

1. Enumerate every step and compare planned versus delivered behavior.
   Check integration points across steps, cumulative drift, and deviations visible in code but not
   recorded in a step.
2. Run final diagnostics, build, and tests for the whole feature.
3. Update every affected `docs/architecture/*.md` page to describe current behavior and rationale.
   Remove obsolete claims; do not append a historical narrative.
4. Complete the durable feature brief:
   - summarize each delivered outcome under `## Completion summary`;
   - promote material deviations under `## Deviations`;
   - list unresolved work under `## Follow-ups`;
   - remove resolved `## Blockers`;
   - set `status: completed` and `completed: YYYY-MM-DD`.
5. Re-read architecture pages and the feature record. Verify that code, current docs, and outcome
   agree and that no durable link points into the working folder.
6. Delete the complete sibling working folder.
7. Search repository Markdown for the deleted working-folder path; repair every dangling link.

If the user explicitly abandons a feature, use the same transaction without requiring all steps to
be done: record the reason and each step's last state, set `status: abandoned` and completion date,
verify durable links, then delete the working folder.

## Quality checks

- [ ] Gate preconditions passed before edits.
- [ ] Feature and step statuses match actual execution.
- [ ] Diagnostics, build, tests, and step acceptance criteria pass.
- [ ] Every affected component changed as planned or has a recorded deviation.
- [ ] Material deviations are recorded.
- [ ] Retrospective only: current architecture matches delivered code.
- [ ] Retrospective only: feature status and completion date are final.
- [ ] Retrospective only: no durable link points into the deleted folder.
- [ ] Retrospective only: working folder is gone.

## Constraints

- Execute one step per invocation.
- Never create an ADR or hand-maintained feature index.
- Never treat a historical feature record as current architecture.
- Never update architecture before the behavior ships.
- Never delete the working folder before architecture and feature consolidation are verified.
- Never mark a feature completed while blockers or unmet acceptance criteria remain.
- Write repository artifacts in English and mirror the user's language in conversation.
