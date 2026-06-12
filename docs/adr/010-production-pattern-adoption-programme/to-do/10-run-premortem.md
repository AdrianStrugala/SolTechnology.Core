---
adr: 010-production-pattern-adoption-programme
step: 10 of 10
status: to-do
---

# Step 10: Run the premortem on the programme

## Summary
Run the [`premortem`](../../../../.github/skills/premortem/SKILL.md) skill on the ADR-010 programme
**structure** before any workstream code is implemented. This is the mandatory final gate. The
premortem here is about the programme decision (split, sequence, dependency exposure, nested gates) —
each child ADR runs its **own** premortem before its own code ships.

## Affected components
- `docs/adr/010-production-pattern-adoption-programme.md` — attach the premortem verdict.
- `docs/adr/010-production-pattern-adoption-programme/summary.md` — record *Go* / *Go with
  mitigations* / *No-go*.

## Details
- Read the premortem `SKILL.md` first (`CLAUDE.md` §0.3), then frame the change as the *programme
  structure*, not any single module's code.
- Seed scenarios from the cross-cutting risks: sequencing breaks (MessageBus authored before Logging
  correlation lands); dependency-ledger surprise (a candidate dep carries a CVE — `CLAUDE.md` §5);
  the `ValidateOnStart` fail-fast behaviour change (G3) taking a consumer host down on deploy;
  child-ADR number drift against the index; an open question answered late stalling the sequence.
- Use the per-module checklists for Cache / SQL / Logging / MessageBus referenced by the skill only
  as a reminder that each child ADR will need its own module-specific premortem.

## Acceptance criteria
- Premortem verdict (*Go* / *Go with mitigations* / *No-go*) recorded with mitigations in the summary.
- Implementation of any workstream is **blocked** until this returns *Go* / *Go with mitigations* AND
  the relevant child ADR's own premortem also passes.

## Open questions
- none.

