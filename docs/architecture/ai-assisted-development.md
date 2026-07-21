# AI-Assisted Development

Repository-specific instructions are divided by responsibility:

| Artifact | Responsibility |
|---|---|
| `CLAUDE.md` | Operational protocol: behavior, tools, gates, and forbidden actions. |
| `docs/ClaudeCodingGuide.md` | Coding and documentation conventions. |
| `.github/agents/*.agent.md` | Roles that own multi-step workflows or require a fresh context. |
| `.github/skills/*/SKILL.md` | Focused procedures loaded on demand. |

One rule has one source of truth. Other artifacts link to it instead of copying it. Agents read
the relevant guide sections and every invoked agent or skill file before acting.

## Planning and delivery

Planning creates one dated feature brief under [`../features/`](../features/) before temporary
step files. It never creates a separate decision record. Architectural rationale discovered
during planning stays in the feature brief until implementation proves the design.

After delivery, the retrospective updates the applicable architecture page, completes the feature
record, and deletes the temporary working folder. Risk gates such as review and premortem are
selected from blast radius and contract impact, not from document type.

## Agent and skill split

Use an agent when the work needs a fresh context or a multi-turn role, such as planning, plan
review, or diagram production. Use a skill for a focused procedure, such as premortem, test
writing, package management, or documentation cleanup.

Agents and skills optimize for deterministic triggers, exact paths and symbols, concise
instructions, and verifiable completion criteria. They do not preserve process history in their
instruction files; that history belongs in dated feature records and Git.
