---
spec: 2026-11-02-core-2-0-http
step: 08
status: to-do
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
- [ ] `grep -nE "AddApiCore|AddApiCoreFilters|UseSwaggerWithVersioning|\bAddVersioning\b|AddHTTPClient\b|IQueryHandler<" docs/Clients.md docs/HTTP-Production-Checklist.md` prints nothing.
- [ ] Snippets compile in the throwaway project (the PR author states it in the PR description).
- [ ] The documentation-cleanup skill reports no broken links for the 2 files.

## Open questions
- none

## Deviations
