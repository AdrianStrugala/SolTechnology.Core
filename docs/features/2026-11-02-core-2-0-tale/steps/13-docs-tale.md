---
spec: 2026-11-02-core-2-0-tale
step: 13
status: done
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
- [x] `grep -nE "\bAddCQRS\b|RestrictControllerToRegisteredHandlers|CancellationToken\.None" docs/Tale.md` prints nothing.
- [x] `grep -n "TaleJsonOptions.Default" docs/Tale.md` matches.
- [x] Snippets compile in the throwaway project (the PR author states it in the PR description).
- [x] RM-5, RM-17: `grep -n "roll back" docs/Tale.md` and `grep -n "TaleManager" docs/Tale.md` each match the new text.

## Open questions
- none

## Deviations

### Page rewritten into the canonical structure; snippet check recorded here

**Original plan:** Edit the listed sections of `docs/Tale.md`; the PR description states that the
snippets compile.

**Actual implementation:** The 557-line page was rewritten into the
[`PublicDocumentationGuide.md`](../../../PublicDocumentationGuide.md) structure (300 lines), like
the AUID and CQRS pages. It drops the marketing sections, the misleading "Versioning" claim (2.0
does check history), the dead option, the wrong `TaleEngine` log category and the hand-built
`DataField` example with a non-existent `Required` property. New content: the `ExposeOverHttp`
opt-in, chapter tokens, the resume outcome table, the custom-repository contract with
`TaleJsonOptions.Default`, the direct-run rule (RM-17: a paused direct run is not persisted and
cannot be resumed; start interactive tales through `TaleManager`), and "Upgrading from 1.x" with
the single cutover and no roll back (RM-5). The direct example goes through `IMediator`. Commits go
straight to `master`, so the snippet check is recorded here: all ten C# blocks were copied into a
throwaway console project (not committed) referencing `SolTechnology.Core.Tale`,
`SolTechnology.Core.CQRS`, NUnit and FluentAssertions, with statement blocks wrapped in methods
that take `builder`, `taleManager` and `cancellationToken`, and stubs for the consumer's
`IOrderRepository`, `FakeOrderRepository` and `MyTaleRepository`; `dotnet build` succeeded with
warnings as errors. No snippet is marked `// illustrative`. `docs/Tale.md` is now in the solution's
`/Docs/` folder.
