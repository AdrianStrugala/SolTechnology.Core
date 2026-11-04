---
spec: 2026-11-02-core-2-0-cache
step: 05
status: done
---

# Step 05: XML docs shipped — Cache

## Summary
XML `<summary>` blocks are written but never shipped: only Logging sets `GenerateDocumentationFile`, so NuGet consumers get no IntelliSense (`P1-4`, `cqrs-tale#16`). With `TreatWarningsAsErrors`, enabling it raises `CS1591` for every undocumented public member. This step enables it per project for `SolTechnology.Core.Cache` and documents every public member. Core step 08 centralizes the switch after every library has shipped its docs. The step runs after this feature's 2.0 cleanup, so no docs are written for members 2.0 removes.

Former core step 57 (Cache part); split per library on 2026-11-02.

## Affected components
- `src/SolTechnology.Core.Cache/SolTechnology.Core.Cache.csproj` — EDIT — `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `src/SolTechnology.Core.Cache/**/*.cs` — EDIT — `<summary>` on every public/protected member reported by `CS1591` (`ModuleInstaller.cs` gets XML doc comments only, no code change)

## Changes
- English, one or two sentences per member, per [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md). No `<NoWarn>CS1591</NoWarn>` (`CLAUDE.md §2`).

## Acceptance criteria
- [x] `dotnet build SolTechnology.Core.slnx` is green with `TreatWarningsAsErrors`.
- [x] `dotnet pack` of the project produces a nupkg containing `lib/net10.0/<Assembly>.xml`.

## Open questions
- none

## Deviations

### Verification

No material deviation. `CS1591` reported 27 members in `CacheConfiguration`, `ExpirationMode`,
`DistributedCacheConfiguration`, `StoredResponse`, `IRedisCache`, `IScopedCache`/`ScopedCache`,
`ISingletonCache`/`SingletonCache`, and `ModuleInstaller` (type, `AddSolLocalCache`, `AddSolDistributedCache`); each has
a summary, and the two `GetOrAdd` implementations use `<inheritdoc />`. The `InstanceName` summary states the key
prefixes and the `"SolTechnology:"` continuity value. `dotnet pack` into the git-excluded `.scratch/` produced
`SolTechnology.Core.Cache.2.0.0.nupkg` with `lib/net10.0/SolTechnology.Core.Cache.xml` next to the DLL and the
`docs/icon.png` icon.

