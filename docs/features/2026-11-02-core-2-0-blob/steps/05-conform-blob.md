---
spec: 2026-11-02-core-2-0-blob
step: 05
status: to-do
---

# Step 05: Guide conformance — Blob

## Summary
`BlobConnectionFactory` hand-writes its constructor, and Blob has the same `static class` +
`this`, brace, and comment violations as the other infrastructure modules (`infra#24`). This is a
behaviour-preserving refactor. Blob has no test project (Q6, accepted risk AR-1).

Former core step 69 (Blob part) plus the Blob-scoped part of former step 70; split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.Blob/Connection/BlobConnectionFactory.cs` — EDIT — `:12-15` primary constructor
- `src/SolTechnology.Core.Blob/**` — EDIT — `extension` blocks, braces, comments, if any remain

## Changes
- No public or protected signature change; no log-template change.
- Behaviour-preserving refactor; use the [`refactor`](../../../../.github/skills/refactor/SKILL.md) skill.
- Log templates: the scoped `[{Placeholder}]` check below is this library's part of former step 70 (L-03). Fix any unbracketed placeholder it finds and add the property name to the core brief's L-03 entry.

## Acceptance criteria
- [ ] `find src/SolTechnology.Core.Blob -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [ ] `grep -rnE "catch\s*\{\s*\}" src/SolTechnology.Core.Blob` prints nothing.
- [ ] `dotnet build SolTechnology.Core.slnx` is green.
- [ ] `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.Blob --include=*.cs` prints nothing (the former step 70 repository-wide check, scoped to this library; tune the regex in the PR; `[LoggerMessage]` templates included).
- [ ] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).

## Open questions
- none

## Deviations
