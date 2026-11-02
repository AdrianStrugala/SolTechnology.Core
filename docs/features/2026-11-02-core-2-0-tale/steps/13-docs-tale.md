---
spec: 2026-11-02-core-2-0-tale
step: 13
status: to-do
---

# Step 13: Module docs — `docs/Tale.md` for the 2.0 Tale contract

## Summary
`docs/Tale.md` documents direct handler persistence (now false after step 03, `:369-385`) and the
dead option `RestrictControllerToRegisteredHandlers` (`:75`). Its example injects the handler with
`CancellationToken.None`. The log category it documents is wrong (`:519-523`). Every C# snippet
must compile against the 2.0 API.

Former core step 60 (Tale part); split per library on 2026-11-02.

## Affected components
- `docs/Tale.md` — EDIT — persistence scope (P-02); `ExposeOverHttp<T>()` (step 11); chapter `CancellationToken` (step 07); `ITaleRepository` 2.0 contract, optimistic concurrency, and migration (steps 08–10); custom repositories serialize `TaleInstance` parts with the public `TaleJsonOptions.Default` (step 02, `C-26`); route the example through `IMediator`; log category
- `SolTechnology.Core.slnx` — EDIT — add `docs/Tale.md` to `/Docs/` if missing

## Changes
- Verification method: copy every C# block into a throwaway console project (not committed) that references `SolTechnology.Core.Tale` and `SolTechnology.Core.CQRS`, then run `dotnet build`. Record the result in this step's Deviations if a snippet must stay illustrative (mark it with `// illustrative` in the doc).
- Follow [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md).
- Premortem mitigations (core step 00, 2026-11-02), documentation part:
  - RM-5 (scenario 5): the `docs/Tale.md` migration section requires a single-version cutover of Tale hosts and rules out a rollback to 1.x after 2.x has written tale state.
  - RM-17 (scenario 17): `docs/Tale.md` states that a direct-handler run that pauses is not persisted and cannot be resumed; interactive tales start through `TaleManager`.

## Acceptance criteria
- [ ] `grep -nE "\bAddCQRS\b|RestrictControllerToRegisteredHandlers|CancellationToken\.None" docs/Tale.md` prints nothing.
- [ ] `grep -n "TaleJsonOptions.Default" docs/Tale.md` matches.
- [ ] Snippets compile in the throwaway project (the PR author states it in the PR description).
- [ ] RM-5, RM-17: `grep -n "roll back" docs/Tale.md` and `grep -n "TaleManager" docs/Tale.md` each match the new text.

## Open questions
- none

## Deviations
