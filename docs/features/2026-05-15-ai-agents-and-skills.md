---
status: completed
created: 2026-05-15
completed: 2026-05-15
---

# AI Agents and Skills

> Historical delivery record. It may not describe the current workflow.

## Goal

Establish repository-specific agents and skills for risk analysis, review, planning, package
management, testing, and documentation maintenance.

## Context

AI assistants participated in most changes but lacked a shared operational protocol and reusable
task procedures. Risk analysis, review, commit formatting, planning, and documentation checks were
performed inconsistently, especially around public NuGet APIs, module installers, persisted
contracts, and repository-wide guidance.

The repository wanted reusable procedures that any supported assistant could load on demand while
keeping normative doctrine in one place.

## Original decision

Adopt `.github/skills/<name>/SKILL.md` as the format for narrow procedures and keep repository-wide
agent doctrine in `CLAUDE.md` rather than introducing a parallel `AGENTS.md`.

The initial catalog included:

| Capability | Responsibility |
|---|---|
| `premortem` | Failure stories, module-specific risks, semver impact, and go/no-go verdict. |
| `blue-red-team` | Supportive and skeptical evaluation of a design choice. |
| `documentation-cleanup` | Module/doc parity, indexes, links, and decision-document integrity. |
| `code-review` | Review against the Coding Guide and module conventions. |
| `commit-message` | Conventional Commit formatting with module scopes and semver notes. |
| `implementation-planning` | Decompose non-trivial work into a reviewed, risk-gated plan. |

Skills were to be repository-local adaptations rather than an upstream submodule. Every skill or
agent had to be read before invocation, and findings had to cite concrete repository evidence.

## Alternatives considered

### Use an upstream skills repository as a submodule

Rejected because it would import unrelated domain procedures and create an external synchronization
boundary for core repository workflow.

### Add a separate `AGENTS.md`

Rejected because overlapping operational doctrine in `AGENTS.md` and `CLAUDE.md` would drift.

### Inline all procedures in `CLAUDE.md`

Rejected because large always-loaded instructions would be harder to maintain and could not be
selected independently by different tools.

### Adopt only premortem

Rejected because review, commit, and documentation procedures already existed informally and were
cheap to formalize as one coherent toolkit.

## Scope

- Keep operational protocol in `CLAUDE.md`.
- Store reusable procedures under `.github/skills/`.
- Store multi-step roles under `.github/agents/`.
- Require read-before-use and evidence-based outputs.

## Implementation plan

Adapt the skills mechanism to this repository, add the initial catalog, and connect it to the
coding guide and risk gates. Keep descriptions as routing hints while making each skill file the
authoritative procedure loaded at execution time.

## Acceptance criteria

- Agents and skills have distinct responsibilities.
- Instructions use one canonical source per rule.
- Risk-sensitive changes invoke premortem independently of feature documentation.

## Completion summary

The initial agent and skill library shipped in `.github/`, with `CLAUDE.md` as the operational
entry point and mandatory read-before-use behavior. The catalog later expanded with package,
dependency, test-writing, use-case authoring, refactoring, and plan-execution procedures.

Its documentation workflow was later simplified to living architecture plus dated feature
records. Current rules live in
[`../architecture/ai-assisted-development.md`](../architecture/ai-assisted-development.md).

## Deviations

- The original implementation coupled planning to ADR creation and an earlier folder-state model;
  both were removed.
- Premortem was initially required for every public/protected symbol change. That gate was later
  narrowed: symbol changes require user confirmation, while premortem remains mandatory for module
  installers, build policy, and persisted contracts.
- Planning moved from an ADR draft and status folders to one dated feature brief plus optional
  temporary steps.

## Consequences

### Positive

- Human and AI contributors share file-cited procedures for recurring repository work.
- Narrow skills can evolve independently without bloating the always-loaded protocol.
- Risk gates are explicit and independent of the type of documentation artifact being written.

### Negative

- Skills and agents must be synchronized when architecture or coding conventions change.
- The mandatory loading and gate checks add overhead to small changes when routing is unclear.

## Follow-ups

- Keep skills synchronized with current architecture and coding rules.
- Keep each rule canonical in one file and link to it from routing documents.
- Evaluate CI enforcement only when it can distinguish meaningful gates from textual compliance.
