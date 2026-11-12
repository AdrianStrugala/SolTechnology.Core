# Delivery Workflow

One method takes every change from a request to an accepted result. Its state lives in the dated
feature brief, the step files, and git — never in a conversation. The brief is the durable record
of what was planned and delivered; the current system design always lives under
[`docs/architecture/`](./).

The driver runs the phases below. The [`deliver`](../../.github/skills/deliver/SKILL.md) skill is
the entry point and drives them end to end; its trigger is the `deliver` row of `CLAUDE.md` §4. A
request that names one owner makes the user the driver: that owner runs exactly one phase. Both
follow this page.

## Phases

A Feature, and any change handed to `deliver`, starts from a clean `git status` and
`./.github/verify.ps1` exiting 0; that commit is the baseline. Red baseline → stop: a later
failure could not be attributed to the change.

### Size

| Size | Trigger | Phases |
|---|---|---|
| Docs-only | `CLAUDE.md` §0 docs-only tier | Build, Report. No brief; the gate is `./.github/checkDocs.ps1`. |
| Small | No "When to invoke" trigger of [`implementation-planning`](../../.github/agents/implementation-planning.agent.md) applies, no `CLAUDE.md` §2 action, no premortem trigger | Build, Report. No brief; the goal and its pass/fail checks, stated in the conversation before the first edit, are the contract. |
| Feature | Everything else | All. |

Between two sizes, take the larger. [`refactor`](../../.github/skills/refactor/SKILL.md) and
[`dependency-audit`](../../.github/skills/dependency-audit/SKILL.md) changes are Small unless
their own routing rules send them to planning.

A Docs-only or Small change runs without `deliver` unless the user says "deliver". The agent edits
the working tree under `CLAUDE.md`, runs the gate once before yielding (`./.github/checkDocs.ps1`
for Docs-only, `./.github/verify.ps1` for Small), and reports. It needs no baseline and no branch,
and the agent does not commit.

### Phase order

Phases run in this order. A resumed delivery starts at the first row whose "Starts when" holds.
`status: blocked` → report the `## Blockers` and stop.

| Phase | Starts when | Owner | Done when |
|---|---|---|---|
| Plan | No brief, or `status: planning` with an unfinished plan | [`implementation-planning`](../../.github/agents/implementation-planning.agent.md) | The intake questions are answered; the brief and its steps exist; a question that arose later is recorded with a recommended default. |
| Review | `review: pending` | [`plan-reviewer`](../../.github/agents/plan-reviewer.agent.md) | The critique is returned. Applying the user's answers in Approval sets `review: done`. |
| Approval | `approved:` empty | The user | See [Approval](#approval). |
| Premortem | `premortem: pending` | [`implement-plan`](../../.github/skills/implement-plan/SKILL.md), step `00` | `premortem:` is `go` or `go-with-mitigations`; mitigations are folded into steps. |
| Build | An implementation step is not accepted | `implement-plan`, one step per run; [`verifier`](../../.github/agents/verifier.agent.md) | See [Accepting a step](#accepting-a-step). |
| Close | Only the retrospective is left | `implement-plan`, retrospective; `verifier`, whole-feature mode | [Completion](#completion) is done, the verdict is `PASS`, the result is committed. |
| Report | The feature is closed | The driver | The user has the report defined in `deliver`. |

### Planned stops

Two, and no others:

1. **Intake questions**, at the start of Plan. The planner asks one batched round about facts the
   repository cannot answer and plans only after the answers. No such fact → no stop.
2. **Approval**, below.

### Approval

Present to the user, in their language:

1. Goal, scope, and what is out of scope.
2. The steps, one line each, with the checks that will prove each one.
3. Gate values (`review`, `premortem`) and why.
4. Every `CLAUDE.md` §2 action the plan contains.
5. The reviewer's findings and every open question with its recommended default.

Wait. The reviewer — the planner when review is waived — applies the answers; repeat until the
user approves. The approval covers the §2 actions in item 4 and nothing else. After an explicit
yes: set `approved: YYYY-MM-DD`, create the branch, commit the plan.

### Accepting a step

A step is `done` when its implementer yields and accepted when it is committed. Accept it only
when, in this order:

1. `./.github/verify.ps1` exits 0 in a run made after the implementer yielded, outside the
   implementer's context.
2. The `verifier` returns `PASS` — required when the step touches `src/SolTechnology.Core.*`, a
   `ModuleInstaller.cs`, or `Directory.Build.props`. Other steps are verified once, in Close.
3. It is committed with [`commit-message`](../../.github/skills/commit-message/SKILL.md); Guide
   §15 fix-on-touch changes go in their own `chore:` commit.

A failed gate or a `FAIL` verdict returns the step to a fresh `implement-plan` run with the gate
output or the findings table verbatim. At most 3 fix rounds per step. NEVER accept a step because
an agent, including a reviewer, reports that it is done.

### Git

A delivery with a baseline works on `feature/<kebab-name>` — the brief's name; without a brief, a name taken from the
request. Create it at Approval; without a brief, before the first edit. One commit per accepted
step. The driver commits; an owner never does. An agent NEVER pushes, merges, opens a pull
request, or commits to `master`.

### Stop conditions

Besides the planned stops, stop only for these. Report the state, the evidence, and the options with a
recommendation. NEVER work around:

- Dirty tree or red baseline at the start.
- A `CLAUDE.md` §2 action that the approved plan does not list.
- Premortem `No-Go`.
- An open question raised during implementation (the step is `blocked`).
- The third failed round on one step.
- A deviation that changes the approach of the approved plan: it needs a new approval.
- A check named in a step's acceptance criteria that cannot run in this environment.

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
approved:
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

- `in-progress` requires `approved: YYYY-MM-DD`. Only the user approves a plan. An agent sets
  the field, with that day's date, after the user says so in the conversation, and NEVER on its
  own judgment. No implementation step starts while the field is empty.
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
approved:
completed:
---

# <Feature title>

> Historical delivery record. It may not describe the current system.

## Goal

## Context

## Scope

## Implementation plan

## Acceptance criteria

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 01 | <Title> | [`steps/01-<step>.md`](YYYY-MM-DD-<kebab-name>/steps/01-<step>.md) | to-do |

## Blockers

## Completion summary

## Deviations

## Follow-ups
```

The initial brief contains the goal, context, scope, implementation plan, and acceptance criteria.
Keep `Completion summary`, `Deviations`, and `Follow-ups` empty until delivery closes. Keep
`Blockers` empty unless the status is `blocked`.

### Gates

A feature with a working folder also carries its gates and step table in the brief:

```yaml
---
status: planning
created: YYYY-MM-DD
approved:
completed:
review: pending
premortem: pending
---
```

- `review:` and `premortem:` hold concrete gate values and are never omitted while steps exist.
  A feature without a working folder has no gate fields and no `## Steps` section.
- `## Steps` lists every step file in order. Its status cells mirror step frontmatter in the same
  edit.

| Field | Allowed values |
|---|---|
| `review` | `pending`, `waived (<reason — planner, date>)`, `done (date)`, `skipped (<reason — user, date>)` |
| `premortem` | `pending`, `waived (<reason — planner, date>)`, `go (date)`, `go-with-mitigations (date)`, `no-go (date)`, `skipped (<reason — user, date>)` |

Only the user may skip a required gate; record their reason verbatim.

## Working folder

Use a temporary sibling folder only when a change needs durable multi-step coordination:

```text
docs/features/
  YYYY-MM-DD-<kebab-name>.md
  YYYY-MM-DD-<kebab-name>/
    steps/
      00-run-premortem.md
      01-<step>.md
      NN-retrospective.md
```

- The folder holds only step files; gates and the step table live in the feature brief.
- Step files use `to-do | blocked | in-progress | done` as execution states.
- Review and premortem gates are required by risk, not by document type.
- The retrospective reviews delivery, updates architecture documentation, completes the feature
  brief, and then deletes the entire working folder.

### Step file

Frontmatter sits on line 1:

```markdown
---
spec: YYYY-MM-DD-<feature>
step: NN
status: to-do
---

# Step NN: <Title>

## Summary
<One short paragraph: what and why.>

## Affected components
- `path/to/File.cs` — NEW / EDIT / DELETE — exact change

## Changes
- Exact symbol, option key, migration, or `package@version`.

## Acceptance criteria
- [ ] Exact build, test, or behavior check.

## Open questions
- none

## Deviations
```

A step with an unresolved `Open questions` item is `blocked`.

## Completion

The retrospective writes the record; it does not judge the delivery. The `verifier`, in
whole-feature mode, does that afterwards. The retrospective performs these actions in order:

1. Run `./.github/verify.ps1`; collect the delivered outcomes and the deviations from the steps
   and the feature diff.
2. Update every affected document under [`docs/architecture/`](./) to describe the current
   system and rationale.
3. Append the delivered outcome, material deviations, and follow-ups to the feature brief.
4. Set the feature status to `completed` or `abandoned` and set `completed: YYYY-MM-DD`.
5. Convert each step link in `## Steps` to its plain file name, keeping the table with final
   statuses as the record of what was done.
6. Verify that no durable link points into the working folder.
7. Delete the working folder.

The remaining feature brief is historical evidence. Never use it as the source of truth for
current architecture or behavior.

## Rationale

Each design choice answers a measured failure of autonomous agents:

| Choice | Reason |
|---|---|
| A step is accepted on the exit code of `verify.ps1`, never on a report or a reviewer's verdict. | In long-running loops an agent reported progress in every cycle while over half the cycles brought no measured improvement, and a stronger LLM judge did not close the gap; only a signal measured outside the transcript did ([Self-Evaluation Bias, 2026](https://arxiv.org/abs/2607.25152)). |
| Implementation and verification run in separate contexts; the `verifier` is read-only, assumes the change must not ship, and drops findings it cannot reproduce from the diff. | Agents praise their own work, and a separate sceptical evaluator is easier to tune than a self-critical generator ([Anthropic, harness design, 2026](https://www.anthropic.com/engineering/harness-design-long-running-apps)); a falsification pass that sees only the diff removes hallucinated findings ([OpenCodeReview, 2026](https://arxiv.org/abs/2608.09290)). |
| The plan is kept and the user approves it; each step's acceptance criteria are fixed before code. | Without a planner a generator under-scopes; agreeing what "done" means before building ties the result to a testable contract (same Anthropic source). |
| State lives in the feature brief, step files, and git; every role starts from a fresh context and a dispatch carries no opinion of the work. | Role separation with isolated workers and a fresh start per cycle counters drift ([Cursor, scaling agents, 2026](https://cursor.com/blog/scaling-agents)); unclear roles, misaligned hand-offs, and missing verification are the three failure classes of multi-agent systems ([MAST, 2025](https://arxiv.org/abs/2503.13657)). |
| Tests come first, and a weakened, disabled, or special-cased test fails verification. | Coding agents add mocks to tests more often than human authors, and mock-heavy tests validate less of the real interaction ([over-mocked tests, MSR 2026](https://arxiv.org/abs/2602.00409)); a test changed after the fact proves nothing about the code it was meant to check. |
| The path scales with size: docs-only and small changes skip planning, the baseline, and the branch; the `verifier` runs per step only for package code and once for the whole feature. | Each harness component costs tokens and encodes an assumption about what the model cannot do alone; the full pipeline pays off only where the model is unreliable solo (same Anthropic source). |
| `deliver` starts on any wording of a Feature request. | The method must not depend on the user remembering a command. |

The harness is re-examined when a model changes: run the regression prompts in
[`AIDocsGuide.md`](../AIDocsGuide.md#regression-prompts) and remove a component that no longer
changes the outcome.