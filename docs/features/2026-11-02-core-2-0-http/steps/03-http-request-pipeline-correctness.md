---
spec: 2026-11-02-core-2-0-http
step: 03
status: to-do
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
- [ ] `dotnet test tests/SolTechnology.Core.HTTP.Tests` is green, and the timeout test fails on `4097d47f`.
- [ ] No public or protected signature changed.

## Open questions
- none

## Deviations
