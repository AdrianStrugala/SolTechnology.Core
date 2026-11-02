---
spec: 2026-11-02-core-2-0-auid
step: 05
status: to-do
---

# Step 05: Module docs — `docs/AUID.md` for the 2.0 namespace and `TimeProvider` overloads

## Summary
`docs/AUID.md` ships as the NuGet README. After step 03 its samples miss the
`SolTechnology.Core.AUID` namespace and the static `Auid.TimeProvider` is gone. Every C# snippet
must compile against the 2.0 API.

Former core step 60 (AUID part); split per library on 2026-11-02.

## Affected components
- `docs/AUID.md` — EDIT — `using SolTechnology.Core.AUID;` (step 03); `TimeProvider` overloads

## Changes
- Verification method: copy every C# block into a throwaway console project (not committed) that references `SolTechnology.Core.AUID`, then run `dotnet build`. Record the result in this step's Deviations if a snippet must stay illustrative (mark it with `// illustrative` in the doc).
- Follow [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md).

## Acceptance criteria
- [ ] Snippets compile in the throwaway project (the PR author states it in the PR description).

## Open questions
- none

## Deviations
