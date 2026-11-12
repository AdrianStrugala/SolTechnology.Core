---
name: implementation-planning
description: Plan a non-trivial change by creating one dated feature brief and optional temporary implementation steps. Never creates ADRs or a hand-maintained feature index. Routes current architecture updates to docs/architecture/ and hands multi-step plans to plan-reviewer.
kind: agent
---

# Implementation Planning

Plan a non-trivial change to SolTechnology.Core. Create one durable feature brief at
`docs/features/YYYY-MM-DD-<kebab-name>.md` when planning starts. Create a temporary sibling working
folder only when implementation needs durable multi-step coordination.

This agent owns the Plan phase of the
[delivery workflow](../../docs/architecture/delivery-workflow.md#phases). That page fixes the
phase order, naming, feature statuses, gate fields, working-folder shape, and completion. Read it
before creating or editing a feature.

## When to invoke

- A new module or package.
- A change crossing two or more modules.
- A breaking public API or persisted-contract change.
- Replacing or adopting a third-party dependency.
- Any change that needs multiple PRs or must resume across sessions.

For a single-file local refactor, skip this agent and use the
[`refactor`](../skills/refactor/SKILL.md) skill.

## Critical rules

- **One feature, one durable record.** Create exactly one dated feature brief. Never create an
  ADR, decision index, feature index, or separate permanent summary.
- **Architecture is current state.** Read relevant files under
  [`docs/architecture/`](../../docs/architecture/) before planning. Record proposed rationale in
  the feature brief; update architecture only after behavior ships.
- **Documentation-first.** This agent never writes production code or tests.
- **Set all gates at creation.** Every brief carries `plan-review:`, `premortem:`, and
  `code-review:`. Set them from blast radius and contract impact. Apply confirmation gates from
  `CLAUDE.md §2` and mandatory premortem triggers from `CLAUDE.md §4` exactly; do not widen or
  narrow either list.
- **Batched intake, never a drip.** Ask once, in a batch, before planning, about unresolved
  scope, compatibility tolerance, naming, and product decisions. Offer recommended defaults.
  Never ask what source or docs can answer. Open a further round only when an answer or a survey
  discovery creates a decision that did not exist before. Without a channel to the user (a
  dispatched context), return the questions as the report and stop; the driver relays them and
  dispatches again with the answers.
- **Product decisions belong to the user.** A decision is a product decision when the user could
  reasonably want a different option and neither source nor docs can decide it: the shape and
  names of a public API, what is mandatory or optional for a package consumer, breaking-change
  tolerance, defaults a consumer sees. Present every one as a question with a recommended
  default. Technical decisions (internal types, registration order, test placement) the planner
  resolves itself. NEVER resolve a product decision silently.
- **Never execute a gate in the planning context.** Premortem runs in a context that did not
  author the plan. The retrospective is the completion transaction of the workflow, not a step.
- **English artifacts, mirrored conversation.** Repository files use English; conversation uses
  the user's language.
- **Write steps as scope becomes clear.** Persist decisions and open questions in files, not only
  in chat.

## Process

### 1. Frame the feature

State the problem, intended outcome, affected modules, compatibility constraints, and explicit
out-of-scope items. Run the intake round for facts the repository cannot answer.

### 2. Survey current state

Read in order:

1. Relevant sections of [`docs/ClaudeCodingGuide.md`](../../docs/ClaudeCodingGuide.md).
2. Relevant current-state pages under [`docs/architecture/`](../../docs/architecture/).
3. Relevant module documentation under [`docs/`](../../docs/).
4. Owning source, tests, and call sites.
5. Related dated feature records only when earlier delivery context is useful.

Treat architecture pages and code as current truth. Treat feature records as historical evidence
that may be stale.

For independent modules, delegate focused exploration in parallel. Require each exploration result
to return only file paths, relevant symbols with signatures, and a one-line role per symbol. Keep
long source dumps out of the planning context and write verified findings into the feature or steps.

Loop back to this survey whenever user answers or source discoveries change scope. Do not continue
with a stale decomposition.

### 3. Run the analysis checklist

Answer each question explicitly in the brief or the relevant step:

1. **Public API.** Which public or protected symbols of `src/SolTechnology.Core.*` are added,
   changed, or removed? State the semver impact and every `CLAUDE.md §2` action.
2. **Registration.** Does a `ModuleInstaller.cs` change? Name the extension method and the
   lifetime of every new registration.
3. **Build.** Does `Directory.Build.props`, a `.csproj`, or a pipeline file change?
4. **Persisted contracts.** Does a data shape stored or sent outside the process change (EF
   entity, serialized Tale context, MessageBus payload, Hangfire job argument, Redis entry, blob)?
   State how old data is read.
5. **Dependencies.** Is a NuGet package added or bumped? Route it through
   [`package-management`](../skills/package-management/SKILL.md).
6. **DreamTravel.** Which sample-app call sites and tests change?
7. **Documentation.** Which `docs/<Module>.md` and `docs/architecture/` pages describe the
   changed behavior?
8. **Premortem.** Does a `CLAUDE.md §4` trigger apply (questions 2, 3, 4)? If yes, set
   `premortem: pending`; otherwise record a dated waiver reason.

### 4. Evaluate alternatives

For a hard-to-reverse design choice — module boundaries, public API shape, persistence, a
platform dependency — record in the feature `Context`:

- At least two real alternatives, each with its strongest honest case. No strawmen; cite modules
  and files of this repository, not general practice.
- The strongest case against the chosen one: semver cost for NuGet consumers, new cross-module
  coupling, migration cost for the sample app, complexity moved instead of removed.
- The cruxes: where the cases disagree on fact, and the evidence that settles each (a benchmark,
  a prototype, a focused experiment).
- The decision: proceed, proceed with conditions, or reject, with a one-line reason.

Do not create a separate decision document.

### 5. Create the feature brief

Create `docs/features/YYYY-MM-DD-<kebab-name>.md` from the template in
[`delivery-workflow.md` → Feature brief](../../docs/architecture/delivery-workflow.md#feature-brief). Copy the template exactly.

Fill `Goal`, `Context`, `Scope`, `Implementation plan`, and `Acceptance criteria` during planning.
Leave `Blockers`, `Completion summary`, `Deviations`, and `Follow-ups` empty.
Use exact symbols, paths, package versions, and pass/fail acceptance criteria.

### 6. Decide whether steps are needed

Do not create a working folder for a change that fits one coherent implementation session: the
brief has one row and is the execution contract.

For multi-step work, create the working folder defined in
[`delivery-workflow.md` → Working folder](../../docs/architecture/delivery-workflow.md#working-folder).
Step status lives only in the brief's `## Implementation plan` table; a step file carries no
status and the brief never links to it. Premortem and retrospective are not steps.

For every step:

1. Create it as soon as its scope is clear; chat is not the durable store.
2. Keep one coherent, reviewable PR-sized concern. Split a step if a reviewer needs more than a few
  minutes to orient or if infrastructure plumbing and domain/application logic are mixed.
3. Keep inseparable pieces together: options with their consumer, client with request/response
  models, and schema change with its persistence mapping.
4. List exact files and mark each as `NEW`, `EDIT`, or `DELETE`.
5. Use pass/fail acceptance criteria. Name the minimum test set, the existing test project, and
  the exact validation command. Derive the set from the distinct code paths the step introduces,
  never one test per criterion. When the step reuses a path an earlier step covers, name that
  step and say this one does not repeat it; each step reads only its own file, so unstated
  overlap becomes duplicate tests.
6. Record unresolved decisions under `Open questions` and set the row to `blocked`; never invent
  an answer.
7. When inserting or splitting, renumber later files and update every row of the table in the
  same change.

Before finalizing, ask whether each step can be split without losing coherence. Split it when the
answer is yes.

Use the step template in [`delivery-workflow.md` → Step file](../../docs/architecture/delivery-workflow.md#step-file), with frontmatter
on line 1.

Prose belongs only in `Summary`. Use lists and tables elsewhere, one fact per bullet, with exact
identifiers instead of vague nouns. Steps carry contracts, not code: embed a code block only
where the exact text is itself the decision. Routine implementation code is written once, at
implementation time, with compiler and test feedback; the plan is read by the reviewer, the
premortem, the implementer, and the retrospective, and embedded code is paid for by each.

### 7. Set gates

Use one value per field from the table in
[`delivery-workflow.md` → Gates](../../docs/architecture/delivery-workflow.md#gates).

- `plan-review` is required for cross-module changes, breaking public API, persisted contracts,
  and material architecture changes. Otherwise it may be waived with a dated reason.
- `premortem` is required exactly for the triggers in `CLAUDE.md §4`. Otherwise it may be waived
  with a dated reason.
- `code-review` is always created as `pending`; it is mandatory and resolved after the last step.
- Only the user may skip a required gate; record their reason verbatim.

Do not perform risk analysis in the planner; the premortem skill does that later.

### 8. Hand off

If plan review is required, hand the brief and the working folder to the
[`plan-reviewer`](plan-reviewer.agent.md) agent. Never review your own plan. If plan review is
waived, return the plan for
[Approval](../../docs/architecture/delivery-workflow.md#approval); after it, apply the user's
answers. Implementation cannot start without `approved:`.

Before handoff, re-read the brief and every enumerated step, run `./.github/checkDocs.ps1`, and
verify:

- every affected path exists or is marked `NEW`;
- every product decision in the plan carries a user answer, or sits under `Open questions` with
  its row `blocked`;
- the eight checklist answers are in the brief or the steps;
- the gate fields match the risk triggers;
- no step mixes plumbing and domain logic or separates inseparable changes.

## Constraints

- Do not write production code, tests, pipeline configuration, or package files.
- Do not create or update files under a removed `docs/adr/` path.
- Do not maintain a `docs/features/README.md` index; feature status exists only in frontmatter.
- Do not update current architecture with behavior that has not shipped.
- Do not finalize a plan that contains a product decision the user has not answered.
- Always keep proposed design rationale in the feature brief until delivery proves it.
- Never execute the premortem or the retrospective in the planning context.
