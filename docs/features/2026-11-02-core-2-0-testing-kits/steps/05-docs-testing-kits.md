---
spec: 2026-11-02-core-2-0-testing-kits
step: 05
status: done
---

# Step 05: Module docs — testing kits

## Summary
The testing-kit docs ship as NuGet READMEs and must match the 2.0 kits: `docs/Testing.md:19`
names `Retry.UntilConditionMet` (the method is `Retry.Until`), and the fixture, strict-mapping,
Ryuk, and `C-23` changes from steps 01–03 are undocumented (`P1-1`, `infra#18-19`). Every C#
snippet must compile against the 2.0 API.

Former core steps 59 (API.Testing and HTTP.Testing part), 60 (Redis.Testing part), and 61 (Testing kits part); split per library on 2026-11-02.

## Affected components
- `docs/API.Testing.md` — EDIT — fixture disposal (step 01)
- `docs/HTTP.Testing.md` — EDIT — strict mapping default (P-11) and step 03 names
- `docs/Redis.Testing.md` — EDIT — `Flush` name (`C-23`, confirmed 2026-11-02)
- `docs/Testing.md` — EDIT — `:19` `Retry.Until`; Ryuk note (step 02)
- `docs/SQL.Testing.md`, `docs/Blob.Testing.md`, `docs/ServiceBus.Testing.md` — EDIT — `C-23` names (confirmed 2026-11-02)

## Changes
- Verification method: copy every C# block into a throwaway console project (not committed) that references the seven `*.Testing` packages, then run `dotnet build`. Record the result in this step's Deviations if a snippet must stay illustrative (mark it with `// illustrative` in the doc).
- Follow [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md).

## Acceptance criteria
- [x] `grep -nE "\bAdd(MessageBus|ServiceBusHealthCheck|PersistentEvents|RecurringJob|CQRS|SQL|SqlHealthCheck)\b|WithPublisher|WithReceiver|UseSolTechnologyFilters|SmartRetry\(|UntilConditionMet" docs/Testing.md docs/API.Testing.md docs/HTTP.Testing.md docs/Redis.Testing.md docs/SQL.Testing.md docs/Blob.Testing.md docs/ServiceBus.Testing.md` prints nothing.
- [x] Snippets compile in the throwaway project (the PR author states it in the PR description).
- [x] The documentation-cleanup skill reports no broken links for `docs/API.Testing.md` and `docs/HTTP.Testing.md`.

## Open questions
- none

## Deviations

### Targeted edits, not a canonical-structure rewrite

**Original plan:** the listed edits per page, following `PublicDocumentationGuide.md`.

**Actual implementation:** the seven pages keep their current structure; only the listed content changed. `Testing.md`
names `Retry.Until` and `EnsureRunning` (its Ryuk section landed in step 02, with the `ServiceBus.Testing.md` sidecar
text). `API.Testing.md` gains a usage snippet and the disposal behaviour. `HTTP.Testing.md` gains a "Strict matching"
section (P-11, the comma-split hint, and the opt-out through `WireMockStartup.Run(settings)`), and its fake keeps the
path prefix in a private constant instead of the deleted `BaseUrl`. `SQL.Testing.md` gains a usage snippet with
`Initialize` and `Reset`. `Redis.Testing.md`, `Blob.Testing.md` (which now names the `azurite:3.37.0` default) and
`ServiceBus.Testing.md` use the `C-23` names. The `Ct.Any` snippet awaits the NSubstitute check, because without
`await` it raises CS4014 in a consumer that treats warnings as errors, and the second Bogus example has its own
method name so the block compiles.

### Other pages that called renamed members

Step 03's renames also broke snippets outside the seven pages, so `docs/Bus.md`, `docs/Blob.md`, `docs/SQL.md`,
`docs/Cache.md`, `docs/theQuality.md` and `docs/architecture/infrastructure-modules.md` now use the `C-23` names (a
name change only; the retrospective owns `docs/architecture/testing.md`).

Verification: all 16 C# blocks of the seven pages compile in a throwaway project under `.scratch/` (not committed)
that references the seven kits, with stub types for the consumer's own classes (`Customer`, `IGoogleHTTPClient`,
`Program`, `NotFoundError`); no snippet is marked illustrative. The acceptance grep prints nothing, and every relative
link and anchor of the seven pages resolves.
