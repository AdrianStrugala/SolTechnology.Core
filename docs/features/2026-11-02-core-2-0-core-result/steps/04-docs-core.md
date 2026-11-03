---
spec: 2026-11-02-core-2-0-core-result
step: 04
status: done
---

# Step 04: Module docs — `docs/Core.md` as the `SolTechnology.Core` package README

## Summary
`SolTechnology.Core` packs the root `README.md`, whose relative images do not render on nuget.org
and which describes the repo, not the Result pattern (`repo#A9`). This step adds `docs/Core.md` for
`Result`, `Result<T>`, the `Error` hierarchy, and JSON polymorphism (P-01), and packs it as the
package README. Every C# snippet must compile against the 2.0 API.

Former core step 60 (Core part); split per library on 2026-11-02.

## Affected components
- `docs/Core.md` — NEW — `Result`, `Result<T>`, `Error` hierarchy, JSON polymorphism (P-01)
- `src/SolTechnology.Core/SolTechnology.Core.csproj` — EDIT — `:14` delete the `..\..\README.md` `None` item and the `mini-logo.png` item; add `<PackageReadmeSource>Core.md</PackageReadmeSource>` (the shared `src/Directory.Build.props` `ItemGroup` then packs both)
- `SolTechnology.Core.slnx` — EDIT — add `docs/Core.md` to `/Docs/` if missing

## Changes
- Verification method: copy every C# block into a throwaway console project (not committed) that references `SolTechnology.Core`, then run `dotnet build`. Record the result in this step's Deviations if a snippet must stay illustrative (mark it with `// illustrative` in the doc).
- Follow [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md).
- Premortem mitigation (core step 00, 2026-11-02), documentation part:
  - RM-20 (scenario 20): `docs/Core.md` states that the `Result`/`Error` JSON contract is System.Text.Json only, and that `Result` must not travel inside Newtonsoft-serialized payloads (MessageBus messages, Hangfire job arguments).

## Acceptance criteria
- [x] `dotnet pack src/SolTechnology.Core/SolTechnology.Core.csproj` contains `docs/readme.md` equal to `docs/Core.md`.
- [x] Snippets compile in the throwaway project (the PR author states it in the PR description).
- [x] RM-20: `grep -n "Newtonsoft" docs/Core.md` matches the new text.

## Open questions
- none

## Deviations

### Snippet verification

**Original plan:** the PR author states in the PR description that the snippets compile.

**Actual implementation:** delivered as a direct commit, so the check is recorded here: every C# block of `docs/Core.md` was copied into a throwaway console app and a throwaway NUnit project (referencing `SolTechnology.Core` and `SolTechnology.Core.Testing`, `TreatWarningsAsErrors` on). Both built; the app's output matched the comments in the page (aggregate message, `InnerErrors.Count`, the JSON literal, the restored `NotFoundError`), and the `ShouldBeFailure<NotFoundError>` test passed. No snippet is illustrative.
