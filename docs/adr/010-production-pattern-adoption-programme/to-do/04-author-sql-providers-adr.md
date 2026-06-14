---
adr: 010-production-pattern-adoption-programme
step: 04 of 10
status: to-do
---

# Step 04: Author the SQL connection-providers child ADR (S1, S2, S4)

## Summary
Author the child ADR (provisional ADR-013) that adds pluggable connection-string providers,
SQL-state → `Result` error translation, and a documented `Result`-returning repository convention to
`SolTechnology.Core.SQL`. The EF `EntityBase` companion (S3) is a separate, conditional ADR
(step 09), not bundled here. Seeds its own plan and premortem.

## Affected components
- `docs/adr/<next>-sql-connection-providers.md` — the child ADR.
- `docs/adr/<next>-sql-connection-providers/` — its plan folder.

## Details
- **S1 — provider seam.** `ISqlConnectionStringProvider` with `Plain`, `LoginPassword`, and
  `ManagedIdentity` implementations, selected by config; **default = static string** so existing
  `SQLConnectionFactory` (`src/SolTechnology.Core.SQL/Connections/`) consumers are untouched.
  **Guard-rail (source defect):** the managed-identity provider MUST cache the AAD token until near
  expiry — never fetch a fresh token per connection request.
- **S2 — error translation.** Map well-known SQL-state / SQL-Server error numbers (unique violation,
  deadlock, timeout) to `Result` errors. Core SQL is `Microsoft.Data.SqlClient` (SQL Server) today,
  so target SQL Server numbers; keep the helper extensible.
- **S4 — repository convention.** Document "repositories return `Result`, never throw for expected
  outcomes (not-found, duplicate)" in `docs/SQL.md` + `ClaudeCodingGuide.md` §5/§13.
- **Dependency impact (`CLAUDE.md` §1):** `Azure.Identity` is needed only by the managed-identity
  provider — run `package-management` + `dependency-audit` and report in the child ADR.

## Acceptance criteria
- Child ADR authored with blue/red + premortem-as-final-step; semver **MINOR**.
- Token-caching guard-rail for the managed-identity provider is explicit in the ADR.
- Default-stays-static is stated so the change is non-breaking for current consumers.
- `Azure.Identity` dependency impact reported per `CLAUDE.md` §1.
- Index row added in `docs/adr/README.md`.

## Open questions
- `Azure.Identity` adoption for the managed-identity provider (report in the child ADR).

