---
name: implement-plan
description: Execute one step from a multi-step implementation plan. Use when the user says "implement next step", "proceed with the plan", "work on step N", or hands off from the `plan-reviewer` agent. Reads the step file, implements exactly the work described, moves the file from `to-do/` (or `reviewed/`) to `done/`, updates the plan summary, and optionally records implementation deviations.
---

# Implement Plan

Execute **one** step from an implementation plan produced by the
[implementation-planning](../../agents/implementation-planning.agent.md) agent. Plan layout is
fixed by [ADR-006](../../../docs/adr/006-implementation-plan-workflow.md):

```
docs/adr/<NNN>-<feature>/
  summary.md
  to-do/       NN-<step>.md
  reviewed/    NN-<step>.md   (drafts produced by plan-reviewer)
  done/        NN-<step>.md
```

## When to use

- The user says: "implement next step", "proceed with the plan", "work on step N",
  "go ahead with the plan".
- Handed off from the [`plan-reviewer`](../../agents/) agent (planned) after a plan is approved.
- An ADR has `Implementation: 🔍 Implementing` in [`docs/adr/README.md`](../../../docs/adr/README.md)
  and the user wants the next step done.

## Procedure

### 1. Locate the step file

- If the user named the step, open that file directly.
- If not, open the ADR index ([`docs/adr/README.md`](../../../docs/adr/README.md)) — find the
  first ADR with status `🔍 Implementing`. Feature plans live under `docs/features/<NNN>-<feature>/`
  with the identical layout; check [`docs/features/README.md`](../../../docs/features/README.md) too.
- Open its `summary.md`. Pick the first row with status `⬜ to-do`. If none exists, ask the user
  which ADR to work on.
- **Premortem gate (`00`).** If that first `⬜ to-do` row is step `00` (`00-run-premortem.md`), it is
  the [ADR-006 §5](../../../docs/adr/006-implementation-plan-workflow.md) gate, not an implementation
  step. Execute it by running the [`premortem`](../premortem/SKILL.md) skill and recording the
  verdict in the step file — **touch no `src/` / `tests/` code**. Mark it `✅ done` only on a **Go** /
  **Go with mitigations** verdict (fold any *No-Go* mitigation into the relevant step first), then
  move it to `done/` and yield. **No `01..NN` step may start while `00` is still `⬜ to-do`.**
- The step file lives in `to-do/` or `reviewed/` per `summary.md`'s "File" column.

### 2. Read the full step file before editing

Read the entire step file in full. Note:

- **Affected components** — the exact files to touch.
- **Details** — what to do.
- **Acceptance criteria** — how to verify.
- **Open questions** — if any are unresolved, STOP and invoke
  [`roast-me`](../roast-me/SKILL.md). Do NOT implement against an open question.

### 3. Load related skills

Skills referenced or implied by the step:

- Adding a `PackageReference` → [`package-management`](../package-management/SKILL.md).
- Writing tests → consult `ClaudeCodingGuide §8`.
- Logging additions → consult `ClaudeCodingGuide §11`.
- Public API surface change → already covered by the plan's `00` premortem gate
  ([ADR-006 §5](../../../docs/adr/006-implementation-plan-workflow.md)); if the step adds surface the
  gate did not foresee, STOP and re-run [`premortem`](../premortem/SKILL.md) before it lands.

### 4. Implement exactly the step

- Scope is limited to **the single step**. Do not bleed into the next step.
- Respect every rule in `CLAUDE.md §0` (pre-flight cite the sections you read) and the relevant
  `ClaudeCodingGuide` section(s).
- After each edit, call `get_errors` per `CLAUDE.md §2`.
- After the step is functionally complete, build the relevant solution
  (`dotnet build SolTechnology.Core.slnx`, plus DreamTravel build for samples).

### 5. Move the step file

`to-do/` → `done/` (or `reviewed/` → `done/`):

```bash
mv docs/adr/<NNN>-<feature>/to-do/NN-<step>.md docs/adr/<NNN>-<feature>/done/NN-<step>.md
```

If the file was in `reviewed/`, source path is `reviewed/` instead of `to-do/`. Either way the
destination is always `done/`.

### 6. Update the plan file's frontmatter

In the moved file, change `status: to-do` → `status: done`.

### 7. Update `summary.md`

Find the row for the step. Change:

- Status cell: `⬜ to-do` (or `🔍 reviewed`) → `✅ done`.
- File link path: `to-do/NN-<step>.md` (or `reviewed/NN-<step>.md`) → `done/NN-<step>.md`.

Do **not** modify any other rows.

#### Example row — before and after

**Before** (was in `to-do/`):
```markdown
| 02 | Agents/Skills split | [`to-do/02-agents-skills-split.md`](to-do/02-agents-skills-split.md) | ⬜ to-do |
```

**After**:
```markdown
| 02 | Agents/Skills split | [`done/02-agents-skills-split.md`](done/02-agents-skills-split.md) | ✅ done |
```

### 8. Record implementation deviations (if any)

If you deviated from the plan (different file, different approach, discovered a constraint that
forced a change), append a `## Retrospective — Implementation Deviations` section at the end of
the moved step file. Do NOT modify the original plan sections — preserve the historical record
of what was planned.

#### Format

```markdown
## Retrospective — Implementation Deviations

### 1. <Short title describing the deviation>
**Original plan:** <what the plan said or implied>
**Actual implementation:** <what was done and the technical reason for the change>

### 2. <Another deviation>
**Original plan:** ...
**Actual implementation:** ...
```

This section is **optional**. Add it only when a deviation is worth recording. Trivial path
adjustments and obvious bug fixes do not need an entry.

### 9. Check if the ADR is now fully implemented

If every row in `summary.md` is `✅ done`, the ADR is complete. Trigger step 10. If not, you
are done — yield back to the user.

### 10. Collapse on completion

Per ADR-006 (Amendment 2026-05-25), when every step ships, the per-ADR working folder is **not
preserved**. Information that matters survives in the ADR file; everything else is noise that
inflates the repo.

Procedure — execute in order, one ADR per invocation:

1. **Read every file under `done/`.** Identify what belongs in the ADR's Implementation summary:
   shipped artifact (file path / skill / agent name), one-line outcome, plus any
   `Retrospective — Implementation Deviations` entries that document a recurring failure mode
   (not trivia like "fixed a typo").
2. **Append an `## Implementation summary` section to the ADR file** (`docs/adr/NNN-<feature>.md`).
   This is the **only** trace that survives. Format:

   ```markdown
   ## Implementation summary

   Completed <YYYY-MM-DD>. The per-step working folder
   (`docs/adr/NNN-<feature>/`) was deleted per the ADR-006 collapse-on-completion rule.

   | # | Step | Shipped |
   |---|---|---|
   | 01 | <title> | <one-line outcome with cite-able file path> |
   | ... | ... | ... |

   ### Preserved deviations

   - **<step>** — <one-line lesson worth keeping>.
   ```

3. **Update the [ADR index](../../../docs/adr/README.md)** — flip the implementation column
   from `🔍 Implementing` to `✅ Done`. Drop any link to the (now-deleted) `summary.md`.
4. **Delete the working folder.** `rm -rf docs/adr/NNN-<feature>/` — `to-do/`, `reviewed/`,
   `done/`, and `summary.md` go together. Verify the folder is gone before yielding.
5. **Verify no dangling links.** `grep -rn 'NNN-<feature>/' .` should return zero results in
   `.github/`, `docs/` (outside the deleted folder), `CLAUDE.md`, or `src/`. If a doc / skill /
   agent linked into the working folder, update the link to the ADR file instead.

The ADR file becomes the single source of truth for what shipped under that decision.

## Quality checks

Before yielding back to the user:

- [ ] The step file lives in `done/` only (verify it is gone from `to-do/` / `reviewed/`).
- [ ] The moved file's frontmatter shows `status: done`.
- [ ] `summary.md`: the row shows `✅ done` and links to `done/`.
- [ ] No other `summary.md` rows were modified.
- [ ] If the ADR is fully implemented, the index row in `docs/adr/README.md` is also updated.
- [ ] Every file in the step's "Affected components" actually changed.
- [ ] `get_errors` clean per `CLAUDE.md §2`.
- [ ] `dotnet build SolTechnology.Core.slnx` green (and DreamTravel build for sample changes).
- [ ] Acceptance criteria from the step file are demonstrably met.

## Constraints

- DO NOT start any `01..NN` implementation step while the plan's `00` premortem gate is still
  `⬜ to-do`. The gate runs first ([ADR-006 §5](../../../docs/adr/006-implementation-plan-workflow.md));
  starting code before a *Go* verdict turns the premortem into a postmortem.
- DO NOT implement work outside the named step. Scope is one step.
- DO NOT modify the original "Summary / Affected components / Details / Acceptance criteria"
  sections of the moved step file. They are a historical record.
- DO NOT move files in bulk (multiple steps per invocation). One step per invocation, even when
  several are obviously trivial — keeps the audit trail clean and lets the user diff one
  decision at a time.
- DO NOT skip the `get_errors` / build / test checks because "the change looks obvious".
- DO NOT mark a step `done` if any acceptance criterion is unmet — instead, surface the gap and
  ask the user whether to split the step or amend the plan.
- DO NOT skip the collapse procedure (step 10) when the last step completes. Leaving stale
  `to-do/` / `done/` / `summary.md` files after the ADR ships re-introduces the noise ADR-006
  Amendment 2026-05-25 exists to remove.
- DO NOT delete the working folder before the Implementation summary lands in the ADR file.
  The collapse is one transaction: summary first, delete second.
- DO NOT bury a recurring failure mode in the deleted `done/` files. If a `Retrospective —
  Implementation Deviations` entry describes a pattern future implementers should know about,
  promote it to the ADR's `### Preserved deviations` block.
- DO NOT invent an ad-hoc multi-step workflow when this skill is unavailable. STOP and tell the
  user `implement-plan` is required (CLAUDE.md §2). The step lifecycle (`to-do/` → `reviewed/`
  → `done/`, frontmatter flip, summary update, deviation log, collapse on completion) is the
  skill's contract; a freehand substitute silently breaks the ADR-006 audit trail.

