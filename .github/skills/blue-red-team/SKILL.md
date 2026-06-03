---
name: blue-red-team
description: Evaluate a SolTechnology.Core design decision from both supportive (Blue) and skeptical (Red) perspectives. Companion to premortem for ADR-grade choices.
---

# Blue / Red Team

Adversarial thinking for **design-level** decisions in SolTechnology.Core
(new module, public API shape, framework choice, breaking change).

Use this skill alongside [premortem](../premortem/SKILL.md):

- **blue-red-team** answers *should we do this at all?*
- **premortem** answers *if we do this, how does it fail?*

## When to use

- Before opening an [ADR](../../../docs/adr/) — the Blue/Red output seeds the
  *Decision* and *Alternatives Considered* sections.
- When choosing between two patterns within Tale Code (e.g. Result vs exception,
  Chain handler vs single handler, Story step vs CQRS handler).
- When introducing a new module to `src/SolTechnology.Core.*`.

## Critical rules

- **No strawmen.** Make the strongest honest case for each side.
- **Be specific to this repo.** Cite modules, files, ADRs, sample apps — not generic
  industry talking points.
- **Tale Code lens.** Both sides must address readability and prose-like flow
  (see [docs/ClaudeCodingGuide.md](../../../docs/ClaudeCodingGuide.md)).

## Process

### 1. State the proposal

One sentence. Include the concrete change (file / module / API shape).

### 2. Blue team — arguments for

Consider:

- Tale Code readability win — does it read more like prose?
- Consistency with existing modules (CQRS, Story, Logging patterns).
- Reduction in NuGet API surface or removal of foot-guns.
- Test simplification, fewer mocks, smaller files (class-size budget).
- Strategic alignment with [docs/theDesign.md](../../../docs/theDesign.md).

### 3. Red team — arguments against

Consider:

- Breaking change for public NuGet consumers (semver MAJOR cost).
- Hidden complexity moved, not removed.
- Performance / allocation cost.
- New cross-module coupling (`SolTechnology.Core.X` now needs `Y`).
- Build-side fallout (`TreatWarningsAsErrors`, analyzer noise).
- Migration cost for sample apps and external consumers.

### 4. Reconcile

Identify the cruxes — points where Blue and Red disagree on fact, not preference.
For each crux, state what evidence would settle it (a benchmark, a prototype, a
consumer survey, an ADR).

## Output format

### Proposal

<placeholder>One-sentence statement.</placeholder>

### Blue Team
1. <placeholder>argument with file / module / ADR reference</placeholder>

### Red Team
1. <placeholder>argument with file / module / ADR reference</placeholder>

### Cruxes
1. <placeholder>disputed fact + evidence that would settle it</placeholder>

### Recommendation
<placeholder>Proceed / Proceed with conditions / Reject — with one-line reason.</placeholder>

