---
status: completed
created: 2026-07-21
completed: 2026-07-21
---

# Project Documentation Model

> Historical delivery record. It may not describe the current system. Current documentation rules
> live in [`../architecture/delivery-workflow.md`](../architecture/delivery-workflow.md).

## Goal

Replace permanent ADRs, copied indexes, and durable implementation trackers with two clear forms of
project documentation that remain useful during rapid development.

## Context

The repository used numbered ADRs for decisions, dated feature files for backlog and delivery,
manual ADR and feature indexes, and temporary implementation folders. Over time those artifacts
copied status, implementation state, and current architecture into several places. The copies
drifted, and historical documents were sometimes treated as current system documentation.

At the same time, collapsing old ADRs into short feature summaries removed too much historical
value: original context, alternatives, intended guarantees, consequences, and deviations were
needed to understand how the system evolved.

Public module pages under `docs/<Module>.md` are a separate user-facing contract. This migration
must not rewrite them except where a link to the retired documentation model must change.

## Decision

Use two durable documentation categories:

1. `docs/architecture/` describes current behavior and current rationale. It is mutable, verified
   against source and tests, and updated with the implementation that changes the system.
2. `docs/features/YYYY-MM-DD-<name>.md` records planning and delivery history. It is created when
   planning begins, may become stale after completion, and preserves the original context,
   alternatives, implementation plan, outcome, deviations, consequences, and follow-ups.

Optional temporary execution state lives in a sibling folder while a multi-step feature is active.
It contains `summary.md` and numbered steps, including premortem and retrospective brackets when
required. The retrospective consolidates durable history into the feature record and removes the
working folder only after current architecture has been synchronized.

Feature statuses are:

- `planning`
- `planned`
- `in-progress`
- `blocked`
- `completed`
- `abandoned`

Temporary step statuses remain `to-do`, `blocked`, `in-progress`, and `done`.

Risk gates depend on blast radius, not document type. Premortem remains mandatory for module
installers, build policy, and persisted contracts. Public/protected symbol changes require explicit
user confirmation.

## Alternatives considered

### Keep ADRs and improve their indexes

Rejected because the core problem was not formatting. Permanent decisions, mutable architecture,
and implementation status were mixed in one artifact, while copied indexes created another source
of state.

### Use only current architecture documentation

Rejected because delivery history, rejected alternatives, and deviations would be lost.

### Use only feature records

Rejected because historical records naturally become stale and should not be the source of truth
for current behavior.

### Keep very short feature summaries

Rejected during migration because the summaries lost the reasoning needed to evaluate previous
approaches. Feature records may be detailed; only architecture pages must remain concise and
current.

### Maintain a manual feature index

Rejected because filename chronology and frontmatter already contain the relevant data. Any listing
should be generated rather than synchronized by hand.

## Scope

- Create the current architecture documentation set.
- Convert every former ADR into a detailed dated feature record.
- Preserve active JWT work as `planning`, not current architecture.
- Update agents, skills, guides, and repository protocol to the new lifecycle.
- Retarget durable links from ADRs to current architecture or historical feature records according
  to meaning.
- Remove the ADR tree and hand-maintained feature index after link validation.
- Preserve public module pages except for link-only migration changes.

## Implementation plan

1. Define the canonical architecture and feature lifecycle.
2. Build current architecture pages from source and tests.
3. Convert historical decisions and delivery summaries into dated feature records.
4. Enrich converted records with original context, alternatives, consequences, and deviations.
5. Update workflow agents, skills, and authoring guides.
6. Retarget links and remove obsolete workflow artifacts.
7. Validate frontmatter, links, diagnostics, and repository-wide terminology.

## Acceptance criteria

- `docs/architecture/` is the only source for current architecture and rationale.
- Every durable feature filename matches `YYYY-MM-DD-<kebab-name>.md`.
- Every feature contains valid status, creation date, and completion date rules.
- Completed feature records preserve meaningful historical reasoning and delivered deviations.
- Active JWT work remains `planning` and is absent from current authentication behavior.
- No durable link targets `docs/adr/` or `docs/features/README.md`.
- Agents and skills never create ADRs or manual feature indexes.
- Public module pages contain no unrelated rewrite from this migration.
- Obsolete workflow artifacts are removed only after the replacement content and links are
  validated.

## Completion summary

The repository now uses two durable project-documentation categories:

- `docs/architecture/` contains mutable, source-verified current behavior and rationale;
- dated files directly under `docs/features/` preserve planning and delivery history.

Former decisions were converted into detailed feature records that retain original context,
alternatives, intended guarantees, consequences, delivered outcomes, deviations, and follow-ups.
The active JWT feature remains `planning` with its temporary execution folder intact.

Repository protocol, authoring guides, agents, and skills now route current architecture and
historical delivery separately. Risk gates remain based on blast radius. Durable links were
retargeted by meaning, the ADR tree and manual feature index were removed, and production-pattern
records were renamed to the dated convention.

Public module documentation was preserved. `Auth.md` and `CQRS.md` match their pre-migration
versions; other module pages contain only link changes required by removal of the old model.

Validation completed on 2026-07-21:

- all dated feature filenames and frontmatter statuses passed the lifecycle validator;
- completed records contain completion dates and `Completion summary` sections;
- every durable relative Markdown link resolves;
- architecture and feature tables have consistent columns;
- no active agent or skill creates ADRs or a hand-maintained feature index;
- `git diff --check` reported no whitespace errors;
- the migration diff contains no production-code changes.

## Deviations

- The first conversion pass condensed former ADRs too aggressively. The records were enriched before
  the ADR tree was removed.
- `docs/Auth.md`, `docs/CQRS.md`, and `docs/Hangfire.md` were initially rewritten as part of a public
  API accuracy audit. Those out-of-scope changes were reverted; only link migrations remain where
  required.

## Follow-ups

- Generate repository views of feature status from frontmatter if a listing becomes useful.
- Audit public module documentation against source as a separate explicit feature.
