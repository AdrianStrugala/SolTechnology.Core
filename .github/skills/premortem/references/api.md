# Api — `src/SolTechnology.Core.Api/`

- `ProblemDetails` shape change (title, type, extensions, status mapping) → clients that parse
  the body break.
- `ResultConversionFilter` change → status codes, `Location` headers, or the `Result` JSON shape
  drift for every endpoint.
- Filter registration change (`Filters.Add<T>` ↔ `AddService<T>`) → lifetime and constructor
  resolution change; missing registrations fail at the first request, not at startup.
- Exception detail leak (message, stack trace, upstream URI) in a 5xx body or log scope.
- Swagger / API-versioning registration change → generated documents or routes disappear.
- `Error` subtype serialized through MVC `JsonOptions` instead of the module's options →
  discriminator or casing drift.
- Controller moved from hand-written `try/catch` or `BadRequest(...)` to `IMediator` plus
  `ResultConversionFilter` → status codes follow the `Error` type now; an upstream miss that an
  adapter reports as a generic error turns a former 400 into a 500. Pin every former status code
  (validation, not found, upstream failure) with a component test before the move, and make
  adapters return `NotFoundError` for "no such resource" answers (Google `ZERO_RESULTS`).
