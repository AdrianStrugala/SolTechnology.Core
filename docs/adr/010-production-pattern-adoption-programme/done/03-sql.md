---
adr: 010-production-pattern-adoption-programme
step: 03 of 07
status: done
---
# Step 03: SQL — error translator + repository convention

## Summary
Add SQL-error-to-Result translation and document the repository convention. Connection-string provider (S1) deferred — YAGNI for current consumers.

## Affected components
- `src/SolTechnology.Core.CQRS/Errors/TimeoutError.cs` — new
- `src/SolTechnology.Core.CQRS/Errors/DeadlockError.cs` — new
- `src/SolTechnology.Core.CQRS/Errors/Error.cs` — added JsonDerivedType for new errors
- `src/SolTechnology.Core.SQL/SqlErrorTranslator.cs` — new (S2)
- `src/SolTechnology.Core.SQL/SolTechnology.Core.SQL.csproj` — added ProjectReference to CQRS, Microsoft.Data.SqlClient
- `src/SolTechnology.Core.SQL/Connections/SQLConnectionFactory.cs` — removed stale System.Data.SqlClient using
- `docs/SQL.md` — repository convention + error translator documentation (S4)

## Details
- **S2:** `SqlErrorTranslator.Execute(Func<Task>)` and `.Execute<T>(Func<Task<T>>)` wrap DB operations, catch `SqlException`, and map known error codes (2627/2601 → ConflictError, 1205 → DeadlockError, -2 → TimeoutError) to typed `Result` failures.
- **S4:** Document "repositories return `Result`, never throw for expected outcomes" in `docs/SQL.md`.
- **S1 (deferred):** ISqlConnectionStringProvider / ManagedIdentityProvider — not needed by DreamTravel or any current consumer.

## Acceptance criteria
- Error translator maps known errors correctly
- New error types serializable via System.Text.Json polymorphic dispatch
- docs/SQL.md covers repository convention
