---
adr: 008-testing-framework-companions
step: 07 of 11
status: reviewed
---

<!-- Reviewed: renumbered from to-do/06-blobstorage-testing.md. Step-01 references → step 02;
     no test project. -->

# Step 07: `SolTechnology.Core.BlobStorage.Testing` (Azurite, Azure-specific)

## Summary
Package the Azurite blob-storage container fixture as the Azure-specific companion of
`SolTechnology.Core.BlobStorage`. **Azure only — no LocalStack/AWS.** Single small PR — one fixture,
one concern.

## Affected components
- `src/SolTechnology.Core.BlobStorage.Testing/SolTechnology.Core.BlobStorage.Testing.csproj` — new package (`Testcontainers.Azurite`), version `0.1.0`. Depends on `SolTechnology.Core.Testing`.
- `src/SolTechnology.Core.BlobStorage.Testing/AzuriteFixture.cs` — port of KYC `Infrastructure/Containers/BlobStorageFixture.cs`; exposes the Azure Storage connection string.
- `docs/Blob.md` — note the companion (full pass in step 11).
- `SolTechnology.Core.slnx` — add project.

## Details
- Azure-specific by design: the LocalStack/S3 path from KYC is intentionally **not** ported.
- **Consume the shared lifetime model from step 02**: booted once by the consumer's assembly-level `[OneTimeSetUp]` (within-run reuse free); across-run reuse via `TestContainersContext`'s `TESTCONTAINERS_REUSE` policy (Testcontainers-native `.WithReuse(true)` + stable name) + `ContainerLifecycleHelper.EnsureRunningAsync`; dispose no-op when reuse on. No `ContainerReuse` helper — do not hand-roll a reuse cache.
- No coupling to `SolTechnology.Core.BlobStorage` runtime registration — the fixture only stands up Azurite + returns connection details.
- Optional container-reset / clear-containers helper for between-test cleanup.
- **No test project.** Per ADR-008 there is intentionally no `tests/SolTechnology.Core.BlobStorage.Testing.Tests`; validation is build-based plus a documented manual smoke (blob round-trips). Nothing is added to `tests/`, so PR/CI builds are unaffected.

## Acceptance criteria
- `dotnet build src/SolTechnology.Core.BlobStorage.Testing` succeeds.
- A documented manual smoke confirms the Azurite fixture creates a container and a blob round-trips.
- With `TESTCONTAINERS_REUSE=true`, the container is reused across runs; with it off, disposed (documented manual smoke).
- No LocalStack/AWS types in the package.

## Open questions
- Confirm `Testcontainers.Azurite` version via `package-management` skill.

