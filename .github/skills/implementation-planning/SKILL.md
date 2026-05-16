---
name: implementation-planning
description: Produce an ADR-shaped implementation plan for a SolTechnology.Core change, ending in a premortem gate before any code is written.
user-invocable: true
---

# Implementation Planning

Plan a non-trivial change to SolTechnology.Core. Output is a draft ADR ready to drop into
[docs/adr/](../../../docs/adr/) plus a checklist of code-level steps.

## When to Run

- New module under `src/SolTechnology.Core.*`.
- Change crossing two or more modules.
- Breaking change to a public API.
- Replacing or adopting a third-party dependency.

For single-file local refactors, skip this skill and go straight to
[code-review](../code-review/SKILL.md).

## Critical Rules

- **Documentation-first.** Do not write code in this skill. The output is plan + ADR draft only.
- **Numbered ADR.** Use the next free `NNN` in [docs/adr/](../../../docs/adr/) (current highest:
  [003](../../../docs/adr/003-api-versioning-strategy.md)).
- **End with premortem.** The last step in every plan is "run [premortem](../premortem/SKILL.md)".
  Implementation does not begin until premortem returns *Go* or *Go with mitigations*.

## Process

### 1. Frame the Problem

- One-paragraph problem statement.
- Affected modules under `src/SolTechnology.Core.*`.
- Affected sample apps under `sample-tale-code-apps/`.

### 2. Survey Existing Patterns

Read, in this order:

1. [docs/ClaudeCodingGuide.md](../../../docs/ClaudeCodingGuide.md) — sections relevant to the change.
2. Existing ADRs in [docs/adr/](../../../docs/adr/) — find precedents and constraints.
3. The relevant module doc under [docs/](../../../docs/).
4. The matching review template in [docs/reviews/](../../../docs/reviews/) when present.

### 3. Generate Alternatives

List at least two viable approaches. For each: API shape, modules touched, semver impact,
test impact. Use [blue-red-team](../blue-red-team/SKILL.md) to argue them honestly.

### 4. Recommend

Pick one alternative. State the rationale in terms of Tale Code readability + module fit +
consumer cost.

### 5. Plan the Work

Break the implementation into ordered steps. Each step:

- Names the files to touch.
- Names the test(s) to add under `tests/SolTechnology.Core.<Module>.Tests/`.
- Names the doc to update under [docs/](../../../docs/).
- Calls out DI registration changes in any `ModuleInstaller.cs`.

### 6. Gate with Premortem

Final step: invoke [premortem](../premortem/SKILL.md) with the planned change. Implementation is
blocked until the premortem decision is *Go* or *Go with mitigations*.

## Standard Output Format

### Implementation Plan — `<change title>`

#### Proposed ADR

**File**: `docs/adr/NNN-<kebab-title>.md`

```markdown
# ADR-NNN: <Title>

> **Status:** Proposed
> **Decision Date:** <YYYY-MM-DD>
> **Decision Maker:** <name or team>

## Context
<placeholder>Problem statement and constraints.</placeholder>

## Decision
<placeholder>Chosen approach in one paragraph.</placeholder>

## Alternatives Considered
1. <placeholder>Approach A — pros / cons.</placeholder>
2. <placeholder>Approach B — pros / cons.</placeholder>

## Consequences
**Positive**: <placeholder></placeholder>
**Negative**: <placeholder></placeholder>
**Semver impact**: PATCH / MINOR / MAJOR
```

#### Implementation Steps

1. <placeholder>step — files, tests, docs</placeholder>
2. <placeholder></placeholder>
3. Run [premortem](../premortem/SKILL.md) — required gate.

#### Open Questions
- <placeholder></placeholder>

