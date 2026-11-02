---
spec: 2026-11-02-core-2-0-http
step: 02
status: to-do
---

# Step 02: HTTP — never retry unsafe verbs after an exception or timeout

## Summary
`HttpPolicyFactory.ShouldRetry` reads the HTTP method from `outcome.Result?.RequestMessage`
(`src/SolTechnology.Core.HTTP/HttpPolicyFactory.cs:69`). When the outcome is an exception
(`HttpRequestException`, `TimeoutRejectedException`, `TaskCanceledException`), `Result` is null,
so a POST or PATCH is retried. That is the duplicate-charge case the default exists to prevent
(`S3`, `http-api#1`). The method must come from the request stored in the resilience context.

Former core step 05; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.HTTP/HttpPolicyFactory.cs` — EDIT — `ShouldRetry` and `IsUnsafeVerb` (`:66-71`, `:194-197`)
- `tests/SolTechnology.Core.HTTP.Tests/HttpPolicyFactoryTests.cs` — EDIT — exception and timeout cases

## Changes
- Resolve the method from `args.Context.GetRequestMessage()` (`Microsoft.Extensions.Http.Resilience`). Fall back to `outcome.Result?.RequestMessage?.Method` only when the context has no request.
- Keep the breaker predicate unchanged: failed POSTs still trip the breaker (`HttpPolicyFactory.cs:66`).
- Tests (`[TestCase]` over `POST`, `PATCH` → no retry; `GET`, `PUT` → retry):
  - `Retry_UnsafeVerb_HttpRequestException_IsNotRetried`
  - `Retry_UnsafeVerb_Timeout_IsNotRetried`
  - `Retry_UnsafeVerb_WithRetryOnUnsafeVerbsEnabled_IsRetried`

## Acceptance criteria
- [ ] The new tests fail on `4097d47f` and pass after the change.
- [ ] `dotnet test tests/SolTechnology.Core.HTTP.Tests` is green.
- [ ] No public or protected signature changed.

## Open questions
- none

## Deviations
