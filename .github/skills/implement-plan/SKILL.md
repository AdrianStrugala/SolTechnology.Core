---
name: implement-plan
description: Execute one implementation step from a dated feature plan on its own tests, resolve a pending premortem gate, or close the feature with the retrospective. Enforces the gate fields of the brief, keeps the plan table in step with execution, and records deviations. Use when a feature brief has an approved plan with a row that is not done, a pending premortem, or a resolved code review awaiting the retrospective.
---

# Implement Plan

Execute one step per invocation. Exception: when the caller asks for several or all steps, run
them consecutively in the same context; a fresh context per step re-reads the protocol, the
guides, the brief, and the sources each time.

The durable record is `docs/features/YYYY-MM-DD-<name>.md`; its sibling folder holds temporary
step files and is deleted by the retrospective. This skill owns the Premortem, Build, and Close
phases of the [delivery workflow](../../../docs/architecture/delivery-workflow.md#phases). Read
that page before touching plan files.

## Procedure

### 1. Locate the step and check gates

Open the feature brief and read its `## Implementation plan` table — the only source of step
status. If the caller named no step, select the lowest-numbered row with `to-do`. When the
invocation carries code-review findings, fix only what they name. When every row is `done`, the
remaining work is the retrospective (section 6).

Enumerate the sibling `steps/` folder when it exists and match files to rows by the two-digit
number; an unmatched file or row blocks execution. Read the complete matching step file. Its
`Affected components`, `Changes`, and `Acceptance criteria` are the execution contract. Without
a working folder, the brief's `Scope`, `Implementation plan`, and `Acceptance criteria` are.

Refuse to run when one of these fails:

- `plan-review:` is `done`, `waived (...)`, or a user-authorized `skipped (...)`.
- `approved:` holds a date. Only the user approves the plan
  ([Approval](../../../docs/architecture/delivery-workflow.md#approval)).
- `premortem:` is `go`, `go-with-mitigations`, `waived (...)`, or a user-authorized
  `skipped (...)`. `pending` → section 5. `no-go` blocks until the plan is revised and a fresh
  premortem passes.
- The retrospective runs only when every row is `done` and `code-review:` is `done` or a
  user-authorized `skipped (...)`.
- A gate field is missing or holds anything but one allowed value.
- The row is `blocked`, or its step has an unresolved `Open questions` item.

Only the user may skip a required gate. Record their reason verbatim. When an open question
blocks implementation, set the row to `blocked`, report the question with a recommended default,
and stop; do not implement against an assumption.

### 2. Start the step

Set the row to `in-progress` and the brief to `status: in-progress` in the same edit, before the
first production-code edit.

Load every related skill the step requires. Follow `CLAUDE.md §0`, the relevant Coding Guide
sections, and current architecture pages.

### 3. Implement and verify

Implement exactly the step. Do not fix adjacent issues or start the next step's work.

- For a behavioural change, write the tests the step names first (via
  [`test-writing`](../test-writing/SKILL.md)), run them, and confirm they fail for the expected
  reason before writing production code. After that, NEVER delete, disable, or loosen a test to
  make it pass; a wrong test is a deviation to record.
- Run diagnostics after edits and build the owning project.
- Run only the step's tests: the test set the step names plus the test projects covering every
  edited module (`dotnet test <project>`). NEVER run `./.github/verify.ps1` or the whole suite
  inside a step; they run once, in the code review.
- Verify every acceptance criterion with the command it names.
- Confirm every listed affected component changed as planned or has a recorded deviation.
- Record material deviations in the step's `## Deviations` (the brief's, without a working
  folder) without rewriting `Summary`, `Affected components`, `Changes`, or `Acceptance criteria`;
  those sections preserve the original plan.

Use this format for each material deviation:

```markdown
### <Short title>

**Original plan:** <What the step required — one line.>

**Actual implementation:** <What changed, the technical reason, and how it was verified — at
most 5 lines.>
```

Do not record trivial path corrections or incidental bug fixes that do not change the approach.

When delivery is blocked after work began, set the row to `blocked` and the feature to
`status: blocked`, and record the blocker under `## Blockers`.

### 4. Complete a step

Set the row to `done`. Keep the feature `status: in-progress`. Report the files changed, each
command run with its result, the deviations, and what was not verified; then yield, or continue
with the next step when the caller asked for several.

### 5. Resolve a pending premortem gate

Invoke the [`premortem`](../premortem/SKILL.md) skill in a context that did not author the plan.
Record the output under a `## Premortem` heading in the brief and the verdict in `premortem:`.

- `Go`: set `premortem: go (YYYY-MM-DD)`.
- `Go with mitigations`: fold each mitigation into its named step, then set
  `premortem: go-with-mitigations (YYYY-MM-DD)`.
- `No-Go`: set `premortem: no-go (YYYY-MM-DD)` and feature `status: blocked`, record the
  blockers, return the plan to the planning agent, and stop.

Premortem is documentation-only. Do not edit production code or start step `01` in the same
invocation.

### 6. Run the retrospective

The retrospective is the completion transaction, not a row or a step file. It writes the record
and does not judge the delivery; the [`code-review`](../code-review/SKILL.md) did that. Perform
the six actions of
[Completion](../../../docs/architecture/delivery-workflow.md#completion) in order, then:

- remove resolved entries from `## Blockers`;
- re-read code, architecture pages, and the brief, and verify they agree;
- search repository Markdown for the deleted working-folder path and repair every dangling link.

`## Completion summary` records only what the current state cannot show: what was rejected, what
deviated, what to watch. A sentence that is still true of the system belongs in an architecture
page, and the summary links it.

If the user explicitly abandons a feature, use the same transaction without requiring all rows to
be done: record the reason and each row's last state, set `status: abandoned` and the completion
date, then delete the working folder.

## Quality checks

- [ ] Gate preconditions passed before edits.
- [ ] The brief's status and plan rows match actual execution.
- [ ] Diagnostics are clean, the step's tests pass, and its acceptance criteria pass.
- [ ] Every affected component changed as planned or has a recorded deviation.
- [ ] Retrospective only: the feature's tests were re-read as one set; dead and duplicate ones cut.
- [ ] Retrospective only: every premortem mitigation is marked delivered, superseded, or accepted.
- [ ] Retrospective only: current architecture matches delivered code.
- [ ] Retrospective only: feature status and completion date are final; the working folder is
  gone and no link points into it.

## Constraints

- Never commit; the driver commits each step
  ([Accepting a step](../../../docs/architecture/delivery-workflow.md#accepting-a-step)).
- Never set `code-review:`; the driver sets it from the review's verdict.
- Never create an ADR or hand-maintained feature index.
- Never treat a historical feature record as current architecture.
- Never update architecture before the behavior ships.
- Never delete the working folder before architecture and feature consolidation are verified.
- Never mark a feature completed while blockers or unmet acceptance criteria remain.
- Write repository artifacts in English and mirror the user's language in conversation.
