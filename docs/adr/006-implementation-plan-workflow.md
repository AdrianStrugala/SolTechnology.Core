# ADR-006: Implementation Plan Workflow

> **Status:** Accepted
> **Decision Date:** 2026-05-25 (amended 2026-06-24, 2026-06-29, 2026-07-04)
> **Decision Maker:** Repository maintainers
> **Stakeholders:** Contributors using Claude Code / Copilot / other AI agents

---

This ADR is the **single source of truth** for the plan workflow. Every agent and skill that
touches plan files (`implementation-planning`, `plan-reviewer`, `implement-plan`, `premortem`)
MUST read this ADR before touching any plan file — no exceptions. Agents reference this document;
they never restate its rules in full (restated copies drift).

## Context

The repo has ADRs ([`docs/adr/`](.)) for design decisions and skills
([`.github/skills/`](../../.github/skills/)) for procedures. It was missing:

1. **A persistent place for multi-step task plans** — plans lived in chat and evaporated.
2. **A status tracker across ADRs and features.**
3. **A clear split between agents (roles) and skills (procedures).**
4. *(2026-07-04)* **A hard mechanism keeping the pipeline order** (plan → review → premortem gate
   → implementation → retrospective) that makes any skipped stage a recorded, user-authorized
   decision instead of an accident.
5. *(2026-07-04)* **Explicit rules for writing style and language** of plan artifacts.

## Decision

### 1. All working folders live under `docs/features/`; features are date-named (Amendment 2026-07-04)

```
docs/features/
  README.md                             ← feature index (status tracking only)
  YYYY-MM-DD-<feature>.md               ← feature spec
  YYYY-MM-DD-<feature>/                 ← working folder (exists only while work is in flight)
    summary.md                          ← step table + pipeline gate fields (frontmatter)
    steps/
      00-run-premortem.md               ← opening bracket (only when premortem required — §6)
      01-<step-title>.md
      NN-retrospective.md               ← closing bracket, always the highest number (§6)

docs/adr/
  README.md                             ← decision index (allocates ADR numbers)
  NNN-<title>.md                        ← the ADR — a decision record, never a working folder
```

- **Features are date-prefixed** (`YYYY-MM-DD-<kebab-name>`); **ADRs keep `NNN`**; **steps keep
  `NN`, no dates**. Rationale: dates are wrong exactly where re-ordering happens (steps get
  renumbered during review) and right where it never does (features are independent units;
  chronology is stable, self-sorting, and removes the "read the index to pick the next free
  number" round-trip). ADRs stay numbered because decisions cite each other by number.
- **An ADR never has a sibling working folder.** A decision needing multi-step work gets a
  companion feature plan: the ADR carries `Implemented via
  [YYYY-MM-DD-<feature>](../features/YYYY-MM-DD-<feature>.md)`; the feature spec links back. The
  spec may be thin (`Goal: implement ADR-NNN`) — that is fine.

### 2. Step state is a frontmatter field, not a folder (Amendment 2026-07-04)

Step files never move. Each carries:

```yaml
---
spec: YYYY-MM-DD-<feature>
step: NN
status: to-do        # to-do | blocked | in-progress | done
---
```

- **Status vocabulary:** `to-do | blocked | in-progress | done`.
- **Derivation rule for `blocked`:** a step with ≥ 1 unanswered entry in its `Open questions`
  section MUST have `status: blocked`. Whoever answers the last question (inline, below the
  question — the question stays) flips the status back in the same edit. Whoever adds a question
  flips it to `blocked` in the same edit.
- **The step file's frontmatter is the source of truth.** `summary.md`'s step table is a
  snapshot; whoever flips a step's status updates the matching summary row **in the same change**.
- Summary status cells: `⬜ to-do` / `⛔ blocked` / `🔧 in-progress` / `✅ done`.
- There is **no per-step `reviewed` status**. Review is a plan-level gate (§7); the review trail
  is the git diff plus the critique. A step's status tracks execution only.
- **Renumbering** (when inserting or splitting a step): rename subsequent files — the
  **retrospective always keeps the highest number** — update every link in `summary.md`, verify
  no link under the working folder dangles.
- Rationale for dropping folder states: the previous `to-do/` → `reviewed/` → `done/` moves made
  every relative link fragile — two skills carried compensating verification steps only because
  files moved. A field flip is one edit, zero moved files, zero broken links, and `git log`
  still shows the full lifecycle.

### 3. Indexes track every ADR and every feature

[`docs/adr/README.md`](README.md) and [`docs/features/README.md`](../features/README.md) each
hold a table: identifier, title, status, implementation status, link. Whenever an entry is added
or its status changes, the index updates in the same change. The ADR index also allocates ADR
numbers; the feature index tracks status only (dates self-allocate).

### 4. Agents vs skills split

`.github/agents/` for roles. `.github/skills/` for procedures.

| Kind | Path | Purpose |
|---|---|---|
| **Agent** | `.github/agents/<name>.agent.md` | A role / persona with toolset + handoffs. Owns a multi-step workflow or needs a fresh context window (a reviewer must not be anchored by the session that produced the plan). |
| **Skill** | `.github/skills/<name>/SKILL.md` | A narrow procedure or methodology, loaded on demand by whoever is currently working. Composable; supports lazy-loaded references. |

Decision test: *needs its own context window or a multi-turn user conversation → agent; is a
procedure/checklist/format executed inside another workflow → skill.*

### 5. Mermaid is the diagram syntax

Sequence / component / flow diagrams use Mermaid. Diagrams produced by agents live in
`docs/diagrams/`.

### 6. The plan is bracketed: premortem `00` opens it, the retrospective closes it (Amendments 2026-06-24, 2026-07-04)

**Opening bracket — `00-run-premortem.md`** (exists only when `premortem: pending`, §7):

- **Authored last, numbered first.** The planner writes it once all `01..NN-1` steps exist. The
  "lowest-numbered `to-do` first" rule sorts it ahead of implementation structurally.
- **Enforcement lives in the gate field, not the number** *(reframed 2026-07-04)*: since
  `implement-plan` hard-checks the summary's `premortem:` field before any `01..NN` step, the
  `00` numbering is defence-in-depth, not the mechanism. The file's real roles are: the **brief**
  (modules touched, API delta, applicable checklists, links to steps) and the **durable record**
  (scenario table, accepted risks, verdict) — consultable during implementation and kept out of
  `summary.md`, which every `implement-plan` invocation reads and must stay lean.
- Executing `00` = running the [premortem](../../.github/skills/premortem/SKILL.md) skill **in a
  session that did not author the plan** (the `implement-plan` session satisfies this), recording
  the full output in the step file and the verdict in the `premortem:` field. It touches no
  `src/` code. *No-Go* blocks the plan until mitigations are folded into steps and the premortem
  re-runs. The premortem asks the user nothing — its channel is the verdict; user-facing
  questions belong to the planner (intake) and the plan-reviewer (critique).

**Closing bracket — `NN-retrospective.md`** *(new 2026-07-04)*:

- **Always the highest-numbered step**, authored by the planner together with the plan. It
  becomes the lowest `to-do` only when everything else is `done` — the sequencing rule triggers
  it structurally.
- Executing it (via `implement-plan`, docs-only, own invocation ⇒ fresh session):
  1. **Review the whole feature against the plan** — diff plan vs delivered code per step and
     across steps (integration seams between PRs); catch deviations visible in code but absent
     from `## Deviations`; note residual tech debt and follow-ups.
  2. **Consolidate** — absorb `summary.md` (gate verdicts) and every step's outcome, preserved
     deviations, and follow-ups into the spec's `## Implementation summary`.
  3. **Verify, then collapse** — re-read the summary section for completeness and dead links
     **before** deleting the working folder. The collapse is one transaction: consolidate first,
     delete second.
- Rationale for a dedicated step: previously the collapse ran as an afterthought at the end of
  the last coding invocation — full-context sessions consolidate poorly — and nothing ever
  reviewed the feature as a whole.

### 7. Pipeline gates and conscious skips (Amendment 2026-07-04)

The pipeline is: **plan → review → premortem gate → implement (per step) → retrospective.**
Prompts alone cannot enforce it — enforcement is structural, via gate fields that downstream
stages hard-check.

`summary.md` frontmatter (grammar):

- `review:` — `pending` | `waived (<reason — planner, date>)` | `done (<date>)` |
  `skipped (<reason — user, date>)`
- `premortem:` — `pending` | `waived (<reason — planner, date>)` | `go (<date>)` |
  `go-with-mitigations (<date>)` | `no-go (<date>)` | `skipped (<reason — user, date>)`

Rules:

- **The planner sets each field at plan creation** to `pending` (stage required) or
  `waived(...)`, per these criteria — recorded, never implicit:
  - `review` required for: ADR-driven plans, plans crossing two or more modules, breaking
    public-API changes. Otherwise the planner may waive with a reason.
  - `premortem` required when the plan touches any premortem-mandatory trigger (public NuGet API
    surface, `ModuleInstaller.cs`, `Directory.Build.props`, persisted contracts — see the
    [premortem skill](../../.github/skills/premortem/SKILL.md)). Otherwise waivable with a
    reason; then no `00` file exists.
- **Only the user may `skip` a required stage.** The agent records the user's reason verbatim,
  with date, and never sets `skipped` on its own initiative. This is what makes every deviation
  a conscious user choice: it exists only as a recorded artifact.
- **Hard preconditions in `implement-plan`:** refuse `00` while `review: pending`; refuse any
  `01..NN` step unless `premortem` ∈ {`go`, `go-with-mitigations`, `waived`, `skipped`} and
  (when `00` exists) `00` is `done`; refuse the retrospective while any other step is not
  `done`. A missing or malformed gate field blocks execution — report, never guess.
- **Cancellation.** If the user abandons a feature, run a **retro-lite** via the retrospective
  step: mark the spec `Abandoned` with the reason and each step's last state, then collapse as
  usual. No working folder outlives its feature.
- **Recommended hardening (not yet implemented):** a CI validator script — status vocabulary,
  gate-field grammar, "no step beyond `to-do` while `00` pending", link resolution. The only
  deterministic guarantee; file it as its own feature plan.

### 8. Writing style for plan artifacts (Amendment 2026-07-04)

Binds everyone who writes or edits step files: the planner, the plan-reviewer, and mitigation
folding during the `00` gate. Step files are read by busy engineers and executing agents —
optimise for fast scanning and unambiguous execution.

- **Prose is allowed ONLY in a step's `Summary` section** (one short paragraph: what and why).
  Every other section is concrete and technical — no narrative, no justification, no filler,
  no hedging.
- **Plain language.** Short sentences. The simplest word that is still precise.
- **Lists and tables over paragraphs.** Bullets for changes, numbered lists for ordered work,
  tables for field/test matrices. One fact per bullet.
- **Exact identifiers.** Name the symbol (`AddHealthChecks`, `HttpOptions.RequestTimeout`), the
  file path, the option key, the `package@version` — never "the config" or "the service".
  Identifiers in backticks; multi-line code only in short copyable fenced blocks.
- **Acceptance criteria are pass/fail checkboxes** — a build, a test, an endpoint returning X.
  Never "works correctly" or "is tested".
- **Tests as the minimum covering set**: consolidate same-scenario asserts into one test;
  parameterize same-logic-different-data (xUnit `[Theory]`/`[InlineData]` per
  `ClaudeCodingGuide §8`); split only for genuinely different behaviour. Never a wall of
  near-duplicate rows.
- Exceptions by nature: the premortem's failure stories and the spec's `Goal` may be short
  narrative paragraphs — still specific, still evidence-anchored.

### 9. Language (Amendment 2026-07-04)

- **All repository artifacts are written in English**: ADRs, feature specs, step files,
  summaries, indexes, agent and skill files, code comments, commit messages.
- **Conversation with the user mirrors the user's language** — Polish question, Polish answer —
  while artifacts stay English.
- Identifiers, file paths, and quoted symbols are never translated in either direction.

## Alternatives Considered

1. **`.github/work/<task>/`** as the plan location. Rejected: plans drift when separated from
   the spec that motivated them.
2. **Date-prefixed step filenames.** Rejected for steps: dates lock ordering where re-numbering
   happens. *(2026-07-04)* **Accepted for features**, where ordering never changes and dates
   self-allocate — see Decision §1.
3. **Keep `agents/` and `skills/` unified.** Rejected: roles and procedures have different read
   patterns.
4. **(2026-06-24) Premortem numbering.** *Last-numbered + "run first" note* — rejected: relies
   on prose. *Non-file row in `summary.md`* — rejected then; *(2026-07-04)* revisited once gate
   fields made the numbering redundant as enforcement — still rejected, because the record
   (scenario table, accepted risks) needs a durable home that keeps `summary.md` lean on the
   hot read path. The file stays; its rationale changed (see §6).
5. **(2026-06-29) File features as ADRs** — rejected: dilutes "one ADR = one decision".
6. **(2026-07-04) Keep folder states (`to-do/`/`reviewed/`/`done/`)** — rejected: every file
   move invalidated relative links; one preserved deviation (step 07) already documents a
   file-shape failure in this machinery. *Frontmatter status field* — chosen.
7. **(2026-07-04) Working folders next to ADRs** — rejected: two parents with identical
   machinery; the ADR mutated from decision record into implementation tracker. *One parent
   under `docs/features/`* — chosen.
8. **(2026-07-04) Pipeline enforced by prompt rules** — rejected: prompts are advisory. *Gate
   fields + hard preconditions (+ optional CI validator)* — chosen.
9. **(2026-07-04) Collapse embedded in the last coding invocation** — rejected: consolidation
   done as an afterthought in a full-context session is low quality, and nothing reviewed the
   feature as a whole. *Dedicated retrospective bracket step* — chosen: same machinery as `00`,
   fresh session guaranteed by one-step-per-invocation.

## Consequences

**Positive**

- Multi-step tasks survive across sessions; agents resume from files, not chat.
- Every stage skip is user-authorized and recorded — auditable pipeline.
- No file moves → no broken relative links; `git log` shows state history.
- Feature dates self-allocate; the ADR index stays a pure decision log.
- Every feature ends with a whole-feature review before its working docs disappear.

**Negative**

- ADR-driven work costs one extra (possibly thin) feature spec.
- Status changes touch two places (step frontmatter + summary row) in one edit — mitigated by
  the derivation rules and, eventually, the CI validator.
- One-time migration of in-flight plan folders to the new layout.

**Semver impact:** PATCH (docs-only).

## Related

- [ADR-004](004-ai-agents-and-skills.md) — the agents-and-skills mechanism this ADR extends.
- [`docs/ClaudeCodingGuide.md` §19](../ClaudeCodingGuide.md) — AI-only documentation rules.
- [`CLAUDE.md` §3](../../CLAUDE.md) — skill / agent index.

## Implementation summary

Completed 2026-05-25. The per-step working folder (`docs/adr/006-implementation-plan-workflow/`)
was deleted per the collapse-on-completion rule. This summary is the durable record.

| # | Step | Shipped |
|---|---|---|
| 01 | Lock conventions in ADR-006 + index | This ADR file + [`docs/adr/README.md`](README.md) Plan workflow section. |
| 02 | Agents / Skills split + skill audit | `.github/agents/` created, `implementation-planning` migrated to [`.github/agents/implementation-planning.agent.md`](../../.github/agents/implementation-planning.agent.md), audit applied to remaining skills. |
| 03 | Skill `roast-me` | [`.github/skills/roast-me/SKILL.md`](../../.github/skills/roast-me/SKILL.md). |
| 04 | Skill `package-management` + canonical-versions table | [`.github/skills/package-management/SKILL.md`](../../.github/skills/package-management/SKILL.md) + `references/canonical-versions.md`. |
| 05 | §15 anti-patterns: entity leak + split schema change | [`docs/ClaudeCodingGuide.md`](../ClaudeCodingGuide.md) §15 table rows. |
| 06 | Skill `implement-plan` + first-consumer demonstration | [`.github/skills/implement-plan/SKILL.md`](../../.github/skills/implement-plan/SKILL.md); this ADR was the first consumer. |
| 07 | Agent `plan-reviewer` | [`.github/agents/plan-reviewer.agent.md`](../../.github/agents/plan-reviewer.agent.md), wired into [`agents/README.md`](../../.github/agents/README.md), CLAUDE.md §3, and the planner handoff. |
| 08 | Agent `diagram` + Mermaid conventions + `docs/diagrams/` | [`.github/agents/diagram.agent.md`](../../.github/agents/diagram.agent.md) + [`docs/diagrams/README.md`](../diagrams/README.md); CLAUDE.md §2 enforces routing through the agent. |
| 09 | "Refuse if tool unavailable" rule + `temp/` cleanup | CLAUDE.md §2 row added; `roast-me`, `implement-plan`, `diagram` reinforce the rule; `temp/` removed from repo root. |

### Preserved deviations

- **Step 07** — the original `to-do/07-agent-plan-reviewer.md` shipped with its frontmatter and
  sections written in reverse order. `implement-plan` had to rewrite the file in canonical order
  before executing the step. Lesson: when authoring step files, always verify frontmatter is
  line 1 before yielding.