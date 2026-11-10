# SQL — `src/SolTechnology.Core.SQL/`

- Migration ordering / missing migration → schema drift between envs.
- Mixed Dapper + EF in one transaction → inconsistent state on failure.
- Connection / transaction leak under exception path.
- Parameter binding change → SQL injection regression.
- Index dropped without a `BREAKING CHANGE:` → query plan regression on consumer DBs.
- SQL command classification by the first keyword → EF Core batches (`SET NOCOUNT ON;`) and
  `TagWith` comments misnamed; skip leading `SET …;` statements and comments.
- Capturing statement text or SqlClient error messages → literal values (PII) in logs; keep it
  opt-in.
