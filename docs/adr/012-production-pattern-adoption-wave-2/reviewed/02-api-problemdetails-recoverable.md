---
adr: 012-production-pattern-adoption-wave-2
step: 02 of 24
status: reviewed
---

# Step 02: B4 — Surface `Recoverable` in the API `ProblemDetails` (`Core.Api`)

## Summary
The foundation `Error` record already carries `public bool Recoverable { get; init; }`
(`src/SolTechnology.Core/Error.cs`), but the hint is dropped at the HTTP boundary —
`ApiProblemDetailsFactory` never writes it into the response. This step writes
`Extensions["recoverable"]` on both the `FromError` and `FromException` paths so a client can tell
whether retrying is worthwhile. Separate PR (kept independent of step 01): a small, contract-affecting
change to one factory plus its documentation.

## Affected components
- `src/SolTechnology.Core.API/Exceptions/ApiProblemDetailsFactory.cs` — add a
  `RecoverableKey = "recoverable"` constant; write the extension in `FromError(...)` and
  `FromException(...)`. **The class is `internal static`** today.
- `docs/Api.md` — document `extensions.recoverable` as a stated response contract (alongside the
  existing `extensions.correlationId` documentation).
- `tests/SolTechnology.Core.API.Tests/` — assert the extension **through the pipeline** (the factory is
  internal — see the testing note below), on both the success-to-failure conversion and the exception
  path.

## Details
- **`FromError` path:** the current signature is `FromError(Error error, string? correlationId)` —
  **there is no `options` parameter on this path.** Set
  `problem.Extensions[RecoverableKey] = error.Recoverable;` directly. Always emit the key (even when
  `false`) so absence is never ambiguous — per the harvest's recommendation. The `Error` is in hand,
  so no status-derived default is needed here.
- **`FromException` path:** the current signature is
  `FromException(Exception exception, int statusCode, string? correlationId, ApiExceptionOptions options)`.
  The `Error` is not available, so derive a conservative default from the mapped status code: an
  **unmapped 5xx ⇒ `recoverable = true`** (transient server fault worth retrying), a **mapped 4xx ⇒
  `recoverable = false`** (deterministic client/business rejection). **Note `ApiExceptionOptions`
  exposes only `IncludeExceptionDetails` today — there is no recoverable-override hook.** Document the
  status-derived default as the contract; if a per-host override is later wanted it is a **new,
  additive `ApiExceptionOptions` field**, not an existing seam, and is out of scope for this step.
- Follow the existing extension conventions in this factory: camelCase key, written into
  `ProblemDetails.Extensions`, mirroring how `correlationId` is handled (`CorrelationIdKey`).
- Consider the framework-level `AddProblemDetails` customisation in
  `Core.Api/ModuleInstaller.cs` — for non-MVC paths (routing 404, status-code pages) the error
  object is absent, so the same status-derived default applies. Decide whether to also stamp
  `recoverable` there for consistency, or scope this step strictly to the MVC factory and note the
  gap. Recommend stamping in both for a uniform contract.
- Pairs with B2 (step 18): the producer states recoverability here; the consumer's HTTP layer can
  act on it.

### Testing note (factory is internal — assert through the pipeline)
`ApiProblemDetailsFactory` is `internal static` and there is **no `InternalsVisibleTo` from
`SolTechnology.Core.API` to `SolTechnology.Core.API.Tests`** today (verified). Tests therefore MUST
NOT call `ApiProblemDetailsFactory.FromError(...)` / `FromException(...)` directly. Assert the
`recoverable` extension **through the request pipeline** — drive a failed `Result` through
`ResultConversionFilter` and a mapped exception through `ExceptionFilter` using the API test host
(the existing `Core.API.Tests` fixture) and inspect the produced `ProblemDetails` body. Do **not**
add `InternalsVisibleTo` solely for this test.

## Acceptance criteria
- `ProblemDetails` produced from a failed `Result` carries `extensions.recoverable` reflecting
  `Error.Recoverable`, always present.
- `ProblemDetails` produced from a mapped exception carries `extensions.recoverable = false` for
  mapped 4xx and `true` for unmapped/5xx, by the documented conservative default.
- `docs/Api.md` documents the field as a contract.
- Tests cover (via the pipeline, not the internal factory): `Error.Recoverable=true`/`false` through
  `FromError`; a mapped 4xx and an unmapped 5xx through the exception path.

## Open questions
- Whether to also stamp `recoverable` on the framework `AddProblemDetails` customisation for
  non-MVC paths. Recommend yes (uniform contract); flag for the reviewer.

