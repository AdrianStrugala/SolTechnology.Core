---
adr: 010-production-pattern-adoption-programme
step: 09 of 10
status: to-do
---

# Step 09: Author the SQL EF-companion child ADR (S3) — conditional

## Summary
**Conditional on open question Q2 (step 01).** If the EF companion is approved, author the child ADR
(provisional ADR-018) for a new `SolTechnology.Core.SQL.EntityFramework` package shipping `EntityBase`
+ automatic `Created`/`Updated` UTC timestamps. If declined, record the decision and keep the
`EntityBase` pattern documentation-only. Kept separate from ADR-013 because adding EF Core to a
today-Dapper-only module is a distinct architectural decision and a new `src/` package.

## Affected components
- `docs/adr/<next>-sql-entityframework-companion.md` — the child ADR (if approved).
- `docs/adr/<next>-sql-entityframework-companion/` — its plan folder (if approved).

## Details
- **S3 — `EntityBase` + timestamps.** A reusable base `DbContext` that stamps `Created`/`Updated` on
  `EntityBase` entities and forces `DateTimeKind.Utc`. **Guard-rail (G1, source defect):** stamp via
  the injected `TimeProvider`, never raw `DateTime.UtcNow` — the exact bug the reference app shipped.
- **New `src/` package.** `SolTechnology.Core.SQL.EntityFramework` is a new top-level package — gated
  by `CLAUDE.md` §1 maintainer confirmation. EF Core (+ a provider) is a new dependency; run
  `package-management` + `dependency-audit` and report.
- **If declined:** append the decision to ADR-010 and document the `EntityBase`/timestamp shape as a
  consumer-owned pattern in `docs/SQL.md`; do not create the package.

## Acceptance criteria
- If approved: child ADR authored with blue/red + premortem-as-final-step; new-package confirmation
  obtained per `CLAUDE.md` §1; timestamps use `TimeProvider` (not `DateTime.UtcNow`); semver **MINOR**
  (new package). Index row added in `docs/adr/README.md`.
- If declined: the not-to-build decision is recorded in ADR-010 and the pattern is documented only.

## Open questions
- Q2 (EF companion yes/no) — must be resolved in step 01.

