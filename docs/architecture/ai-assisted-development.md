# AI-Assisted Development

Repository-specific instructions are divided by responsibility:

| Artifact | Responsibility |
|---|---|
| `CLAUDE.md` | Operational protocol: behavior, tools, gates, and forbidden actions. |
| `docs/ClaudeCodingGuide.md` | C# coding conventions. |
| `docs/PublicDocumentationGuide.md` | Public module documentation authoring conventions. |
| `.github/agents/*.agent.md` | Roles that own multi-step workflows or require a fresh context. |
| `.github/skills/*/SKILL.md` | Focused procedures loaded on demand. |

One rule has one source of truth. Other artifacts link to it instead of copying it. Agents read
the relevant guide sections and every invoked agent or skill file before acting. `CLAUDE.md` §4 is
the only index of agents and skills; `checkDocs.ps1` fails when a file on disk is missing from it.

Instruction files state only rules that are specific to this repository and that no tool
enforces; repository context files raise cost without raising task success unless they are
limited to non-standard practices ([Evaluating AGENTS.md, 2026](https://arxiv.org/abs/2602.11988)).

GitHub Copilot and Claude Code both run the same files. `.github/copilot-instructions.md` points
Copilot at `CLAUDE.md`; `CLAUDE.md` §4 states how each tool invokes an agent or a skill.

## Enforcement

A rule a machine can check is enforced by a machine; prose carries only what needs judgment.

| Layer | Enforces | Where |
|---|---|---|
| Compiler | Braces (IDE0011), log templates without interpolation or concatenation (CA2254). A warning fails the build under `src/`. | `.editorconfig`, `EnforceCodeStyleInBuild` in the root `Directory.Build.props` |
| `checkConventions.ps1` | `#region`, log `[]` brackets, `Async` suffix, stub code — on added C# lines only, so existing debt is reported and never auto-fixed. | `.github/checkConventions.ps1` |
| `checkDocs.ps1` | Link targets, heading anchors, § citation existence, agent / skill index parity; for feature briefs and active plans: frontmatter, status vocabulary, one value per gate field, plan rows matching step files, no working folder after completion, and an overdue retrospective. | `.github/checkDocs.ps1` |
| CI | Both scripts on every pull request. | `.github/workflows/checks.yml` |
| `verify.ps1` | One command for build, core tests, the DreamTravel Unit and Component tests, and both scripts. Its exit code is the acceptance signal for a Small change and, inside the code review, for a Feature; a plan step runs only its own tests. | `.github/verify.ps1` |

## Human control points

The user approves every plan (`approved:` in the feature brief; no implementation starts without
it), confirms every action on the `CLAUDE.md` §2 list, and confirms every change to an instruction
file. An agent proposes a new rule; it does not write one on its own initiative.

## Delivery

One method delivers every change: the [delivery workflow](delivery-workflow.md). That page holds
the phases, the feature-record rules, and the evidence behind each design choice. The
[`deliver`](../../.github/skills/deliver/SKILL.md) skill is its entry point. Risk gates such as
review and premortem are selected from blast radius and contract impact, not from document type.

## Agent and skill split

Use an agent when the work needs a fresh context or a multi-turn role, such as planning, plan
review, or diagram production. Use a skill for a focused procedure, such as premortem, test
writing, package management, or documentation cleanup.

Agents and skills optimize for deterministic triggers, exact paths and symbols, concise
instructions, and verifiable completion criteria. They do not preserve process history in their
instruction files; that history belongs in dated feature records and Git.
