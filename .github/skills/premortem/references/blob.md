# BlobStorage — `src/SolTechnology.Core.BlobStorage/`

- SAS / connection string handling change → permissions either too narrow or too broad.
- ETag / optimistic concurrency removed → last-write-wins regression.
- Streaming vs buffered upload change → memory spike.
- Default `BlobClientOptions` swapped → retry / timeout drift.

