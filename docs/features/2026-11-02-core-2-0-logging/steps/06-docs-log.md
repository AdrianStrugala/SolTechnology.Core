---
spec: 2026-11-02-core-2-0-logging
step: 06
status: done
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
- [x] Snippets compile in the throwaway project (the PR author states it in the PR description).

## Open questions
- none

## Deviations

### Snippet verification and accuracy fixes

**Original plan:** Update `docs/Log.md` for steps 02–05 and compile every C# block in a throwaway console project; the PR author states the result.

**Actual implementation:** Every C# block (registration, configuration, `AuditWriter`, `LogDetail`, custom enricher, request headers, lifecycle events, OpenTelemetry, testing, scope helpers, PII masking, `[Masked]` property, `ITimingService` handler, background-worker recipe) was copied into a throwaway `net10.0` web project outside the repository that referenced `SolTechnology.Core.Logging`, `SolTechnology.Core.CQRS` and `SolTechnology.Core.API.Testing` plus the OpenTelemetry 1.20.1, NUnit and FluentAssertions packages, with stub types for the consumer-side names (`ImportInvoices`, `ICityDomainService`, `ISettlementHandler`). `dotnet build` succeeded with 0 errors, and running it printed `sec***com ***MASKED*** john***.com`, matching the documented masking results. No snippet is marked `// illustrative`. Three snippets did not compile before and were fixed: the testing example used `APIFixture.CreateClient()` (the fixture exposes `ServerClient`), and the scope-helper example declared `scope` twice. Two inaccurate statements were corrected on touch: `APIFixture` does not wire Logging itself (the app's `Program.cs` does), and `CoreLoggingActivitySources` is a static class, not a DI registration.
