---
adr: 010-production-pattern-adoption-programme
step: 03 of 07
status: to-do
---
# Step 03: SQL — provider seam + error translator + repository convention

## Summary
Add pluggable connection-string providers, SQL-error-to-Result translation, and document the repository convention.

## Affected components
- `src/SolTechnology.Core.SQL/Connections/ISqlConnectionStringProvider.cs` — new (S1)
- `src/SolTechnology.Core.SQL/Connections/PlainConnectionStringProvider.cs` — new (S1)
- `src/SolTechnology.Core.SQL/Connections/ManagedIdentityProvider.cs` — new (S1)
- `src/SolTechnology.Core.SQL/Errors/SqlErrorTranslator.cs` — new (S2)
- `src/SolTechnology.Core.SQL/ModuleInstaller.cs` — register provider
- `src/SolTechnology.Core.SQL/SolTechnology.Core.SQL.csproj` — conditionally add `Azure.Identity`
- `docs/SQL.md` — repository convention section (S4)

## Details
- **S1:** `ISqlConnectionStringProvider.GetConnectionStringAsync()`. Default = `PlainConnectionStringProvider` (wraps static string). `ManagedIdentityProvider` caches token until `ExpiresOn - 5min`.
- **S2:** Maps SQL Server errors 2627/2601 → duplicate, 1205 → deadlock, -2 → timeout to typed `Error` values.
- **S4:** Document "repositories return `Result`, never throw for expected outcomes" in `docs/SQL.md`.

## Acceptance criteria
- Existing `AddSQL(config)` unchanged (plain provider default)
- Token-caching: repeated calls within TTL don't hit AAD
- Error translator maps known errors correctly
- Tests pass

## Open questions
- Evaluate if `Microsoft.Data.SqlClient` `Authentication=ActiveDirectoryManagedIdentity` removes need for `Azure.Identity` direct dep

