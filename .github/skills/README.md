# Skills

Skill library for AI agents working in SolTechnology.Core. Adapted from
[aiex](https://github.com/) (Open Banking Copilot Change Manager) and refactored for a NuGet-package
codebase that follows the [Tale Code](../../docs/ClaudeCodingGuide.md) philosophy.

Before invoking a skill you **must** `read_file` its `SKILL.md`. Skills are not pre-loaded —
their instructions live only inside the file. Never infer what a skill does from its name.
Conventions, references, and output formats live only inside `SKILL.md`.

## Index

| Skill | When to use | Premortem coupling |
|---|---|---|
| [premortem](premortem/SKILL.md) | Any change touching public NuGet API, `ModuleInstaller`, persisted contracts, or build files. **The primary risk gate.** | self |
| [blue-red-team](blue-red-team/SKILL.md) | Design-level decision, ADR seeding. | runs *before* premortem |
| [code-review](code-review/SKILL.md) | Reviewing a diff against Coding Guide + module review templates. | requires premortem when API surface changes |
| [commit-message](commit-message/SKILL.md) | Producing a Conventional Commits message with semver footer. | reads premortem output |
| [documentation-cleanup](documentation-cleanup/SKILL.md) | Validating module/doc parity, indexes, tables, Mermaid, ADRs, links. | independent |
| [implementation-planning](implementation-planning/SKILL.md) | Planning a multi-module or breaking change; produces ADR draft. | ends with premortem gate |

## Workflow

```
                ┌─────────────────────┐
                │ implementation-     │
                │ planning            │
                └──────────┬──────────┘
                           │ ADR draft
                           ▼
                ┌─────────────────────┐
                │ blue-red-team       │  (design decisions)
                └──────────┬──────────┘
                           │
                           ▼
                ┌─────────────────────┐
                │ premortem           │  ← gate
                └──────────┬──────────┘
                           │ Go / Go with mitigations
                           ▼
                ┌─────────────────────┐
                │ implement (code)    │
                └──────────┬──────────┘
                           │
                           ▼
                ┌─────────────────────┐
                │ code-review         │
                └──────────┬──────────┘
                           │
                           ▼
                ┌─────────────────────┐
                │ documentation-      │
                │ cleanup             │
                └──────────┬──────────┘
                           │
                           ▼
                ┌─────────────────────┐
                │ commit-message      │
                └─────────────────────┘
```

## Source

Skills derived from [aiex](https://github.com/) `.github/skills/`:

- `premortem-failure-analysis` → [premortem](premortem/SKILL.md) (refactored with module checklists)
- `blue-red-team-analysis` → [blue-red-team](blue-red-team/SKILL.md) (Tale Code lens)
- `documentation-cleanup` → [documentation-cleanup](documentation-cleanup/SKILL.md) (docs/ layout)

Skills written from scratch for this repo (no aiex counterpart):

- [code-review](code-review/SKILL.md)
- [commit-message](commit-message/SKILL.md)
- [implementation-planning](implementation-planning/SKILL.md)

Deliberately **not ported** from aiex (banking-domain or workflow-specific): `agent-output-file-writer`,
`change-scope-selection`, `git-change-analysis`, `human-factors-framework`, `inverse-dependency-analysis`.
Rationale: see [docs/adr/004-ai-agents-and-skills.md](../../docs/adr/004-ai-agents-and-skills.md).

