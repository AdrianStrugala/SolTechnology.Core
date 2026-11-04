---
spec: 2026-11-02-core-2-0-http
step: 03
status: done
---

# Step 03: HTTP — request pipeline correctness

## Summary
Four defects sit in the HTTP request and retry path:

- `RequestBuilder`'s `catch … when (ex is not OperationCanceledException)` also skips
  `TaskCanceledException`, so an `HttpClient.Timeout` escapes `Try*Async` as an exception, and
  the `TimeoutError` branch is dead (`http-api#5`).
- Retry evaluation buffers the whole error body without a cap or a cancellation token
  (`http-api#16`).
- The retry log stringifies the exception into a property (`http-api#17`, L-02).
- `IncludeResponseBodyInException` is honoured only per request, while its XML doc claims more
  (`http-api#6`, design A10(b)).

Former core step 20; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.HTTP/RequestBuilder.cs` — EDIT — `:320` catch filter; `:21-22,368-376` XML doc of `IncludeResponseBodyInException`
- `src/SolTechnology.Core.HTTP/ServiceCallErrorMapper.cs` — EDIT — `:29-38` comment and branch; `:129,141` braces, `catch (JsonException)`
- `src/SolTechnology.Core.HTTP/HttpPolicyFactory.cs` — EDIT — `:88` `LoadIntoBufferAsync(args.Context.CancellationToken)` with the 8 KiB cap; `:140-143` `LogWarning(args.Outcome.Exception, …)`; `:24-25` XML doc
- `src/SolTechnology.Core.HTTP/RetryPredicates.cs` — EDIT — `:37,39` token, cap, `catch (JsonException)`
- `src/SolTechnology.Core.HTTP/HttpPolicyConfiguration.cs` — EDIT — `:31-35` XML doc worst-case statement
- `tests/SolTechnology.Core.HTTP.Tests/RequestBuilderTests.cs` — EDIT
- `tests/SolTechnology.Core.HTTP.Tests/RetryPredicateTests.cs` — EDIT

## Changes
- Catch filter: `when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)`.
- Reuse the `RequestBuilder` 8 KiB body cap constant in the retry predicate. Do not add a new public constant.
- Tests:
  - `TryGetAsync_HttpClientTimeout_ReturnsTimeoutError`.
  - `TryGetAsync_CallerCancelled_Throws`.
  - `RetryPredicate_LargeErrorBody_ReadsAtMost8KiB`.

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.HTTP.Tests` is green, and the timeout test fails on `4097d47f`.
- [x] No public or protected signature changed.

## Open questions
- none

## Deviations

### Capped buffering keeps the caller's stream intact

**Original plan:** `HttpPolicyFactory.cs:88` calls `LoadIntoBufferAsync(args.Context.CancellationToken)` with the 8 KiB cap.

**Actual implementation:** A new internal `RequestBuilder.TryBufferCappedBody` calls `LoadIntoBufferAsync(MaxCapturedBodyBytes, token)` and returns `false` on the `HttpRequestException` a larger body raises; the factory then skips the predicate and the transient status decides. A body without a `Content-Length` that overflows is already partly consumed, and Polly returns the last attempt's response whatever `ShouldHandle` says, so the factory also skips buffering and the predicate on the last attempt (`AttemptNumber >= MaxRequestRetries`): only discarded responses can be partly read. `RequestBuilder.MaxCapturedBodyBytes` became `internal`; no public constant was added.

### No cancellation token in `RecoverableOnly`

**Original plan:** `RetryPredicates.cs:37,39` — token, cap, `catch (JsonException)`.

**Actual implementation:** The public predicate type `Func<HttpResponseMessage, ValueTask<bool>>` carries no token, and adding one would change a public signature. `RecoverableOnly` calls the same capped buffering with `CancellationToken.None`, which is a no-op inside the pipeline because the factory has already buffered the body with the request's token. Narrowing the catch to `JsonException` made two guards necessary: a non-object JSON root (`TryGetProperty` throws `InvalidOperationException`) and, in `ServiceCallErrorMapper`, a non-string `title`/`message`/`detail` (`GetString` throws) now return the 1.x results explicitly. `RecoverableOnly_BodyIsNotAJsonObject_AllowsRetry` and `FromStatusCode_BodyWithoutUsableProblemFields_FallsBackToStatus` pin them.

### `FromException` maps every cancellation to `TimeoutError`

**Original plan:** `ServiceCallErrorMapper.cs:29-38` comment and branch.

**Actual implementation:** The `TaskCanceledException` branch became `OperationCanceledException`, matching the new `TrySend` filter, which lets through any cancellation the caller did not request. A bare `OperationCanceledException` passed to the public `FromException` now maps to `TimeoutError` instead of the generic non-recoverable error; the signature is unchanged.

### `IncludeResponseBodyInException` property doc

**Original plan:** XML docs of `IncludeResponseBodyInException` in `RequestBuilder.cs:21-22,368-376`.

**Actual implementation:** The property's own XML doc in `HttpPolicyConfiguration.cs` said "Enable per-client", the claim design A10(b) withdraws, so it now says the flag is honoured per request only. The same file's `RetryPredicate` doc states the 8 KiB cap. The worst-case retry statement in `docs/HTTP-Production-Checklist.md` (`3 × RequestTimeout`) is corrected in step 08 with the other checklist edits.

### Semver footer of the commit

**Original plan:** Commits follow the `commit-message` skill; a log contract change listed as `L-nn` is `Semver: MAJOR` with a `BREAKING CHANGE:` footer, as in the earlier library features.

**Actual implementation:** Commit `f2e575b3` carries `Semver: PATCH` although it ships L-02 (the retry warning's `{Exception}` placeholder is gone; the exception is the log entry's exception). History is not rewritten; this record and the brief's L-02 entry are the source for the release notes.
