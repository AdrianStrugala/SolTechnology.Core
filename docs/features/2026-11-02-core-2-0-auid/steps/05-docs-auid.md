---
spec: 2026-11-02-core-2-0-auid
step: 05
status: done
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
- [x] Snippets compile in the throwaway project (the PR author states it in the PR description).

## Open questions
- none

## Deviations

### Page rewritten into the canonical structure

**Original plan:** Add `using SolTechnology.Core.AUID;` and the `TimeProvider` overloads to `docs/AUID.md`.

**Actual implementation:** Following `PublicDocumentationGuide.md` meant replacing the `Overview` heading and the numbered usage headings, so the page was rewritten into the canonical sections (lead, Features, Registration, Configuration, Usage, Testing, Conventions; 196 lines). The step 02 uniqueness and ordering text moved under Usage unchanged. New content describes 2.0 behaviour: the parse-rejection table, the converter behaviour table, dictionary keys, the raw `long` conversions, and a `FakeTimeProvider` test. Two wrong claims were fixed: `OrderService.cs` gives `ORD`, not `ORS` (also in the `Auid.New` XML example), and formatting allocates the result string. No link pointed at an anchor of the old page.

### Snippet verification recorded here, not in a PR description

**Original plan:** The PR author states the snippet result in the PR description.

**Actual implementation:** Commits go straight to `master`, so the result is recorded here. All 7 C# blocks were built in a throwaway library project (gitignored `.scratch/`, not committed). It referenced `SolTechnology.Core.AUID`, `NUnit`, `FluentAssertions` and `Microsoft.Extensions.TimeProvider.Testing`, with `TreatWarningsAsErrors` on. Statement blocks were wrapped in methods and an `Order` stub was added. Result: 0 warnings, 0 errors; no `// illustrative` marker was needed.
