# AUID — `src/SolTechnology.Core.AUID/`

- String format change (length, separators, culture, calendar) → stored IDs no longer parse;
  every persisted record, URL, and log query that holds the string form breaks.
- Parse made stricter → IDs that an earlier version wrote (even with corrupt bits) become
  unreadable; name the affected population in the release notes.
- `AuidJsonConverter` / `AuidTypeConverter` behaviour change (empty string, invalid input, error
  type) → deserialization of persisted JSON or model binding fails with a new exception type.
- Namespace or type move → binary break for sibling packages still on the old version
  (`TypeLoadException`), even when the JSON shape is unchanged.
- Bit layout or ordering change → sort order, range queries, and collision bound shift silently.
- Process-wide mutable state (`TimeProvider`) → parallel tests and libraries interfere.
