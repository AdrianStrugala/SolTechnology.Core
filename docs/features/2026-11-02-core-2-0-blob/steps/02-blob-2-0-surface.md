---
spec: 2026-11-02-core-2-0-blob
step: 02
status: done
---

# Step 02: Blob — 2.0 surface (own namespace, cancellation, atomic metadata, internal factory)

## Summary
The Blob extensions have three problems (`infra#6`, `C-18`):

- They live in the third-party namespace `Azure.Storage.Blobs`.
- They hard-code `CancellationToken.None`.
- They upload content and then set metadata in a separate call. A reader in between, or a crash,
  sees an Avro blob without its format marker. The read side also fetches content and properties
  separately (`BlobContainerClientExtensions.cs:16-18,61-74`).

`BlobConnectionFactory` is a public implementation type. Q6 (user, 2026-11-02): no Blob test
project is created, so this step is validated by build and source checks only (accepted risk AR-1).

Former core step 50; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.Blob/BlobContainerClientExtensions/BlobContainerClientExtensions.cs` — EDIT — `namespace SolTechnology.Core.Blob`; `CancellationToken ct = default` on `ReadFromBlob<T>` and both `WriteToBlob`; upload with `BlobUploadOptions { Metadata = … }`; read the format from `DownloadContentAsync(ct).Value.Details.Metadata`
- `src/SolTechnology.Core.Blob/Connection/BlobConnectionFactory.cs` — EDIT — `internal sealed`
- `sample-tale-code-apps/DreamTravel/**` — EDIT — usings, if Blob is used

## Changes
- No metadata window: the format key travels in `BlobUploadOptions.Metadata` of the single upload call; no separate `SetMetadataAsync` call remains.
- Persisted-contract check (premortem trigger): the metadata key name and values stay byte-identical, so blobs written by 1.x are still read correctly.
- Premortem RM-12 (core step 00, 2026-11-02, scenario 12): 1.x preserves metadata that already exists on the blob (`BlobContainerClientExtensions.cs:62-66` copies `GetPropertiesAsync().Value.Metadata` before setting the format key). An overwriting upload with `BlobUploadOptions.Metadata` replaces all metadata, so the single upload must pass the merged dictionary: existing metadata, if the blob exists, plus the format key. The read side keeps the 1.x fallback: a missing format key means JSON.
- No automated test is added (Q6). Recommended, not a gate: record in the PR description a manual Avro and JSON round-trip against Azurite, including a `GetPropertiesAsync` check that the format key is present right after upload.

## Acceptance criteria
- [x] `dotnet build SolTechnology.Core.slnx -c Release` is green.
- [x] `grep -rnE "SetMetadataAsync|CancellationToken\.None" src/SolTechnology.Core.Blob` prints nothing.
- [x] `grep -rn "namespace Azure" src` prints nothing.
- [x] RM-12: the source of `WriteToBlob` builds the upload `Metadata` from the blob's existing metadata plus the format key (`grep -n "Metadata" src/SolTechnology.Core.Blob/BlobContainerClientExtensions/BlobContainerClientExtensions.cs` shows the merge). The PR's manual Azurite check, if run, shows that a pre-existing custom metadata key survives an overwrite.

## Open questions
- none (`C-18` confirmed by the user on 2026-11-02; Q6 resolved: no Blob test project)

## Deviations

### Manual Azurite check; no consumer to update

**Original plan:** no automated test (Q6); a manual Avro and JSON round-trip against Azurite is recommended;
DreamTravel usings change if it uses Blob.

**Actual implementation:** DreamTravel and the tests do not reference Blob, so no using changed and the coverage gap
stays AR-1. The uncommitted `.scratch/` console from step 01 ran against `azurite:latest` and passed: JSON and Avro
round-trips with the format key present right after the upload, the one-argument overload writes `json`, a custom
`owner` key survives an Avro overwrite (RM-12), a blob written the 1.x way (upload, then `SetMetadataAsync` with
`avro`) and a blob without the key (read as JSON) both read back, and a cancelled token stops both `WriteToBlob` and
`ReadFromBlob`. The write keeps step 01's single `ContainerNotFound` retry with the same options, and the read stays
buffered through `DownloadContentAsync`, as in 1.x.
