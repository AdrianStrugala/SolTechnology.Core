---
adr: 008-testing-framework-companions
step: 07 of 11
status: done
---

<!-- Reviewed: renumbered from to-do/06-blobstorage-testing.md. Step-01 references → step 02;
     no test project. -->

<!-- IMPLEMENTATION DECISIONS (done):
  1. `AzuriteFixture` is an `IAsyncDisposable` POCO (matches `RedisFixture` / `SQLFixture` style;
     the plan's `IAsyncLifetime` mention was xUnit-flavoured — companions expose InitializeAsync +
     DisposeAsync directly, NUnit-friendly).
  2. Did NOT port KYC's hand-rolled reuse cache (Semaphore + static _sharedContainer + _initialized).
     Reuse defers to `TestContainersContext.ReuseContainers` (step 02): `WithCleanUp(!Reuse)`, and when
     reuse is on → `WithName().WithReuse(true).WithAutoRemove(false)` + `ContainerLifecycleHelper
     .EnsureRunningAsync` (restarts the container if it was stopped externally between runs).
  3. Exposes `ConnectionString`, plus ergonomic `CreateBlobContainerAsync(name)` and `ClearAsync()`
     (between-test reset: deletes all blob containers), and `WithNetwork(network, alias)` for parity.
  4. Azure-specific by design — LocalStack/S3 intentionally not ported.
  5. Packages: `Testcontainers.Azurite` 3.9.0 (match the Testcontainers 3.9.0 family — KYC's 4.3.0
     pulls Testcontainers 4.x and would conflict with the other companions) + `Azure.Storage.Blobs`
     12.23.0 (match the runtime BlobStorage package). Both CVE-clean (validate_cves). Added to
     canonical-versions.md. Default image: azurite:3.35.0.

  Validation: build -c Release → 0 errors. Manual smoke (throwaway console, since no test project):
  Azurite boots, CreateBlobContainerAsync + upload/download round-trips 'hello-azurite', ClearAsync
  drops all containers (0 remaining), dispose → SMOKE_OK EXIT=0. Smoke project deleted after the run. -->

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
- `dotnet build src/SolTechnology.Core.BlobStorage.Testing` succeeds. ✅
- A documented manual smoke confirms the Azurite fixture creates a container and a blob round-trips. ✅
- With `TESTCONTAINERS_REUSE=true`, the container is reused across runs; with it off, disposed (documented manual smoke). ✅ (reuse path: `WithReuse(true)` + `EnsureRunningAsync`; dispose no-op when reuse on)
- No LocalStack/AWS types in the package. ✅

## Open questions
- Confirm `Testcontainers.Azurite` version via `package-management` skill. → **3.9.0** (match the Testcontainers family).

