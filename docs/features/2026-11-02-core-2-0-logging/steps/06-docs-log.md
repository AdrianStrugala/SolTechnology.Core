---
spec: 2026-11-02-core-2-0-logging
step: 06
status: to-do
---

# Step 06: Module docs — `docs/Log.md`

## Summary
`docs/Log.md` ships as the NuGet README and must reflect the fixed configuration overload
(step 02), segment-aware path matching (step 03), correlation-ID sanitization (step 04), and the
2.0 public surface (step 05). Every C# snippet must compile against the 2.0 API.

Former core step 60 (Log part); split per library on 2026-11-02. The RM-15 note in `docs/Log.md` describes CQRS behaviour and moved to cqrs step 05.

## Affected components
- `docs/Log.md` — EDIT — `:44,81` the overload works; P-14; P-15; `LoggerExtensions` (`C-20`); CQRS emits on the same `OperationsName` source

## Changes
- Verification method: copy every C# block into a throwaway console project (not committed) that references `SolTechnology.Core.Logging`, then run `dotnet build`. Record the result in this step's Deviations if a snippet must stay illustrative (mark it with `// illustrative` in the doc).
- Follow [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md).

## Acceptance criteria
- [ ] Snippets compile in the throwaway project (the PR author states it in the PR description).

## Open questions
- none

## Deviations
