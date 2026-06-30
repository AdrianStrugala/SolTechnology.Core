---
adr: 013-release-1.0
step: 09 of 11
status: reviewed
---

<!-- Reviewed (2026-06-30): made step 09 the sole OWNER of the docs/Clients.md → HTTP disposition
     (answer 5) and clarified that step 11 only verifies the resulting links (resolves the 09↔11
     circular dependency).
     2026-06-30 (Tale decision): added the README `Story Framework` → `Tale Framework` row (badge
     SolTechnology.Core.Tale, link docs/Tale.md) + the hero StoryHandler→TaleHandler; the docs/Tale.md
     CONTENT (562-line Story.md rewrite) is authored by step 11, this step only repoints row/badge/hero. -->

# Step 09: README + `HTTP`-successor reconciliation + package parity

## Summary
Make the README package tables an exact mirror of what ships: repoint the ghost "HTTP Clients" row
from `ApiClient` to `SolTechnology.Core.HTTP`, and ensure every shipped package (including `Hangfire`
and the 7 `.Testing` companions) has exactly one row and vice-versa. **This step owns the
`docs/Clients.md` disposition** — repurpose it as the canonical `HTTP` doc (or a thin redirect to it);
step 11 only verifies that the resulting links resolve (no circular dependency). Docs-only PR.

## Affected components
- `README.md` — EDIT — Core Libraries table (HTTP row **and** the `Story Framework` → `Tale Framework`
  row) + the hero code block (`: StoryHandler<…>` → `: TaleHandler<…>`) + parity sweep.
- `docs/Clients.md` — EDIT — **repurpose as the `HTTP` doc** (or a thin redirect to a new `docs/HTTP.md`); this step is the owner of that decision. Coordinate the chosen target with the `HTTP` package README authored in step 02.

## Changes
- Replace the "HTTP Clients" row (currently links `docs/Clients.md`, badges
  `SolTechnology.Core.ApiClient`) with an `HTTP` row: link the HTTP doc (the repurposed `Clients.md`
  or its redirect target), badge `SolTechnology.Core.HTTP`. Add a one-line "successor of
  `SolTechnology.Core.ApiClient`" note + link to the migration guide (step 10).
- **Own the `docs/Clients.md` disposition (answer 5):** repurpose `Clients.md` into the `HTTP` doc, or
  make it a thin redirect to `docs/HTTP.md`. Whichever is chosen, this step is where it is decided and
  executed; step 11 only verifies link integrity against it.
- **`Story Framework` → `Tale Framework` row (decision 13).** Re-label the row, repoint its link to
  `docs/Tale.md`, and badge **`SolTechnology.Core.Tale`** (was `SolTechnology.Core.Story`). Add a
  one-line "succeeds `SolTechnology.Core.Story`" note + link to the migration guide (step 10). The old
  `SolTechnology.Core.Story` is **not** listed as a current package (same treatment as `ApiClient`).
  Also update the README **hero** code block: `: StoryHandler<CalculateBestPathQuery, …>` →
  `: TaleHandler<…>` (the class is already `CalculateBestPathTale`). The **content** of `docs/Tale.md`
  is authored by **step 11** (the 562-line `Story.md → Tale.md` rewrite via `documentation-cleanup`);
  this step only repoints the row/badge/hero. `docs/Tale.md` already exists (empty) so the link
  resolves; step 11 (last) fills it and is the final integrity gate.
- Verify the Core Libraries table has a row for each supported runtime package: `Core`, `API`,
  `HTTP`, `AUID`, `Authentication`, `BlobStorage`, `Cache`, `CQRS`, `Hangfire`, `Tale`, `Logging`,
  `MessageBus`, `SQL`. (`Hangfire` row already present; `Tale` replaces the `Story` row.)
- Verify the Testing companions table lists all 7 (already present) and that none point at a dead doc.
- **Do not hand-edit `nuget-stats.json`** — it is a generated snapshot (`lastUpdated` field). Record
  (in `docs/CICD.md`, step 10) that out-of-repo entries (`SolTechnology.Avro.*`,
  `SolTechnology.DateTimeZone`, `SolTechnology.Core.Flow`, `SolTechnology.Core.Faker`) and the
  deprecated `ApiClient` will persist in stats until they age out; they are **not** missing-source
  bugs.

## Acceptance criteria
- [ ] No README row references `SolTechnology.Core.ApiClient` **or** `SolTechnology.Core.Story` as a
      current package.
- [ ] The `Tale Framework` row badges `SolTechnology.Core.Tale`, links `docs/Tale.md`, and notes it
      succeeds `SolTechnology.Core.Story` (+ migration link); the README hero reads `: TaleHandler<…>`.
- [ ] Every supported runtime + testing package has exactly one README row; every README row maps to a
      shipped package.
- [ ] The HTTP row badges `SolTechnology.Core.HTTP` and links a live HTTP doc (the repurposed
      `Clients.md` or its redirect target).
- [ ] `docs/Clients.md` is either the `HTTP` doc or a working redirect to it — no dangling ApiClient page.
- [ ] `nuget-stats.json` is unchanged by hand; its out-of-scope packages are documented in `CICD.md`.

## Open questions
- none — `Clients.md` → HTTP disposition is owned and resolved here (answer 5); step 11 only verifies links.





