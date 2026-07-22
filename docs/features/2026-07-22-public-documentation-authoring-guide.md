---
status: completed
created: 2026-07-22
completed: 2026-07-22
review: waived (documentation-only single-topic ownership migration with no module, API, persistence, package, build-policy, or production-code change — planner, 2026-07-22)
premortem: waived (no ModuleInstaller.cs, Directory.Build.props, or persisted contract is affected — planner, 2026-07-22)
---

# Public Documentation Authoring Guide

> Historical delivery record. It may not describe the current system.

## Goal

Move the public module documentation authoring rules from `docs/ClaudeCodingGuide.md` §18 into a
dedicated `docs/PublicDocumentationGuide.md` while preserving every rule, routing behavior, and
stable section-number citation.

## Context

[`docs/ClaudeCodingGuide.md`](../ClaudeCodingGuide.md) §18 currently owns the canonical
`docs/<Module>.md` structure, hard rules, refactoring procedure, companion-skill linking policy,
and [`docs/Api.md`](../Api.md) reference example. That user-facing documentation guidance is a
distinct authoring concern from C# coding conventions. [`docs/AIDocsGuide.md`](../AIDocsGuide.md)
already establishes the one-rule-one-place and stable-numbering constraints, so §18 must remain as
a concise link rather than be renumbered or removed.

The new guide will be `docs/PublicDocumentationGuide.md`. Keeping the destination directly under
`docs/` matches `docs/AIDocsGuide.md` and the stack guides, avoids creating a new top-level folder,
and gives root instructions and skills one canonical authoring target. Keeping the content in
`docs/ClaudeCodingGuide.md` was considered and rejected because it preserves the mixed ownership;
moving it under `docs/architecture/` was rejected because authoring conventions are not current
system architecture.

The current references to §18 are routing or rule citations in `CLAUDE.md`,
`docs/AIDocsGuide.md`, `.github/skills/README.md`, `.github/skills/premortem/SKILL.md`, and the
[active JWT documentation step](2026-07-06-jwt-bearer-authentication/steps/04-documentation.md).
[`docs/architecture/ai-assisted-development.md`](../architecture/ai-assisted-development.md) also
assigns coding and documentation conventions to `docs/ClaudeCodingGuide.md`; that ownership table
must route public documentation authoring separately after the move. Existing public module pages
vary in shape, but this migration does not audit or rewrite them.

## Scope

- `docs/PublicDocumentationGuide.md` — NEW — receive the complete public module documentation
  structure, hard rules, refactoring procedure, companion-skill policy, and reference example from
  `docs/ClaudeCodingGuide.md` §18.
- `docs/ClaudeCodingGuide.md` — EDIT — replace §18 with a concise stable-numbered link and retarget
  only internal public-documentation routing that would otherwise point back to the moved content.
- `CLAUDE.md` — EDIT — route public module documentation authoring and user-facing documentation
  lessons to `docs/PublicDocumentationGuide.md` without changing pre-flight, confirmation, or
  self-improvement behavior.
- `docs/AIDocsGuide.md` — EDIT — retarget direct §18 citations and ownership descriptions without
  moving AI-only documentation rules.
- `docs/architecture/ai-assisted-development.md` — EDIT — record the new current owner for public
  documentation authoring and narrow the `docs/ClaudeCodingGuide.md` responsibility accordingly.
- `.github/skills/README.md` — EDIT — retarget the package-companion skill documentation citation.
- `.github/skills/premortem/SKILL.md` — EDIT — replace the stale §18 example citation without
  changing the premortem workflow.
- `docs/features/2026-07-06-jwt-bearer-authentication/steps/04-documentation.md` — EDIT — retarget
  the active implementation contract's two §18 citations to the new guide without changing scope,
  status, or acceptance behavior.
- Existing `docs/<Module>.md` pages, including `docs/Api.md`, remain content-identical.
- No other `docs/ClaudeCodingGuide.md` section, architecture topic, feature history, source code,
  test, project, package, pipeline, or build configuration is migrated or changed.
- No temporary working folder is required because the destination, stub, and cross-reference
  updates form one coherent documentation-only implementation unit.

## Implementation plan

| Step | Outcome | Status |
|---|---|---|
| 01 | Create the public documentation authoring guide, leave the §18 stub, retarget only verified citations and ownership routing, and validate content parity and links. | done |

## Acceptance criteria

- [ ] `docs/PublicDocumentationGuide.md` is the sole canonical owner of every structure rule, hard
  rule, refactoring instruction, companion-skill policy, and reference example currently in
  `docs/ClaudeCodingGuide.md` §18.
- [ ] `docs/ClaudeCodingGuide.md` retains heading `## 18. Public module documentation
  (`docs/<Module>.md`)` as a concise link to `docs/PublicDocumentationGuide.md`; no existing guide
  section is renumbered and no unrelated section changes.
- [ ] Every repository reference that cites or duplicates §18 routes to the new guide or the stable
  §18 stub according to its purpose; no unrelated cross-reference changes.
- [ ] Root pre-flight, self-improvement routing, skill behavior, confirmation gates, templates,
  validation requirements, and handoffs are behaviorally unchanged.
- [ ] Existing public module pages and `docs/Api.md` have no content changes.
- [ ] No file outside the paths listed in `Scope` changes.
- [ ] A repository-wide search finds no stale §18 citation that expects the moved details to remain
  in `docs/ClaudeCodingGuide.md`.
- [ ] Every relative Markdown link in each changed file resolves, and `git diff --check` passes.
- [ ] The implementation diff contains documentation files only and does not create a temporary
  feature working folder.

## Completion summary

- Created `docs/PublicDocumentationGuide.md` as the sole owner of public module documentation
  structure, hard rules, refactoring instructions, companion-skill linking, and the `docs/Api.md`
  reference example.
- Reduced `docs/ClaudeCodingGuide.md` §18 to a stable-numbered routing stub and separated public
  documentation lessons from C# self-improvement routing.
- Retargeted verified live root, AI-doc, architecture, skill, premortem, and active JWT feature
  references without changing their workflows or requirements.
- Verified content parity, relative links, stale §18 citations, unchanged public module pages, and
  whitespace hygiene.

## Deviations

None.

## Follow-ups

None.
