# Delivery Workflow

Every non-trivial change starts with one dated feature brief. The brief is the durable record of
what was planned and delivered. Temporary implementation steps may support delivery, but the
current system design always lives under [`docs/architecture/`](./).

## Feature naming

Create the feature brief when planning starts:

```text
docs/features/YYYY-MM-DD-<kebab-name>.md
```

The date is the planning start date. The name distinguishes features started on the same day.
Do not allocate sequence numbers and do not add `summary` to the filename.

## Feature status

The feature brief frontmatter is the only source of status:

| Status | Meaning |
|---|---|
| `planning` | Scope and implementation approach are still being defined. |
| `planned` | The plan is ready and implementation has not started. |
| `in-progress` | At least one implementation step is in progress. |
| `blocked` | Delivery cannot continue until a recorded blocker is resolved. |
| `completed` | Implementation and completion summary are finished; the working folder is deleted. |
| `abandoned` | Delivery stopped intentionally; the reason and last state are recorded. |

Use this frontmatter:

```yaml
---
status: planning
created: YYYY-MM-DD
completed:
---
```

Allowed transitions:

```text
planning -> planned | abandoned
planned -> in-progress | abandoned
in-progress -> blocked | completed | abandoned
blocked -> in-progress | abandoned
```

- `blocked` requires at least one unresolved entry under `## Blockers`.
- `completed` requires `completed: YYYY-MM-DD`, a `## Completion summary`, and no working folder.
- `abandoned` requires `completed: YYYY-MM-DD` and the reason under `## Completion summary`.
- `completed` and `abandoned` are terminal. Further work starts a new dated feature.
- Never copy status into a hand-maintained index.

## Feature brief

Create the durable file before creating implementation steps:

```markdown
---
status: planning
created: YYYY-MM-DD
completed:
---

# <Feature title>

> Historical delivery record. It may not describe the current system.

## Goal

## Context

## Scope

## Implementation plan

## Acceptance criteria

## Completion summary

## Deviations

## Follow-ups
```

The initial brief contains the goal, context, scope, implementation plan, and acceptance criteria.
Keep `Completion summary`, `Deviations`, and `Follow-ups` empty until delivery closes.

## Working folder

Use a temporary sibling folder only when a change needs durable multi-step coordination:

```text
docs/features/
  YYYY-MM-DD-<kebab-name>.md
  YYYY-MM-DD-<kebab-name>/
    summary.md
    steps/
      00-run-premortem.md
      01-<step>.md
      NN-retrospective.md
```

- `summary.md` tracks temporary execution state; it is not a second feature specification.
- Step files use `to-do | blocked | in-progress | done` as execution states.
- Review and premortem gates remain in `summary.md` when required by risk, not by document type.
- The retrospective reviews delivery, updates architecture documentation, completes the feature
  brief, and then deletes the entire working folder.

## Completion

The retrospective performs these actions in order:

1. Verify the delivered behavior and acceptance criteria.
2. Update every affected document under [`docs/architecture/`](./) to describe the current
   system and rationale.
3. Append the delivered outcome, material deviations, and follow-ups to the feature brief.
4. Set the feature status to `completed` or `abandoned` and set `completed: YYYY-MM-DD`.
5. Verify that no durable link points into the working folder.
6. Delete the working folder.

The remaining feature brief is historical evidence. Never use it as the source of truth for
current architecture or behavior.