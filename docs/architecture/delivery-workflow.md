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
| Plan review | `plan-review: pending` | [`plan-reviewer`](../../.github/agents/plan-reviewer.agent.md), fresh context | The critique is returned. Applying the user's answers in Approval sets `plan-review: done`. |
| Approval | `approved:` empty | The user | See [Approval](#approval). |
| Premortem | `premortem: pending` | [`implement-plan`](../../.github/skills/implement-plan/SKILL.md) running the premortem skill, fresh context | `premortem:` is `go` or `go-with-mitigations`; mitigations are folded into steps. |
| Build | A row of `## Implementation plan` is not `done` | `implement-plan`, one step per run | See [Accepting a step](#accepting-a-step). |
| Code review | Every row is `done` and `code-review: pending` | [`code-review`](../../.github/skills/code-review/SKILL.md), fresh context | See [Code review](#code-review). |
| Close | `code-review:` is resolved | `implement-plan`, retrospective | [Completion](#completion) is done and committed. |
| Report | The feature is closed | The driver | The user has the report defined in `deliver`. |

### Planned stops

Two, and no others:

1. **Intake questions**, at the start of Plan. The planner asks one batched round about facts the
   repository cannot answer and plans only after the answers. No such fact → no stop. A further
   round opens only when an answer or a discovery creates a decision that did not exist before.
2. **Approval**, below.

### Approval

Present to the user, in their language:

1. Goal, scope, and what is out of scope.
2. The steps, one line each, with the checks that will prove each one.
3. Gate values (`plan-review`, `premortem`) and why.
4. Every `CLAUDE.md` §2 action the plan contains.
5. The reviewer's findings and every open question with its recommended default.

Wait. The reviewer — the planner when plan review is waived — applies the answers; repeat until
the user approves. The approval covers the §2 actions in item 4 and nothing else. After an
explicit yes: set `approved: YYYY-MM-DD`, create the branch, commit the plan.

### Accepting a step

A step runs only its own checks. It is accepted when:

1. The implementer ran the tests the step names and every acceptance criterion of the step, and
   set its row in `## Implementation plan` to `done`. The full `./.github/verify.ps1` does not run
   inside a step.
2. It is committed with [`commit-message`](../../.github/skills/commit-message/SKILL.md); Guide
   §15 fix-on-touch changes go in their own `chore:` commit.

One implementer carries all steps of a feature in one context. Plan review, premortem, and code
review each start from a fresh context.

### Code review

The one independent judgment of a feature. When every row is `done`, the driver runs
[`code-review`](../../.github/skills/code-review/SKILL.md) in a fresh context with the brief and
the baseline commit. The skill is read-only: it runs `./.github/verify.ps1`, proves every
acceptance criterion, checks that no test was weakened, and reviews the diff.

- `PASS` → the driver sets `code-review: done (YYYY-MM-DD)`.
- `FAIL` → the implementer gets the findings table verbatim and fixes only what it names; the
  review runs again in a fresh context. At most 3 rounds.

NEVER resolve the gate because an agent, including the implementer, reports that the feature is
done. A Small change handed to `deliver` gets the same review when it touches
`src/SolTechnology.Core.*`, a `ModuleInstaller.cs`, or `Directory.Build.props`; otherwise its gate
is `./.github/verify.ps1` run by the driver.

### Git

A delivery with a baseline works on `feature/<kebab-name>` — the brief's name; without a brief, a
name taken from the request. Create it at Approval; without a brief, before the first edit. One
commit per step. The driver commits; an owner never does. An agent NEVER pushes, merges, opens a
pull request, or commits to `master`.

### Stop conditions

Besides the planned stops, stop only for these. Report the state, the evidence, and the options with a
recommendation. NEVER work around:

- Dirty tree or red baseline at the start.
- A `CLAUDE.md` §2 action that the approved plan does not list.
- Premortem `No-Go`.
- An open question raised during implementation (the row is `blocked`).
- The third failed round of the code review.
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
plan-review: pending
premortem: pending
code-review: pending
completed:
---

# <Feature title>

> Historical delivery record. It may not describe the current system.

## Goal

## Context

## Scope

## Implementation plan

| Step | Outcome | Status |
|---|---|---|
| 01 | <Reviewable delivery outcome> | to-do |

## Acceptance criteria

## Blockers

## Completion summary

## Deviations

## Follow-ups
```

The initial brief contains the goal, context, scope, implementation plan, and acceptance criteria.
Keep `Completion summary`, `Deviations`, and `Follow-ups` empty until delivery closes. Keep
`Blockers` empty unless the status is `blocked`.

The `## Implementation plan` table is the only source of step status. Its `Status` cell is
exactly `to-do`, `blocked`, `in-progress`, or `done`. A row with an unanswered open question is
`blocked`.

### Gates

Every brief carries the three gate fields from its creation; a missing or malformed field blocks
execution. Each field holds one value from the table and nothing else — the reasoning belongs in
the brief's body.

| Field | Allowed values | Resolved |
|---|---|---|
| `plan-review` | `pending`, `done (date)`, `waived (reason — planner, date)`, `skipped (reason — user, date)` | Before Approval. |
| `premortem` | `pending`, `go (date)`, `go-with-mitigations (date)`, `no-go (date)`, `waived (reason — planner, date)`, `skipped (reason — user, date)` | After Approval, before the first step. `no-go` blocks implementation until the plan is revised and a fresh premortem passes. |
| `code-review` | `pending`, `done (date)`, `skipped (reason — user, date)` | After the last step, before the retrospective. Never waived. |

Gates are selected from blast radius and contract impact, not from document type. Only the user
may skip a required gate; record their reason verbatim. No gate is a row of the plan or a step
file.

## Working folder

Use a temporary sibling folder only when a change needs durable multi-step coordination:

```text
docs/features/
  YYYY-MM-DD-<kebab-name>.md
  YYYY-MM-DD-<kebab-name>/
    steps/
      01-<step>.md
      NN-<step>.md
```

- The folder holds only step files. A step file is an execution contract without status; it
  matches one row of `## Implementation plan` by its two-digit number.
- Enumerate `steps/` directly. The brief never links into the working folder.
- A feature without a working folder has one row; the brief is its execution contract.
- The retrospective deletes the entire working folder.

### Step file

Frontmatter sits on line 1:

```markdown
---
spec: YYYY-MM-DD-<feature>
step: NN
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

A step carries contracts, not code: verified paths, symbols with signatures, the interfaces
between steps, test scenarios by name and assertion, and exact commands. A code block appears
only where the exact text is itself the decision (a literal, a key, a regex).

## Completion

The retrospective is the completion transaction, not a row or a step file. It runs when every
row is `done` and `code-review:` is `done` or `skipped`. It writes the record; the judgment was
made in [Code review](#code-review). In order:

1. Compare planned and delivered behavior across all steps; collect every deviation, including
   one visible in the diff but not recorded in a step.
2. Re-read every test the feature added, as one set. Delete a test a later step made dead and a
   test that repeats another step's coverage.
3. Reconcile the premortem's required mitigations one by one: delivered, superseded, or accepted
   as risk with the reason. A mitigation that names a test requires that test to exist.
4. Update every affected document under [`docs/architecture/`](./) to describe the current
   system and rationale.
5. Append the delivered outcome, material deviations, and follow-ups to the feature brief. Set
   the status to `completed` or `abandoned` and set `completed: YYYY-MM-DD`.
6. Delete the working folder. Run `./.github/verify.ps1` when action 2 removed a test, otherwise
   `./.github/checkDocs.ps1`.

The remaining feature brief is historical evidence. Never use it as the source of truth for
current architecture or behavior.

## Rationale

Each design choice answers a measured failure of autonomous agents:

| Choice | Reason |
|---|---|
| A feature is accepted on the exit code of `verify.ps1` and a fresh-context review, never on the implementer's report. | In long-running loops an agent reported progress in every cycle while over half the cycles brought no measured improvement, and a stronger LLM judge did not close the gap; only a signal measured outside the transcript did ([Self-Evaluation Bias, 2026](https://arxiv.org/abs/2607.25152)). |
| The code review runs in a context that did not write the code, is read-only, assumes the change must not ship, and drops findings it cannot reproduce from the diff. | Agents praise their own work, and a separate sceptical evaluator is easier to tune than a self-critical generator ([Anthropic, harness design, 2026](https://www.anthropic.com/engineering/harness-design-long-running-apps)); a falsification pass that sees only the diff removes hallucinated findings ([OpenCodeReview, 2026](https://arxiv.org/abs/2608.09290)). |
| The plan is kept and the user approves it; each step's acceptance criteria are fixed before code. | Without a planner a generator under-scopes; agreeing what "done" means before building ties the result to a testable contract (same Anthropic source). |
| State lives in the feature brief, step files, and git; a dispatch carries no opinion of the work. | Unclear roles, misaligned hand-offs, and missing verification are the three failure classes of multi-agent systems ([MAST, 2025](https://arxiv.org/abs/2503.13657)). |
| Tests come first, and a weakened, disabled, or special-cased test fails the review. | Coding agents add mocks to tests more often than human authors, and mock-heavy tests validate less of the real interaction ([over-mocked tests, MSR 2026](https://arxiv.org/abs/2602.00409)); a test changed after the fact proves nothing about the code it was meant to check. |
| A step runs only its own tests and one implementer carries all steps; the full gate and the review run once per feature. Docs-only and small changes skip planning, the baseline, and the branch. | Each harness component costs tokens and encodes an assumption about what the model cannot do alone (same Anthropic source). A fresh agent per step re-reads the protocol, the guides, the brief, and the sources each time, and the full suite inside every step repeats work the feature-end run does once. |
| Gates are frontmatter fields with one value each, and a script checks them. | A workflow kept only in prose drifts: statuses outside the vocabulary, paragraphs in gate fields, and briefs left `in-progress` after their last step. A script catches each of them. |
| `deliver` starts on any wording of a Feature request. | The method must not depend on the user remembering a command. |

The harness is re-examined when a model changes: run the regression prompts in
[`AIDocsGuide.md`](../AIDocsGuide.md#regression-prompts) and remove a component that no longer
changes the outcome.
