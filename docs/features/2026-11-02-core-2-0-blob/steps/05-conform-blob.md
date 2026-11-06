---
spec: 2026-11-02-core-2-0-blob
step: 05
status: done
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
- [x] `find src/SolTechnology.Core.Blob -name "*.cs" -not -path "*/obj/*" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [x] `grep -rnE "catch\s*\{\s*\}" src/SolTechnology.Core.Blob` prints nothing.
- [x] `dotnet build SolTechnology.Core.slnx` is green.
- [x] `grep -rnE "Log[A-Za-z]*\(.*\"[^\"]*[^\[]\{[A-Z][A-Za-z]*\}[^\]]" src/SolTechnology.Core.Blob --include=*.cs` prints nothing (the former step 70 repository-wide check, scoped to this library; tune the regex in the PR; `[LoggerMessage]` templates included).
- [x] Public API diff is empty (compare `dotnet build` ref assemblies, or use `Microsoft.DotNet.ApiCompat` locally if available).

## Open questions
- none

## Deviations

### `ReadFromBlob<T>` stays a classic extension method

**Original plan:** convert every extension method to an `extension` block.

**Actual implementation:** `ModuleInstaller` and the non-generic Blob extensions use `extension` blocks; `ReadFromBlob<T>` stays a classic `this` method, because for an unconstrained generic member of an `extension` block the SDK 10.0.100 compiler drops `T`'s nullable annotation on the emitted static method (CS8714 at static-form call sites). The exception is recorded in Coding Guide §9.7. ApiCompat (both directions) shows only the compiler-generated `extension` marker types; full suite green.
