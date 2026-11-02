---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 16
status: to-do
---

# Step 16: Blob — thread-safe container cache with async creation

## Summary
The singleton `BlobConnectionFactory` mutates a plain `Dictionary` with check-then-add, so
concurrent first calls throw "same key" or corrupt the dictionary. It also calls the blocking
`CreateIfNotExists()` over the network (`B6`, `Connection/BlobConnectionFactory.cs:10,17-27`).
Cached clients also outlive `AzuriteFixture.ClearAsync`, which deletes every container
(`infra#7`). Q6 (user, 2026-11-02): no new `tests/SolTechnology.Core.Blob.Tests` project, and no
existing test project references `SolTechnology.Core.Blob`. This step is therefore validated by
build and source checks only; the missing coverage is accepted risk AR-1 in the feature brief.

## Affected components
- `src/SolTechnology.Core.Blob/Connection/BlobConnectionFactory.cs` — EDIT — `ConcurrentDictionary<string, Lazy<Task<BlobContainerClient>>>`
- `src/SolTechnology.Core.Blob/Connection/IBlobConnectionFactory.cs` — EDIT — only if an async accessor is required (if so, it is a public change: stop and request confirmation)

## Changes
- Container creation runs once per name through `GetOrAdd` with `CreateIfNotExistsAsync`.
- A `ContainerNotFound` (404) on use evicts the cache entry and creates the container again once. This fixes the `ClearAsync` interaction without changing the fixture.
- No automated test is added (Q6). Recommended, not a gate: record in the PR description a manual check against Azurite (`AzuriteFixture` or `docker run mcr.microsoft.com/azure-storage/azurite`): 50 parallel first calls create the container once, and a call after container deletion recreates it.

## Acceptance criteria
- [ ] `dotnet build SolTechnology.Core.slnx -c Release` is green.
- [ ] `grep -nE "(^|[^A-Za-z])Dictionary<|CreateIfNotExists\(" src/SolTechnology.Core.Blob/Connection/BlobConnectionFactory.cs` prints nothing.
- [ ] No public or protected signature changed (otherwise stop for confirmation).

## Open questions
- none

## Deviations
