---
status: completed
created: 2026-05-25
completed: 2026-07-21
---

# Delivery Workflow

> Historical delivery record. It may not describe the current workflow.

## Goal

Persist multi-step implementation plans across AI sessions and enforce review, premortem, and
retrospective stages.

## Context

Plans previously existed only in chat. The first implementation introduced numbered decision
records, feature indexes, duplicated status tables, and working folders. The repository needed a
durable place for multi-session work, a status model, a distinction between autonomous roles and
load-on-demand procedures, and an enforceable order for plan, review, risk analysis,
implementation, and retrospective.

## Original approach

The first durable model separated decisions from implementation plans:

```text
docs/adr/NNN-decision.md
docs/features/YYYY-MM-DD-feature.md
docs/features/YYYY-MM-DD-feature/
	summary.md
	steps/
		00-run-premortem.md
		01-implementation-step.md
		NN-retrospective.md
```

Features used dates because their chronological identity was stable. Steps used sequence numbers
because review could insert or reorder work. Decisions retained numeric ADR identifiers and linked
to a companion feature when implementation required multiple steps.

### Step state

An earlier folder-state model moved step files through `to-do`, `reviewed`, and `done` directories.
It was replaced by frontmatter because moves broke relative links. The later step grammar was:

```yaml
---
spec: YYYY-MM-DD-feature
step: NN
status: to-do
---
```

Step statuses were `to-do`, `blocked`, `in-progress`, and `done`. An unanswered open question
implied `blocked`. The step frontmatter was authoritative while the summary table mirrored it for
quick scanning.

### Pipeline brackets and gates

Risk-sensitive plans opened with step `00`, which stored premortem scenarios, accepted risks, and
the verdict. Every plan closed with the highest-numbered retrospective step, which compared the
delivered system with the plan, consolidated outcomes and deviations, synchronized durable docs,
and deleted the temporary working folder.

`summary.md` carried explicit review and premortem gate fields. Required stages could be skipped
only after a user-authorized reason was recorded. `implement-plan` refused implementation when the
fields were missing, malformed, pending, or no-go.

### Agents and skills

Roles requiring a fresh context or multi-turn ownership lived under `.github/agents/`; narrow,
composable procedures lived under `.github/skills/`. Planning, review, premortem, implementation,
and retrospective were deliberately different contexts to reduce anchoring on the original plan.

### Artifact style

All repository artifacts were written in English while conversation mirrored the user's language.
Steps favored exact identifiers, short lists and tables, pass/fail acceptance criteria, and the
minimum covering test set. Mermaid was selected for repository diagrams.

## Alternatives considered

### Store plans under `.github/work/`

Rejected because plans would be separated from the feature that motivated them.

### Date-prefix every step

Rejected because step ordering changes during review. Dates remained appropriate for independent
feature records.

### Keep agents and skills together

Rejected because autonomous roles and reusable procedures have different lifecycle and context
requirements.

### Enforce ordering only through prompts

Rejected because prompts are advisory. Explicit gate fields and hard preconditions were selected.

### Run consolidation at the end of the last coding step

Rejected because a full implementation context produced weak whole-feature review. A dedicated
retrospective step created a fresh closing context.

## Scope

- Persist feature briefs and steps in the repository.
- Separate planning, review, risk analysis, implementation, and retrospective roles.
- Record gate results and implementation deviations.
- Collapse temporary steps after delivery.

## Implementation plan

The workflow evolved through folder-state tracking, frontmatter state, explicit gate fields, and
retrospective consolidation. It also introduced the planning and review agents, `roast-me`, package
management, plan execution, diagram authoring, and refusal rules for unavailable mandatory tools.

## Acceptance criteria

- Multi-step work can resume from repository files.
- Risk-sensitive changes cannot silently skip required gates.
- Temporary implementation steps are removed after consolidation.
- Review and premortem decisions are explicit and auditable.
- Plans use exact, executable acceptance criteria.
- A retrospective records deviations and verifies durable documentation before collapse.

## Delivered components

| Component | Delivered responsibility |
|---|---|
| `implementation-planning` agent | Feature brief and multi-step plan creation. |
| `plan-reviewer` agent | Independent critique and in-place revision. |
| `implement-plan` skill | One-step execution with gate checks and status updates. |
| `premortem` skill | Independent risk verdict and mitigation folding. |
| `roast-me` skill | Pre-planning question ledger. |
| `package-management` skill | Repository-canonical dependency versions. |
| `diagram` agent | Versioned Mermaid diagrams under `docs/diagrams/`. |

The original delivery also added Coding Guide anti-pattern rules and established the workflow itself
as the first consumer of `implement-plan`.

## Completion summary

The durable feature brief, optional working folder, independent planning/review/risk roles, gate
checks, one-step execution, and dedicated retrospective all shipped and were exercised by later
features. The initial implementation used ADRs as permanent decision records and maintained manual
ADR and feature indexes.

On 2026-07-21, the documentation model was simplified to mutable architecture documentation and
one dated feature record per delivery. The useful planning, review, premortem, implementation, and
retrospective mechanics were retained. Current rules live in
[`../architecture/delivery-workflow.md`](../architecture/delivery-workflow.md).

## Deviations

- Hand-maintained ADR and feature indexes drifted from the canonical workflow.
- Copying status across frontmatter, summaries, and indexes created inconsistent state.
- Permanent implementation summaries duplicated current architecture and became stale.
- The first plan-reviewer step was authored with malformed section/frontmatter ordering and had to
	be repaired before execution. This led to explicit structural validation of step files.
- Public/protected API changes were removed from the premortem trigger set and kept behind explicit
	user confirmation instead.

## Consequences

### Positive

- Plans survive chat and context boundaries.
- Skipped gates are deliberate and recorded.
- Fresh review and retrospective contexts reduce planner anchoring.
- Temporary execution detail can be collapsed without losing decisions or deviations.

### Negative

- Mirrored state in step frontmatter, summary tables, and indexes created synchronization cost.
- The original ADR-driven model required two durable artifacts for one implementation.
- Workflow machinery itself required migrations as the repository learned from its use.

## Follow-ups

- Generate any future feature listing from frontmatter rather than maintaining an index.
- Keep one authoritative field for each status and avoid copied state.
- Validate temporary-plan structure automatically only when the validator has a clear ownership
	and maintenance path.
