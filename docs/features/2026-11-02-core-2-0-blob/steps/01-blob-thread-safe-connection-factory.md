---
spec: 2026-11-02-core-2-0-blob
step: 01
status: done
---

# Step 01: Blob — thread-safe container cache with async creation

## Summary
The singleton `BlobConnectionFactory` mutates a plain `Dictionary` with check-then-add, so
concurrent first calls throw "same key" or corrupt the dictionary. It also calls the blocking
`CreateIfNotExists()` over the network (`B6`, `Connection/BlobConnectionFactory.cs:10,17-27`).
Cached clients also outlive `AzuriteFixture.ClearAsync`, which deletes every container
(`infra#7`). Q6 (user, 2026-11-02): no new `tests/SolTechnology.Core.Blob.Tests` project, and no
existing test project references `SolTechnology.Core.Blob`. This step is therefore validated by
build and source checks only; the missing coverage is accepted risk AR-1 in the core brief.

Former core step 16; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.Blob/Connection/BlobConnectionFactory.cs` — EDIT — `ConcurrentDictionary<string, Lazy<Task<BlobContainerClient>>>`
- `src/SolTechnology.Core.Blob/Connection/IBlobConnectionFactory.cs` — EDIT — only if an async accessor is required (if so, it is a public change: stop and request confirmation)

## Changes
- Container creation runs once per name through `GetOrAdd` with `CreateIfNotExistsAsync`.
- A `ContainerNotFound` (404) on use evicts the cache entry and creates the container again once. This fixes the `ClearAsync` interaction without changing the fixture.
- No automated test is added (Q6). Recommended, not a gate: record in the PR description a manual check against Azurite (`AzuriteFixture` or `docker run mcr.microsoft.com/azure-storage/azurite`): 50 parallel first calls create the container once, and a call after container deletion recreates it.

## Acceptance criteria
- [x] `dotnet build SolTechnology.Core.slnx -c Release` is green.
- [x] `grep -nE "(^|[^A-Za-z])Dictionary<|CreateIfNotExists\(" src/SolTechnology.Core.Blob/Connection/BlobConnectionFactory.cs` prints nothing.
- [x] No public or protected signature changed (otherwise stop for confirmation).

## Open questions
- none

## Deviations

### Recreation after deletion happens in the write path

**Original plan:** a `ContainerNotFound` (404) on use evicts the factory's cache entry and creates the container
again once; only the two `Connection/` files change.

**Actual implementation:** `WriteToBlob` catches `RequestFailedException` with `BlobErrorCode.ContainerNotFound`,
calls `CreateIfNotExistsAsync` on the same client, and retries the upload once. The extensions never see the factory,
and the cached `BlobContainerClient` is a stateless handle, so recreating the container through it is enough; the
factory evicts an entry only when its creation failed, so a transient failure is not cached forever. Reads do not
recreate: after a deletion the blob cannot exist, so they still fail with 404. `GetConnection` stays synchronous (an
async accessor would change `IBlobConnectionFactory`), so the first call per name waits for `CreateIfNotExistsAsync`.

### Manual Azurite check instead of an automated test

**Original plan:** no automated test (Q6, AR-1); a manual Azurite check is recommended.

**Actual implementation:** no test project or sample references `SolTechnology.Core.Blob` or `Blob.Testing`
(`grep -rl "Core.Blob" --include=*.csproj` finds only the two packages), so the coverage gap stays AR-1. An
uncommitted console in `.scratch/` ran against Azurite through `AddSolBlob`: the 1.x factory copied verbatim threw on
49 of 50 parallel first calls; the new factory returned one cached client for 50 parallel first calls, and a write
after `AzuriteFixture.ClearAsync` recreated the container and read back. `AzuriteFixture`'s default image
`azurite:3.35.0` rejects the service version `2026-06-06` of `Azure.Storage.Blobs` 12.29.0 with 400
`InvalidHeaderValue`, so the check ran on `azurite:latest` (built 2026-08-25); this is a follow-up for the
testing-kits feature.
