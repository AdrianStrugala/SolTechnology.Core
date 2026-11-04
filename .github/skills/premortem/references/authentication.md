# Auth — `src/SolTechnology.Core.Auth/`

- Comparison change (hashing, encoding, trimming, case) → valid keys rejected, or invalid keys
  accepted.
- Timing side channel reintroduced (string equality, early exit on length).
- Options shape change (field ↔ property, renamed key) → configuration binding silently leaves
  the key empty; an empty configured key must never authenticate.
- Handler constructor change → consumers that subclass the handler or register it manually stop
  compiling or fail at resolve.
- Key, header value, or claim written to logs or to a problem response.
- Scheme name or header name change → every client is rejected.
