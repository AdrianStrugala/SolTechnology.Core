---
spec: 2026-11-02-core-2-0-blob
step: 04
status: done
---

# Step 04: Module docs — `docs/Blob.md`

## Summary
`docs/Blob.md` ships as the NuGet README. It uses names that no longer exist and unsupported
appsettings auto-binding (`infra#18-19`), and it predates the 2.0 namespace and cancellation
(step 02). Every C# snippet must compile against the 2.0 API.

Former core step 61 (Blob part); split per library on 2026-11-02.

## Affected components
- `docs/Blob.md` — EDIT — `:15-23,43`; `using SolTechnology.Core.Blob;`; `CancellationToken` (step 02)

## Changes
- Verification method: copy every C# block into a throwaway console project (not committed) that references `SolTechnology.Core.Blob`, then run `dotnet build`. Record the result in this step's Deviations if a snippet must stay illustrative (mark it with `// illustrative` in the doc).
- Follow [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md).

## Acceptance criteria
- [x] `grep -nE "\bAdd(MessageBus|ServiceBusHealthCheck|PersistentEvents|RecurringJob|CQRS|SQL|SqlHealthCheck)\b|WithPublisher|WithReceiver|UseSolTechnologyFilters|SmartRetry\(|UntilConditionMet" docs/Blob.md` prints nothing.
- [x] Snippets compile in the throwaway project (the PR author states it in the PR description).

## Open questions
- none

## Deviations

### Page rewritten to the canonical structure, snippets verified

**Original plan:** fix the stale names at `:15-23,43`, add `using SolTechnology.Core.Blob;` and `CancellationToken`.

**Actual implementation:** the page was rewritten to the `PublicDocumentationGuide.md` structure (lead, Features,
Registration, Configuration, Usage, Testing, Conventions), as the guide asks of a page the task already touches; the
`Overview` heading, the auto-binding claim, and `CreateConnection` are gone. The three C# blocks were copied verbatim into
an uncommitted web project in `.scratch/` that references `Blob` and `Blob.Testing`; the statement-level registration
block needed only `using SolTechnology.Core.Blob;` and `WebApplication.CreateBuilder` in front, as the other module pages
assume. The build had 0 errors and 0 warnings, so no snippet is marked illustrative. A second uncommitted console ran
the repository snippet against Azurite: the Avro round-trip passed, and a missing blob and a missing container both
threw `RequestFailedException` with status 404, as the behaviour table says.

### The Testing snippet fails at runtime on the fixture's default image

**Original plan:** snippets compile.

**Actual implementation:** they compile, but `new AzuriteFixture()` starts `azurite:3.35.0`, which rejects the service
version `2026-06-06` of `Azure.Storage.Blobs` 12.29.0 with 400 `InvalidHeaderValue` (found in step 01). The runtime
check above therefore ran on `azurite:latest`. The page keeps the default constructor; bumping the fixture's default
image stays the testing-kits follow-up from step 01.
