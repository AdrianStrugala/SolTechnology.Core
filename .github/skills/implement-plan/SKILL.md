---
name: implement-plan
description: Execute one step from a multi-step implementation plan. Use when the user says "implement next step", "proceed with the plan", "work on step N", or after plan review. Hard-checks the pipeline gate fields, implements exactly the work described, flips the step's status field, updates the summary, records deviations. The final retrospective step reviews the whole feature and collapses the working folder.
---

# Implement Plan

Execute **one** step from an implementation plan produced by the
[implementation-planning](../../agents/implementation-planning.agent.md) agent. Plan layout,
status vocabulary, gate fields, and bracket steps are fixed by
[ADR-006](../../../docs/adr/006-implementation-plan-workflow.md) — **read it before touching any
plan file**:

```
docs/features/YYYY-MM-DD-<feature>/
  summary.md                 (frontmatter: review / premortem gate fields)
  steps/
    00-run-premortem.md      (only when premortem is required — opening bracket)
    NN-<step>.md             (frontmatter: status: to-do | blocked | in-progress | done)
    NN-retrospective.md      (always the highest number — closing bracket)
```

Step files never move. State lives in each file's `status:` frontmatter field, mirrored to the
summary table in the same change.

## When to use

- The user says: "implement next step", "proceed with the plan", "work on step N",
  "go ahead with the plan".
- A feature has `Implementation: 🔍 Implementing` in
  [`docs/features/README.md`](../../../docs/features/README.md) and the user wants the next
  step done.
- The user abandons a feature → run the **retro-lite** path (§8, last paragraph).

## Procedure

### 1. Locate the step and check the gates

1. If the user named the step, open that file directly. Otherwise: find the active feature via
   the [feature index](../../../docs/features/README.md), open its `summary.md`, and pick the
   **lowest-numbered step whose file (enumerate `steps/` by directory listing, not by summary
   links) shows `status: to-do`**.
2. **Hard gate preconditions (ADR-006 §7)** — check `summary.md` frontmatter:
   - Executing `00`: refuse while `review: pending`. Report; suggest the
     [`plan-reviewer`](../../agents/plan-reviewer.agent.md) agent or an explicit user skip
     (`skipped (<reason — user, date>)` — only the user may authorize it; record their words
     verbatim).
   - Executing `01..NN`: refuse unless `premortem` ∈ {`go`, `go-with-mitigations`,
     `waived(...)`, `skipped(...)`} **and** (when `00-run-premortem.md` exists) step `00` shows
     `status: done`.
   - Executing the retrospective: refuse unless **every other step** is `done`.
   - A missing or malformed gate field blocks execution — report, never guess.
3. **Executing the `00` gate**: run the [`premortem`](../premortem/SKILL.md) skill and record
   the full output in the step file **and** the verdict in the summary's `premortem:` field —
   touch no `src/` / `tests/` code. On *Go with mitigations*: fold each required mitigation into
   the named step file(s) (docs-only edit) **before** flipping `00` to `done`. On *No-Go*: leave
   `00` at `to-do`, set `premortem: no-go (<date>)`, report the blocking scenarios, and yield —
   the plan goes back to the planner. Then yield; implementing `01` is a separate invocation.
4. **Executing the retrospective**: docs-only — jump to §8.

### 2. Read the full step file before editing

Read the entire step file. Note:

- **Affected components** — the exact files to touch.
- **Changes** — what to do.
- **Acceptance criteria** — how to verify.
- **Open questions / `status: blocked`** — if any entry is unanswered, STOP and invoke
  [`roast-me`](../roast-me/SKILL.md). Do NOT implement against an open question.

Flip the step to `status: in-progress` (frontmatter + summary row, same change) before the first
code edit.

### 3. Load related skills

- Adding a `PackageReference` → [`package-management`](../package-management/SKILL.md).
- Writing tests → `ClaudeCodingGuide §8`.
- Logging additions → `ClaudeCodingGuide §11`.
- Public API surface change the `00` gate did not foresee → STOP and re-run
  [`premortem`](../premortem/SKILL.md) before it lands.

### 4. Implement exactly the step

- Scope is limited to **the single step**. Do not bleed into the next step.
- Respect every rule in `CLAUDE.md §0` (pre-flight cite the sections you read) and the relevant
  `ClaudeCodingGuide` section(s).
- After each edit, run the environment's diagnostics (`get_errors` per `CLAUDE.md §2`, or
  `dotnet build` when no such tool exists).
- After the step is functionally complete, build the relevant solution
  (`dotnet build SolTechnology.Core.slnx`, plus the DreamTravel build for sample changes).

### 5. Flip the status

In the step file: `status: in-progress` → `status: done`. In `summary.md`: the row's status
cell → `✅ done`. Same change, no other rows touched, no files moved.

### 6. Record implementation deviations (if any)

If you deviated from the plan (different file, different approach, a constraint that forced a
change), fill the step file's `## Deviations` section. Do NOT modify the original
Summary / Affected components / Changes / Acceptance criteria sections — they are the historical
record of what was planned. **Unrecorded deviations are invisible to the retrospective.**

```markdown
## Deviations

### 1. <Short title>
**Original plan:** <what the plan said or implied>
**Actual implementation:** <what was done and the technical reason>
```

Optional — only when worth recording. Trivial path adjustments and obvious bug fixes do not
need an entry.

### 7. Yield

If the just-finished step was not the retrospective, yield back to the user. The retrospective
is picked up by the next invocation once it is the lowest `to-do` — its own invocation
guarantees it runs with fresh context, not as an afterthought of a coding session.

### 8. Executing the retrospective step (review → consolidate → verify → collapse)

Per ADR-006 §6, the working folder is **not preserved**. Durable information survives in the
**feature spec**. Execute in order — one transaction, consolidate first, delete second:

1. **Review the whole feature against the plan.** For each step (directory listing of
   `steps/`): diff plan vs delivered code — acceptance criteria demonstrably met? deviations
   visible in the code but absent from `## Deviations`? Then look **across** steps: integration
   seams between PRs, drift accumulated over the sequence, residual tech debt. Run diagnostics
   / `dotnet build` on the touched solution one final time. Record findings; anything genuinely
   unresolved becomes a listed follow-up — never invent a resolution.
2. **Consolidate into the feature spec.** Append to
   `docs/features/YYYY-MM-DD-<feature>.md`:

   ```markdown
   ## Implementation summary

   Completed <YYYY-MM-DD>. Gates: review — <done/waived/skipped + reason>; premortem —
   <go/go-with-mitigations/waived/skipped + reason>. The working folder
   (`docs/features/YYYY-MM-DD-<feature>/`) was deleted per the ADR-006 collapse rule.

   | # | Step | Shipped |
   |---|---|---|
   | 01 | <title> | <one-line outcome with cite-able file path> |

   ### Preserved deviations

   - **<step>** — <one-line lesson worth keeping>.

   ### Follow-ups

   - <anything left open, or remove this subsection>
   ```

   Promote every `Deviations` entry documenting a recurring failure mode (not trivia) to
   `### Preserved deviations`.
3. **Verify before deleting.** Re-read the appended section: every step from the directory
   listing appears with its outcome; gate verdicts, preserved deviations, and follow-ups are
   captured; no link in the spec points into the working folder. Fix gaps now — this is the
   last moment the step files exist.
4. **Update the indexes.** [Feature index](../../../docs/features/README.md): flip
   `🔍 Implementing` to `✅ Done`, drop the link to the (about-to-be-deleted) `summary.md`. If
   the feature implements an ADR: point the ADR's `Implemented via` line at the spec and flip
   the [ADR index](../../../docs/adr/README.md) row.
5. **Delete the working folder.** `rm -rf docs/features/YYYY-MM-DD-<feature>/` — `steps/` and
   `summary.md` go together. Verify the folder is gone.
6. **Verify no dangling links.** `grep -rn 'YYYY-MM-DD-<feature>/' .` must return zero hits in
   `.github/`, `docs/` (outside the deleted folder), `CLAUDE.md`, `src/`. Any doc that linked
   into the working folder now links to the feature spec instead.
7. Flip the retrospective step's own status to `done` conceptually via the index update — the
   file is deleted with the folder; the Implementation summary is its record.

**Retro-lite (abandoned feature — explicit user request only):** skip the all-steps-`done`
precondition and step 1's build; record each step's last state and the abandonment reason in
the spec under `## Implementation summary` with `Status: Abandoned`; then run steps 3–6
unchanged. No working folder outlives its feature.

## Quality checks

Before yielding back to the user:

- [ ] Gate preconditions were checked before any edit (and the refusal path taken if unmet).
- [ ] The step file's frontmatter shows `status: done` (or the correct blocked/refused state).
- [ ] `summary.md`: the row matches the frontmatter; no other rows modified.
- [ ] Every file in the step's "Affected components" actually changed.
- [ ] Diagnostics clean per `CLAUDE.md §2`; `dotnet build SolTechnology.Core.slnx` green (and
      DreamTravel build for sample changes).
- [ ] Acceptance criteria from the step file are demonstrably met.
- [ ] Retrospective invocation only: consolidation verified before deletion; indexes updated;
      dangling-link grep clean; folder gone.

## Constraints

- DO NOT start any `01..NN` step while the gates are unmet (§1.2). Starting code before a *Go*
  verdict turns the premortem into a postmortem.
- DO NOT run the premortem in the session that authored the plan — fresh eyes are the point of
  the gate.
- DO NOT run the retrospective while any other step is not `done` (retro-lite on explicit user
  request excepted).
- DO NOT set `review: skipped` or `premortem: skipped` on your own initiative — only the user
  may skip a required gate; record their reason verbatim.
- DO NOT implement work outside the named step. Scope is one step, one invocation — even when
  several steps look trivial. Keeps the audit trail clean, lets the user diff one decision at a
  time, and guarantees the retrospective its own fresh session.
- DO NOT modify the original planned sections of a step file. Deviations go in `## Deviations`.
- DO NOT move step files. State changes are frontmatter flips mirrored to the summary.
- DO NOT skip diagnostics / build / test checks because "the change looks obvious".
- DO NOT mark a step `done` if any acceptance criterion is unmet — surface the gap and ask the
  user whether to split the step or amend the plan.
- DO NOT delete the working folder before the Implementation summary is written **and
  verified** in the feature spec — consolidate first, delete second, always.
- DO NOT bury a recurring failure mode in the deleted `steps/` files — promote it to
  `### Preserved deviations`.
- DO NOT invent an ad-hoc multi-step workflow when this skill is unavailable. STOP and tell the
  user `implement-plan` is required (`CLAUDE.md §2`). The step lifecycle (gate checks, status
  flips, summary sync, deviation log, retrospective collapse) is the skill's contract; a
  freehand substitute silently breaks the ADR-006 audit trail.
- ALWAYS write artifacts in English and in the ADR-006 §8 style; converse in the user's
  language (ADR-006 §9).