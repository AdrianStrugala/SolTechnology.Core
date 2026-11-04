---
spec: 2026-11-02-core-2-0-http
step: 08
status: done
---

# Step 08: Module docs — Clients and the HTTP production checklist

## Summary
These docs ship as NuGet READMEs, and their samples call APIs that no longer exist (`P1-1`,
`http-api#11-14`). `AddHTTPClient` is a pre-`AddSol*` name. The `docs/Clients.md:116-122` defaults
table disagrees with `HttpPolicyConfiguration.cs` (10 000 / 2 / 10 000 / 5). Every C# snippet must
compile against the 2.0 API.

Former core step 59 (Clients and HTTP checklist part); split per library on 2026-11-02.

## Affected components
- `docs/Clients.md` — EDIT — `:16,49,59,101` names; `:116-122` defaults; `:431` health check name and behaviour after step 05; `IncludeResponseBodyInException` is per-request (design A10); `RequestBuilder` names after step 06
- `docs/HTTP-Production-Checklist.md` — EDIT — `:94,122` names

## Changes
- Verification method: copy every C# block into a throwaway console project (not committed) that references `SolTechnology.Core.HTTP`, then run `dotnet build`. Record the result in this step's Deviations if a snippet must stay illustrative (mark it with `// illustrative` in the doc).
- Follow [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md).

## Acceptance criteria
- [x] `grep -nE "AddApiCore|AddApiCoreFilters|UseSwaggerWithVersioning|\bAddVersioning\b|AddHTTPClient\b|IQueryHandler<" docs/Clients.md docs/HTTP-Production-Checklist.md` prints nothing.
- [x] Snippets compile in the throwaway project (the PR author states it in the PR description).
- [x] The documentation-cleanup skill reports no broken links for the 2 files.

## Open questions
- none

## Deviations

### Snippet verification and page shape

**Original plan:** Fix names, defaults, the health check and `RequestBuilder` names; verify every C# block in a throwaway console project; the PR author states the result in the PR description.

**Actual implementation:** There is no PR (commits go to `master`), so the result is recorded here: all 16 C# blocks of `docs/Clients.md` and both of `docs/HTTP-Production-Checklist.md` were copied verbatim into a throwaway `net10.0` project (outside the repository, deleted afterwards) that referenced `SolTechnology.Core.HTTP` and `SolTechnology.Core.HTTP.Testing` plus FluentAssertions, NUnit and OpenTelemetry; statement blocks were wrapped in methods whose parameters supply the context variables (`httpClient`, `cancellationToken`, `logger`, …). `dotnet build` reported 0 errors and 0 warnings with `TreatWarningsAsErrors` and nullable enabled; no snippet needed an `// illustrative` marker. To follow `PublicDocumentationGuide.md`, the three extra top-level sections (Try verbs, retry predicate, upstream health check) moved under `### Usage` as `####` subsections and the "Breaking migration" blockquote became body text. Consumer-side method names in the samples dropped `Async` (`GetMatch`). The checklist also carries the worst-case retry-time correction deferred from step 03, the 2.0 version, P-10, per-request `IncludeResponseBodyInException`, L-02, and the start-time validation of custom options (P-19). The documentation-cleanup link and table checks pass for both files.
