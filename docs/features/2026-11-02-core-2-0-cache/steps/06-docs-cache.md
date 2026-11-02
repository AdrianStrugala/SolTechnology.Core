---
spec: 2026-11-02-core-2-0-cache
step: 06
status: to-do
---

# Step 06: Module docs — `docs/Cache.md`

## Summary
`docs/Cache.md` uses `AddLocalCache`, `AddDistributedCache`, `AddLocalLock`, and similar methods,
which do not exist (`logging-cache#15`, `:9-10,31,44,207-212,240-244,334-341`). Every C# snippet
must compile against the 2.0 API.

Former core step 60 (Cache part); split per library on 2026-11-02.

## Affected components
- `docs/Cache.md` — EDIT — `AddSolLocalCache`, `AddSolDistributedCache`, `AddSolLocalLock`, `AddSolDistributedLock`, `AddSolLocalIdempotency`, `AddSolDistributedIdempotency`; `InstanceName` must be non-empty (validated at start, P-07); `:272` lock expiry semantics; `C-13` names

## Changes
- Verification method: copy every C# block into a throwaway console project (not committed) that references `SolTechnology.Core.Cache`, then run `dotnet build`. Record the result in this step's Deviations if a snippet must stay illustrative (mark it with `// illustrative` in the doc).
- Follow [`PublicDocumentationGuide.md`](../../../../docs/PublicDocumentationGuide.md).
- Premortem mitigation (core step 00, 2026-11-02), documentation part:
  - RM-2 (scenario 2): `docs/Cache.md` states that `InstanceName` prefixes cache, lock (`lock:`), and idempotency (`idempotency:`) keys, and that `InstanceName = "SolTechnology:"` keeps the 1.x lock and idempotency keyspace during an upgrade.

## Acceptance criteria
- [ ] `grep -nE "\bAdd(Local|Distributed)(Cache|Lock|Idempotency)\b" docs/Cache.md` prints nothing.
- [ ] Snippets compile in the throwaway project (the PR author states it in the PR description).
- [ ] RM-2: `grep -n "SolTechnology:" docs/Cache.md` matches the new text.

## Open questions
- none

## Deviations
