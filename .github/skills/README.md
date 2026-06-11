# Skills

Skill library for AI agents working in SolTechnology.Core. A **skill** is a narrow procedure
loaded on demand for a single task — distinct from an **agent** (a role with a multi-step
workflow under [`../agents/`](../agents/)).

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
| [package-management](package-management/SKILL.md) | Adding or bumping a `PackageReference`. Single source of truth for canonical versions. | independent |
| [dependency-audit](dependency-audit/SKILL.md) | Resolving `NU1901`–`NU1904` CVE warnings or `NU1605` downgrades. Drives fix-at-source over masking. | independent |
| [test-writing](test-writing/SKILL.md) | Authoring or extending tests under `tests/` (NUnit) or sample apps (NUnit for DreamTravel). | independent |
| [refactor](refactor/SKILL.md) | Behaviour-preserving cleanup local to one module (rename internals, split a class, extract a primary ctor, pay down a §15 anti-pattern). | independent |
| [roast-me](roast-me/SKILL.md) | Vague request, under-specified intent, before any non-trivial planning. | runs *before* planning |
| [implement-plan](implement-plan/SKILL.md) | Executing one step from an ADR's `to-do/` or `reviewed/` folder. | independent |

For multi-step planning, use the [implementation-planning](../agents/implementation-planning.agent.md)
agent — not a skill. See [`../agents/README.md`](../agents/README.md) for the agent index.

## Workflow

```mermaid
flowchart LR
    Start([Task]) --> Vague{Vague?}
    Vague -->|yes| Roast[skill: roast-me]
    Vague -->|no| Plan[agent: implementation-planning]
    Roast --> Plan
    Plan --> Review[agent: plan-reviewer]
    Review --> BlueRed[skill: blue-red-team]
    BlueRed --> Premortem[skill: premortem]
    Premortem -->|Go| Impl[skill: implement-plan]
    Impl --> CodeReview[skill: code-review]
    CodeReview --> DocCleanup[skill: documentation-cleanup]
    DocCleanup --> Commit[skill: commit-message]
    Commit --> End([Merge])
```

All listed skills are shipped. Agents (`implementation-planning`, `plan-reviewer`, `diagram`)
live at [`../agents/`](../agents/).

## Premortem gate

Any change that touches a public/protected symbol in `src/SolTechnology.Core.*`, a
`ModuleInstaller.cs`, `Directory.Build.props`, or a persisted contract **must** be gated by a
premortem. Attach the skill's output to the PR. Block on *Go* / *Go with mitigations* with
mitigations in place. Rationale: [ADR-004](../../docs/adr/004-ai-agents-and-skills.md).

## Self-improvement

Rules for editing skill files are in [`docs/ClaudeCodingGuide.md` §19](../../docs/ClaudeCodingGuide.md).
When you add or remove a skill, update the index above and `CLAUDE.md §3` in the same change.
