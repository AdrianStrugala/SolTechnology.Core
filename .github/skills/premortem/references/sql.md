# SQL — `src/SolTechnology.Core.SQL/`

- Migration ordering / missing migration → schema drift between envs.
- Mixed Dapper + EF in one transaction → inconsistent state on failure.
- Connection / transaction leak under exception path.
- Parameter binding change → SQL injection regression.
- Index dropped without a `BREAKING CHANGE:` → query plan regression on consumer DBs.

