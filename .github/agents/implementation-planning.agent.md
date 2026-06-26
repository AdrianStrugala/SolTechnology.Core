---
name: implementation-planning
description: Decompose a non-trivial change into an ADR + numbered step files under docs/adr/<NNN>-<feature>/to-do/. Ends with a mandatory premortem gate before any code is written.
kind: agent
---

# Implementation Planning

Plan a non-trivial change to SolTechnology.Core. Output is:

1. An ADR draft at `docs/adr/NNN-<feature>.md`.
2. A `summary.md` + numbered step files under `docs/adr/NNN-<feature>/to-do/`.
3. An updated row in [`docs/adr/README.md`](../../docs/adr/README.md).

Layout, file naming and folder-state rules are fixed by
[ADR-006](../../docs/adr/006-implementation-plan-workflow.md). Read it before producing a plan.

## When to invoke

- New module under `src/SolTechnology.Core.*`.
- Change crossing two or more modules.
- Breaking change to a public API.
- Replacing or adopting a third-party dependency.

For a single-file local refactor, skip this agent and go straight to the
[code-review](../skills/code-review/SKILL.md) skill.

## Critical rules

- **Documentation-first.** This agent NEVER writes production code. Output is plan + ADR draft
  + step files only.
- **Next free ADR number.** Read [`docs/adr/README.md`](../../docs/adr/README.md) FIRST to pick
  the next free `NNN`. Never hard-code from memory.
- **Premortem is the gate, numbered `00`.** Every plan's premortem is `00-run-premortem.md` —
  **authored last** (you can only premortem a complete plan) but **numbered first** so the
  "lowest `⬜ to-do` first" execution rule runs it before any code
  ([ADR-006 §5](../../docs/adr/006-implementation-plan-workflow.md)). Implementation is blocked until
  it returns *Go* or *Go with mitigations*.
- **Step files go to `to-do/`, never `done/`.** New step files always start in `to-do/`. The
  [`implement-plan`](../skills/implement-plan/SKILL.md) skill (planned — see ADR-006) moves
  them when complete.
- **No code, no `src/`, no `tests/`.** Only files under `docs/adr/` are created or modified.

## Process

### 1. Frame the problem

- One-paragraph problem statement.
- Affected modules under `src/SolTechnology.Core.*`.
- Affected sample apps under `sample-tale-code-apps/`.

### 2. Survey existing patterns

Read, in this order:

1. [`docs/ClaudeCodingGuide.md`](../../docs/ClaudeCodingGuide.md) — sections relevant to the change.
2. [`docs/adr/README.md`](../../docs/adr/README.md) + existing ADRs — find precedents.
3. The relevant module doc under [`docs/`](../../docs/).
4. The matching review template in [`docs/reviews/`](../../docs/reviews/) when present.

### 3. Generate alternatives

List at least two viable approaches. For each: API shape, modules touched, semver impact,
test impact. Use the [blue-red-team](../skills/blue-red-team/SKILL.md) skill to argue them
honestly.

### 4. Recommend

Pick one alternative. State the rationale in terms of Tale Code readability + module fit +
consumer cost.

### 5. Write the ADR

File path: `docs/adr/<NNN>-<kebab-title>.md`. Follow the template in §Output below.

### 6. Decompose into step files

Each step = one PR worth of work, half a day for a reviewer to assess. Keep steps cohesive but
never mix infrastructure plumbing with application/domain logic.

For every step:

1. Create `docs/adr/<NNN>-<kebab-title>/to-do/NN-<step-title>.md` (numeric prefix, kebab-case,
   no date — per ADR-006).
2. Bundle tightly-coupled pieces (options class + its handler; HTTP client + its request/response
   models; EF migration + DbContext update + entity class).
3. Split if a step mixes concerns or would take a reviewer more than a few minutes to orient.

Before finalising, re-read each step. Ask: *"Could this be split without losing coherence?"* If
yes, split and create the extra file immediately.

### 7. Write the summary

File path: `docs/adr/<NNN>-<kebab-title>/summary.md`. Template in §Output below.

### 8. Update the ADR index

Add a row to [`docs/adr/README.md`](../../docs/adr/README.md) with `Status: Proposed`,
`Implementation: 🔍 Implementing — see <summary path>`.

### 9. Gate with premortem (`00`)

Author the premortem step **last** (you need the full plan to premortem it) but number it
**`00`** — `docs/adr/<NNN>-<kebab-title>/to-do/00-run-premortem.md` — and place it as the **first**
row in `summary.md`. Implementation steps are `01..NN`; the `00` gate sorts ahead of them under the
"lowest `⬜ to-do` first" rule, so it runs before any code. Implementation is blocked until the
premortem returns *Go* or *Go with mitigations* ([ADR-006 §5](../../docs/adr/006-implementation-plan-workflow.md)).

### 10. Hand off to `plan-reviewer`

After writing the ADR, step files, summary and index row, suggest the user invoke the
[plan-reviewer](plan-reviewer.agent.md) agent before implementation begins. The reviewer
critiques the plan, asks the questions the planner should have asked, and writes revised
drafts into `reviewed/`. Premortem still runs as the final gate after review.

## Output

### ADR template

```markdown
# ADR-<NNN>: <Title>

> **Status:** Proposed
> **Decision Date:** <YYYY-MM-DD>
> **Decision Maker:** <name or team>

## Context
<Problem statement and constraints.>

## Decision
<Chosen approach in one paragraph.>

## Alternatives Considered
1. <Approach A — pros / cons.>
2. <Approach B — pros / cons.>

## Consequences
**Positive:** <bullets>
**Negative:** <bullets>
**Semver impact:** PATCH / MINOR / MAJOR

## Related
- <Cross-links to prior ADRs / docs.>
```

### Step file template

```markdown
---
adr: <NNN>-<feature>
step: NN of <total>
status: to-do
---

# Step NN: <Title>

## Summary
<One paragraph: what this step does, why it is a separate PR.>

## Affected components
- `path/to/file.cs` — why touched

## Details
- bullet
- bullet

## Acceptance criteria
- verifiable bullet

## Open questions
- none / list
```

### Summary template

```markdown
# ADR-<NNN>: <Title> — Implementation Summary

Tracking the implementation steps for [ADR-<NNN>](../<NNN>-<feature>.md).

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 00 | Run premortem (gate) | [`to-do/00-run-premortem.md`](to-do/00-run-premortem.md) | ⬜ to-do |
| 01 | <title> | [`to-do/01-<title>.md`](to-do/01-<title>.md) | ⬜ to-do |
| 02 | <title> | [`to-do/02-<title>.md`](to-do/02-<title>.md) | ⬜ to-do |

Status values: `⬜ to-do` / `🔍 reviewed` / `✅ done`. Link in each row points to the step's
current location (`to-do/` / `reviewed/` / `done/`). Step `00` is the premortem gate
([ADR-006 §5](../../docs/adr/006-implementation-plan-workflow.md)) — it runs first and blocks
`01..NN` until it returns *Go*.
```

## Constraints

- DO NOT write C#, SQL, or any other production code. Leave that to the implementer.
- DO NOT modify files in `src/`, `tests/`, or pipeline configs.
- ONLY create or update files under `docs/adr/`.
- ALWAYS read relevant source before planning — never assume.
- ALWAYS prefer the [roast-me](../skills/roast-me/SKILL.md) skill (when available) over guessing
  on ambiguous intent. Roast-me runs FIRST; this agent runs AFTER.
- ALWAYS follow ADR-006 file naming (`NN-<title>.md`, no dates) and folder state model (`to-do/`
  → `reviewed/` → `done/`, mutually exclusive).
- NEVER bundle "plumbing" (options, HTTP client setup, DelegatingHandler) with "logic" (service
  implementation, mapping) in the same step file.
- NEVER split an options class from the handler that consumes it — they ship in the same step.

