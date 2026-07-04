---
name: premortem
description: Imagine the change has shipped exactly as planned and broken production; write concrete failure stories, then sweep SolTechnology.Core module checklists for what the stories missed. Produces a Go / Go-with-mitigations / No-Go verdict. Runs as the 00 gate of a plan, always in a session that did not author the plan.
---

# Premortem

Pre-mortem methodology (Gary Klein) adapted for **SolTechnology.Core** — a collection of NuGet
packages consumed by external apps (e.g.
[DreamTravel](../../../sample-tale-code-apps/DreamTravel/),
plus public consumers of `SolTechnology.Core.*`
from NuGet.org).

Instead of asking "what could go wrong?", **assume the change already shipped and failed**, then
work backwards to explain why. Prospective hindsight produces concrete failure stories where a
forward-looking review produces vague worries.

## When to use

**Mandatory** before merging any change that touches:

- A public/protected symbol in `src/SolTechnology.Core.*` (NuGet API surface)
- Any `ModuleInstaller.cs` (DI registration contract)
- `Directory.Build.props` / `src/Directory.Build.props` (build / package metadata)
- A persisted contract (Story state shape, message contract, SQL migration)

**Recommended** for any change crossing module boundaries (e.g. CQRS ↔ Story ↔ Logging).

Within a plan, this skill executes the `00-run-premortem.md` gate
([ADR-006 §6](../../../docs/adr/006-implementation-plan-workflow.md)), invoked by
[`implement-plan`](../implement-plan/SKILL.md).

## Critical rules

- **Never in the authoring session.** A premortem run by the session that wrote the plan is
  anchored by it and rubber-stamps it. It must run in a fresh session (the `implement-plan`
  session satisfies this) or a spawned subagent.
- **Assume the change shipped exactly as planned.** A failure is not "we forgot to implement X"
  — plan defects belong to the [plan-reviewer](../../agents/plan-reviewer.agent.md), which runs
  before this gate. Here: "we shipped X exactly as designed and it still broke something".
- **Stories first, checklists second.** Generate failure narratives before opening any checklist.
  Checklist-seeded scenarios come out shallow and generic; the checklists exist to catch what
  the narratives missed, not to replace them.
- **No probability percentages.** Plausibility + impact, not maths.
- **Cite evidence.** Every scenario references a file path (and line where useful) — no
  speculation without code to back it. Read the real code the plan touches; a premortem against
  the plan text alone produces phantom risks.
- **Think second-order.** Consumers of the NuGet packages (sample apps + invisible public users)
  are part of the blast radius; the bug does not stop at the repo boundary.
- **No user questions.** Your output channels are the verdict, the mitigations, and the record —
  not a question round. A decision only the user can make = a *No-Go* (or *Go with mitigations*)
  stating exactly what must be decided; the planner owns the follow-up conversation.
- **Pair with [blue-red-team](../blue-red-team/SKILL.md)** when the change is a design decision,
  not just a code edit.
- **Cite Coding Guide sections by number** (§3, §11, §18) — if a reference looks wrong, the
  *reference* is stale, not the section.
- **English record; conversation mirrors the user** (ADR-006 §8).

## Process

### 1. Frame the change

Record:

- **Modules touched** (e.g. `SolTechnology.Core.CQRS`, `SolTechnology.Core.Story`)
- **API delta** — added / removed / changed public types, members, signatures, attributes
- **Semver impact** — `MAJOR` / `MINOR` / `PATCH`, tied to concrete diff evidence
- **Consumers in the workspace** — search `sample-tale-code-apps/**` for
  `using SolTechnology.Core.<Module>`
- **External consumers** — assume public NuGet downloaders exist; their code is invisible

Read the actual source the plan touches. Delegate bulk reading to `Explore` subagents; end every
task prompt with:

> Return ONLY: (1) file paths, (2) relevant type/member names with signatures, (3) a one-line
> role for each. No file contents, no code blocks longer than a signature, max ~40 lines.

### 2. Imagine the failure

Pick a moment in the future (next release, next quarter). **The change has failed
spectacularly.** Describe the worst-credible end state in one paragraph. Examples:

- "DreamTravel API returns 500 on every request after upgrading `SolTechnology.Core.Logging`."
- "A community consumer files an issue: their `dotnet build` fails with NU1605 after we bumped a
  transitive `Microsoft.Extensions.*` reference."
- "Story workflows resumed after deploy crash on deserialization."

### 3. Write the failure stories (narrative first — before any checklist)

Generate **5–10 concrete stories** from the end state, working backward. Each story answers:

- **What went wrong** — one sentence.
- **Trigger** — the specific input, upgrade path, config, or race, anchored to file:line in the
  diff/plan.
- **Mechanism** — the causal chain through named symbols (`Type.Member`, package, config key).
- **What warning signs were missed / which assumption proved incorrect.**

Cover the steps individually **and** the plan as a whole: integration seams between steps,
ordering across PRs, partial-upgrade states at consumers.

### 4. Sweep the module checklists (supplementary — catches what stories missed)

Lazy-loaded — read **only** the references matching the modules in the diff. Every bullet is
worth one sentence of evidence before dismissing it. Add any newly surfaced scenario to the list
from §3.

| Module(s) in diff | Reference file |
|---|---|
| `SolTechnology.Core.CQRS` | [`references/cqrs.md`](references/cqrs.md) |
| `SolTechnology.Core.Tale` | [`references/story.md`](references/story.md) |
| `SolTechnology.Core.Logging` | [`references/logging.md`](references/logging.md) |
| `SolTechnology.Core.HTTP`, `SolTechnology.Core.ApiClient` | [`references/http.md`](references/http.md) |
| `SolTechnology.Core.MessageBus` | [`references/messagebus.md`](references/messagebus.md) |
| `SolTechnology.Core.SQL` | [`references/sql.md`](references/sql.md) |
| `SolTechnology.Core.Blob` | [`references/blob.md`](references/blob.md) |
| `SolTechnology.Core.Cache` | [`references/cache.md`](references/cache.md) |
| Any `ModuleInstaller.cs` change | [`references/di.md`](references/di.md) |
| `Directory.Build.props`, TFM bump | [`references/build-and-nuget.md`](references/build-and-nuget.md) |

If the diff touches a module without a reference file, write one (same shape as the existing
files) in the **same** PR so the next contributor inherits the checklist.

### 5. Score blast radius, severity, likelihood

For each scenario:

- **Blast radius** — internal-only / sample apps / public NuGet consumers / all.
- **Severity** — `L` (cosmetic) / `M` (functional regression, recoverable) / `H` (data loss,
  breaking API, broken build for consumers).
- **Likelihood** — `L` / `M` / `H` by how easy the trigger is to hit in normal use.

Drop scenarios with no realistic trigger — the record must not be a laundry list; every entry
earns its place. Confirming an already-defended scenario is valuable output: mark it against its
existing control.

### 6. Map to existing controls and propose mitigations

For each surviving scenario:

- **Existing control** — test in `tests/SolTechnology.Core.<Module>.Tests/`, guard, ADR, doc,
  lint, build setting. If none: `none — gap`.
- **Mitigation** — one of: add/extend a test (link the test class); add a `Guards.*`
  precondition; document a breaking change in `docs/adr/` + bump major; add a runtime check
  (`ModuleInstaller` validation); update `docs/ClaudeCodingGuide.md` so the failure mode is
  prevented for future contributors; defer with explicit rationale ("accepted risk").
- Every mitigation names the **step file** that must carry it.
- Where the failure is runtime-observable at a consumer (a log line, exception type, health
  check), name that early-warning signal alongside the mitigation. For build-time failures the
  existing control (test, guard, analyzer) plays this role — do not invent runtime telemetry a
  library cannot own.

### 7. Decide

- **Go** — no `H` severity, all `M` covered by existing controls, semver classification matches
  the diff.
- **Go with mitigations** — required mitigations listed; the executing session
  ([`implement-plan`](../implement-plan/SKILL.md)) folds each into the named step file before
  the gate flips to `done`.
- **No-Go** — at least one `H` scenario with no plausible mitigation, or a decision only the
  user can make. State exactly what blocks and what would unblock; the plan returns to the
  planner.

Record the full output (format below) in `00-run-premortem.md` under the gate's heading; the
invoking session mirrors the verdict into the summary's `premortem:` field.

## Output format

### Premortem — `<change title>`

#### Frame

- **Modules touched**: <placeholder>list</placeholder>
- **API delta**: <placeholder>added/removed/changed</placeholder>
- **Semver impact**: <placeholder>MAJOR / MINOR / PATCH</placeholder>
- **Consumers in workspace**: <placeholder>list</placeholder>

#### Imagined Failure

<placeholder>One-paragraph worst-credible end state.</placeholder>

#### Scenarios

| # | Story (what + mechanism) | Trigger (file:line) | Blast radius | Sev | Lik | Existing control | Mitigation → step |
|---|---|---|---|---|---|---|---|
| 1 | <placeholder> | <placeholder> | <placeholder> | L/M/H | L/M/H | <placeholder> | <placeholder> |

#### Top 3 Risks

1. <placeholder>scenario # + one-line why</placeholder>

#### Required Mitigations Before Merge

- <placeholder>concrete action + file path / test name + owning step file</placeholder>

#### Accepted Risks

- <placeholder>scenario # + rationale</placeholder>

#### Decision

**Go / Go with mitigations / No-Go** — <placeholder>one-line reason</placeholder>