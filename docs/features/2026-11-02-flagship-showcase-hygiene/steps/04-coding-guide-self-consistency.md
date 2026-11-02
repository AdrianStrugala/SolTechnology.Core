---
spec: 2026-11-02-flagship-showcase-hygiene
step: 04
status: to-do
---

# Step 04: Coding Guide self-consistency

## Summary
The binding guide contradicts itself and the code (`P1-6`):

- §12 requires the obsolete, non-packable `SolTechnology.Core.Guards` (`logging-cache#16`,
  `docs/ClaudeCodingGuide.md:504`).
- §7 names `AddAuthenticationAndBuildFilter` and `ResponseEnvelopeFilter`, neither of which
  exists.
- §7 presents `CalculateBestPathController` as its template, but that file does not follow §7
  (fixed in step 03).
- §15 lists "dupa" and controller `try/catch` as live examples, which core 05 (former core step 08) and step 03 remove.

## Affected components
- `docs/ClaudeCodingGuide.md` — EDIT — §12 → BCL guards (`ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`, `ArgumentOutOfRangeException.ThrowIfNegative`); §7 `AddSolAuthentication`, `ExceptionFilter` + `ResultConversionFilter`; §15 "Where seen" column marks fixed examples as historical without deleting the rule
- `CLAUDE.md` — EDIT — `§11` gotcha rows still accurate after tale 03, tale 11, and auid 03 (former core steps 18, 43, 44)

## Changes
- Prerequisite: core 05 ([`2026-11-02-core-2-0-flagship-hardening`](../../2026-11-02-core-2-0-flagship-hardening.md)), tale 03 and tale 11 ([`2026-11-02-core-2-0-tale`](../../2026-11-02-core-2-0-tale.md)), auid 03 ([`2026-11-02-core-2-0-auid`](../../2026-11-02-core-2-0-auid.md)) (former core steps 08, 18, 43, 44), and step 03 of this feature are merged. This step moved here from the core feature at the split (2026-11-02) because it must follow step 03.
- Rules are not weakened; only stale references change.

## Acceptance criteria
- [ ] `grep -nE "SolTechnology\.Core\.Guards|AddAuthenticationAndBuildFilter|ResponseEnvelopeFilter" docs/ClaudeCodingGuide.md` prints nothing.
- [ ] The documentation-cleanup skill passes for `docs/ClaudeCodingGuide.md` and `CLAUDE.md`.

## Open questions
- none

## Deviations
