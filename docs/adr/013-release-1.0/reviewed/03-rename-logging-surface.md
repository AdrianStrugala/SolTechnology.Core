---
adr: 013-release-1.0
step: 03 of 11
status: reviewed
---

<!-- Reviewed (2026-06-30): locked the name map (answer 1: AddCoreLogging→AddSolLogging,
     UseCoreLogging→UseSolLogging) and LogDetail stays as-is (answer 6); added the repo-wide
     symbol-string sweep (M1: XML-doc <c>/<see cref>, comments, strings) — concrete Logging hits
     enumerated; switched DreamTravel call-site edits from line numbers to symbol greps (M2); kept the
     optional class rename LoggingServiceCollectionExtensions→ModuleInstaller (nit 03). -->

# Step 03: Rename wave 1 — Logging registration surface (+ internal Api/HTTP callers)

## Summary
Rename the `SolTechnology.Core.Logging` public registration surface to the `AddSol*` / `UseSol*`
convention. Logging goes first because it is the **only** module called by other Core modules
(`Api.AddApiExceptionHandling` calls `AddCoreLogging`; the HTTP correlation path calls
`AddCorrelationIdService`); renaming it first, and fixing those internal callers in the same PR,
keeps every later wave free of cross-module breakage. Pure DI/middleware plumbing — one PR.

## Affected components
- `src/SolTechnology.Core.Logging/ModuleInstaller.cs` (class `LoggingServiceCollectionExtensions`) — EDIT — rename public methods.
- `src/SolTechnology.Core.Api/ModuleInstaller.cs` — EDIT — internal call `AddCoreLogging()` → new name.
- `src/SolTechnology.Core.HTTP/**` (correlation wiring) — EDIT — internal `AddCorrelationIdService()` → new name.
- `sample-tale-code-apps/DreamTravel/src/Presentation/DreamTravel.Api/Program.cs` — EDIT — `AddCoreLogging()` + `UseCoreLogging()` call sites (locate by symbol, not line number).
- `tests/SolTechnology.Core.Logging.Tests/**`, `tests/SolTechnology.Core.HTTP.Tests/CorrelationPropagationTests.cs` — EDIT — update call sites.

## Changes
- Name map (answer 1 — replace `Core` when it immediately follows the verb, else insert `Sol`):
  - `AddCoreLogging(Action<LoggingOptions>?)` → `AddSolLogging(Action<LoggingOptions>?)`
  - `AddCoreLogging(IConfiguration)` → `AddSolLogging(IConfiguration)`
  - `UseCoreLogging(this IApplicationBuilder)` → `UseSolLogging(this IApplicationBuilder)`
  - `AddCorrelationIdService()` → `AddSolCorrelationIdService()`
  - `AddLogScopeEnricher<TEnricher>()` → `AddSolLogScopeEnricher<TEnricher>()`
  - `LogDetail(...)` → **keep `LogDetail`** (answer 6 — fluent continuation, not an `Add*` entry point).
- Optional, recommended (nit 03): rename the class `LoggingServiceCollectionExtensions` →
  `ModuleInstaller` to satisfy `ClaudeCodingGuide` §2 (internal-only; not a breaking change).
- **Symbol-string sweep (M1) — authoritative repo-wide for the symbols renamed in this step.** After
  the rename, no `<c>` / `<see cref>` XML-doc reference, comment, or `throw`/log string may still name
  the old symbol. Known Logging hits to fix in the same PR:
  `Logging/ModuleInstaller.cs:13` (`<c>AddCoreLogging</c>` + `<see cref="UseCoreLogging"/>`),
  `:123`, `:128` (`<c>AddCoreLogging</c>`), `Logging/LoggingOptions.cs:9` (`<c>AddCoreLogging</c>`),
  `Logging/Enrichment/RequestHeadersEnricher.cs:16` (`<c>AddCoreLogging</c>`). Verify with
  `grep -rn "AddCoreLogging\|UseCoreLogging\|AddCorrelationIdService\|AddLogScopeEnricher" src tests sample-tale-code-apps docs`
  returning only `Sol`-prefixed names (or migration-guide rows in `docs/`).
- DreamTravel + test call sites: locate each `AddCoreLogging`/`UseCoreLogging`/`AddCorrelationIdService`
  by **symbol grep**, not by line number — `DreamTravel.Api/Program.cs` is also edited in steps 04/05,
  so any hard-coded line shifts between PRs.
- No `[Obsolete]` shim — hard rename.

## Acceptance criteria
- [ ] No public symbol named `AddCoreLogging`, `UseCoreLogging`, `AddCorrelationIdService`, or
      `AddLogScopeEnricher` remains in `src/`.
- [ ] `grep -rn "AddCoreLogging\|UseCoreLogging\|AddCorrelationIdService\|AddLogScopeEnricher" src tests sample-tale-code-apps docs`
      returns only the new `Sol`-prefixed names (XML-doc, comments, and strings included; `docs/` hits
      are only migration-guide rows).
- [ ] `LogDetail` is unchanged.
- [ ] `dotnet build SolTechnology.Core.slnx` green; `SolTechnology.Core.Logging.Tests` +
      `SolTechnology.Core.HTTP.Tests` pass.

## Open questions
- none — naming transform and `LogDetail` are resolved at step 00.

