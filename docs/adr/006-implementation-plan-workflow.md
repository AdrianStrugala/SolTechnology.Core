# ADR-006: Implementation Plan Workflow

> **Status:** Accepted
> **Decision Date:** 2026-05-25
> **Decision Maker:** Repository maintainers
> **Stakeholders:** Contributors using Claude Code / Copilot / other AI agents

---

## Context

The repo has ADRs ([`docs/adr/`](.)) for design decisions and skills
([`.github/skills/`](../../.github/skills/)) for procedures. It is missing:

1. **A persistent place for multi-step task plans.** Plans currently live in chat history and
   evaporate between sessions. Agents and humans cannot resume a half-finished task.
2. **A status tracker across ADRs.** No single page shows which ADRs are Accepted vs Implementing
   vs Done vs Superseded.
3. **A clear split between agents (roles) and skills (procedures).** Both currently sit under
   `.github/skills/`; some entries are roles (`implementation-planning`), others are procedures
   (`code-review`, `commit-message`).

## Decision

### 1. Plans live next to the ADR that drives them

```
docs/adr/
  README.md                                  ← index + workflow conventions (this ADR + table of all ADRs)
  NNN-feature-name.md                        ← the ADR (decision)
  NNN-feature-name/                          ← OPTIONAL sibling folder when the ADR needs multi-step work
    summary.md                               ← step table; updated as work progresses
    to-do/
      01-step-title.md
      02-step-title.md
    reviewed/                                ← drafts produced by the `plan-reviewer` agent
    done/
      01-step-title.md
```

Rules:

- **Step filenames are numeric, kebab-case, no dates.** `01-step-title.md`, `02-step-title.md`.
  Dates rot when steps shift order during review.
- **`to-do/` / `reviewed/` / `done/` are mutually exclusive states.** A step file lives in exactly
  one folder at any time. The `plan-reviewer` agent writes drafts to `reviewed/` and deletes
  originals from `to-do/`; the `implement-plan` skill moves files from `to-do/` (or `reviewed/`)
  to `done/` after execution.
- **`summary.md` is the source of truth for status.** Each row points to the current location of
  its step file. Status column uses `⬜ to-do` / `🔍 reviewed` / `✅ done`.
- **ADRs without multi-step implementation have no sibling folder.** ADR-001 / ADR-003 / ADR-005
  remain flat files.

### 2. Index tracks every ADR

[`docs/adr/README.md`](README.md) is the index. It contains:

- The plan-workflow conventions above (single source of truth, mirrored from this ADR).
- A table of every ADR with: number, title, status, implementation status, link.

Whenever an ADR is added or its status changes, the index updates in the same change.

### 3. Agents vs skills split

`.github/agents/` for roles. `.github/skills/` for procedures.

| Kind | Path | Purpose |
|---|---|---|
| **Agent** | `.github/agents/<name>.agent.md` | A role / persona with toolset + handoffs. Owns a multi-step workflow. |
| **Skill** | `.github/skills/<name>/SKILL.md` | A narrow procedure. Loaded on demand for a single task. |

The existing `implementation-planning` entry migrates from `skills/` to `agents/`. Other current
entries (`code-review`, `premortem`, `blue-red-team`, `commit-message`, `documentation-cleanup`)
stay as skills.

### 4. Mermaid is the diagram syntax

Sequence / component / flow diagrams use Mermaid (GitHub renders natively). Diagrams produced by
agents live in `docs/diagrams/`.

## Alternatives Considered

1. **`.github/work/<task>/`** as the plan location. Rejected: `docs/adr/` already carries the
   decision context; plans drift if separated from the ADR that motivated them.
2. **Date-prefixed step filenames (`YYYY-MM-DD-title.md`).** Rejected: dates lock ordering;
   re-numbering after a review shuffle is painful and obscures the new sequence.
3. **Keep `agents/` and `skills/` unified.** Rejected: roles and procedures have different read
   patterns. Agents need broad context; skills are narrow. A single folder forces the agent to
   guess which kind a file is.

## Consequences

**Positive**

- Multi-step tasks survive across sessions; agents can resume.
- Status of every ADR visible in one table.
- `plan-reviewer` and `implement-plan` have a fixed contract (folder names, file naming) to rely
  on — fewer ambiguity-driven failures.

**Negative**

- ADR folder grows two layers deep for any ADR with implementation. Discoverability stays via
  the index.
- One-time migration: existing `implementation-planning` skill moves to `agents/`. Cross-refs
  must be updated in the same change (see [ADR-004](004-ai-agents-and-skills.md), `CLAUDE.md §3`,
  `ClaudeCodingGuide §19`).

**Semver impact:** PATCH (docs-only).

## Related

- [ADR-004](004-ai-agents-and-skills.md) — the agents-and-skills mechanism this ADR extends.
- [`docs/ClaudeCodingGuide.md` §19](../ClaudeCodingGuide.md) — AI-only documentation rules.
- [`CLAUDE.md` §3](../../CLAUDE.md) — skill / agent index.

## Implementation summary

Completed 2026-05-25. The per-step working folder (`docs/adr/006-implementation-plan-workflow/`)
was deleted per the collapse-on-completion rule (Decision §5). This summary is the durable
record.

| # | Step | Shipped |
|---|---|---|
| 01 | Lock conventions in ADR-006 + index | This ADR file + [`docs/adr/README.md`](README.md) Plan workflow section. |
| 02 | Agents / Skills split + skill audit | `.github/agents/` created, `.github/skills/implementation-planning/` migrated to [`.github/agents/implementation-planning.agent.md`](../../.github/agents/implementation-planning.agent.md), audit applied to remaining skills. |
| 03 | Skill `roast-me` | [`.github/skills/roast-me/SKILL.md`](../../.github/skills/roast-me/SKILL.md). |
| 04 | Skill `package-management` + canonical-versions table | [`.github/skills/package-management/SKILL.md`](../../.github/skills/package-management/SKILL.md) + `references/canonical-versions.md`. |
| 05 | §15 anti-patterns: entity leak + split schema change | [`docs/ClaudeCodingGuide.md`](../ClaudeCodingGuide.md) §15 table rows. |
| 06 | Skill `implement-plan` + first-consumer demonstration | [`.github/skills/implement-plan/SKILL.md`](../../.github/skills/implement-plan/SKILL.md); this ADR was the first consumer. |
| 07 | Agent `plan-reviewer` | [`.github/agents/plan-reviewer.agent.md`](../../.github/agents/plan-reviewer.agent.md), wired into [`agents/README.md`](../../.github/agents/README.md), CLAUDE.md §3, and the planner handoff. |
| 08 | Agent `diagram` + Mermaid conventions + `docs/diagrams/` | [`.github/agents/diagram.agent.md`](../../.github/agents/diagram.agent.md) + [`docs/diagrams/README.md`](../diagrams/README.md); CLAUDE.md §2 enforces routing through the agent. |
| 09 | "Refuse if tool unavailable" rule + `temp/` cleanup | CLAUDE.md §2 row added; `roast-me`, `implement-plan`, `diagram` reinforce the rule in their failure-modes / constraints sections; `temp/` removed from repo root. |

### Preserved deviations

- **Step 07** — the original `to-do/07-agent-plan-reviewer.md` shipped with its frontmatter and
  sections written in reverse order (frontmatter at the bottom, `# Step 07` near the end).
  `implement-plan` had to rewrite the file in canonical order before executing the step.
  Lesson: when authoring step files, always verify frontmatter is line 1 before yielding.

