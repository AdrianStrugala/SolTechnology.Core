---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 50
status: to-do
---

# Step 50: Blob — 2.0 surface (own namespace, cancellation, atomic metadata, internal factory)

## Summary
The Blob extensions have three problems (`infra#6`, `C-18`):

- They live in the third-party namespace `Azure.Storage.Blobs`.
- They hard-code `CancellationToken.None`.
- They upload content and then set metadata in a separate call. A reader in between, or a crash,
  sees an Avro blob without its format marker. The read side also fetches content and properties
  separately (`BlobContainerClientExtensions.cs:16-18,61-74`).

`BlobConnectionFactory` is a public implementation type. Q6 (user, 2026-11-02): no Blob test
project is created, so this step is validated by build and source checks only (accepted risk AR-1).

## Affected components
- `src/SolTechnology.Core.Blob/BlobContainerClientExtensions/BlobContainerClientExtensions.cs` — EDIT — `namespace SolTechnology.Core.Blob`; `CancellationToken ct = default` on `ReadFromBlob<T>` and both `WriteToBlob`; upload with `BlobUploadOptions { Metadata = … }`; read the format from `DownloadContentAsync(ct).Value.Details.Metadata`
- `src/SolTechnology.Core.Blob/Connection/BlobConnectionFactory.cs` — EDIT — `internal sealed`
- `sample-tale-code-apps/DreamTravel/**` — EDIT — usings, if Blob is used

## Changes
- No metadata window: the format key travels in `BlobUploadOptions.Metadata` of the single upload call; no separate `SetMetadataAsync` call remains.
- Persisted-contract check (premortem trigger): the metadata key name and values stay byte-identical, so blobs written by 1.x are still read correctly.
- Premortem RM-12 (step 00, 2026-11-02, scenario 12): 1.x preserves metadata that already exists on the blob (`BlobContainerClientExtensions.cs:62-66` copies `GetPropertiesAsync().Value.Metadata` before setting the format key). An overwriting upload with `BlobUploadOptions.Metadata` replaces all metadata, so the single upload must pass the merged dictionary: existing metadata, if the blob exists, plus the format key. The read side keeps the 1.x fallback: a missing format key means JSON.
- No automated test is added (Q6). Recommended, not a gate: record in the PR description a manual Avro and JSON round-trip against Azurite, including a `GetPropertiesAsync` check that the format key is present right after upload.

## Acceptance criteria
- [ ] `dotnet build SolTechnology.Core.slnx -c Release` is green.
- [ ] `grep -rnE "SetMetadataAsync|CancellationToken\.None" src/SolTechnology.Core.Blob` prints nothing.
- [ ] `grep -rn "namespace Azure" src` prints nothing.
- [ ] RM-12: the source of `WriteToBlob` builds the upload `Metadata` from the blob's existing metadata plus the format key (`grep -n "Metadata" src/SolTechnology.Core.Blob/BlobContainerClientExtensions/BlobContainerClientExtensions.cs` shows the merge). The PR's manual Azurite check, if run, shows that a pre-existing custom metadata key survives an overwrite.

## Open questions
- none (`C-18` confirmed by the user on 2026-11-02; Q6 resolved: no Blob test project)

## Deviations
