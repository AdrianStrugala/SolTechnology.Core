---
spec: 2026-11-02-core-2-0-api-auth
step: 02
status: to-do
---

# Step 02: Keep upstream URIs and query strings out of `ProblemDetails` and exception messages

## Summary
`ApiProblemDetailsFactory` copies `exception.Message` into `ProblemDetails.Title`
(`src/SolTechnology.Core.API/Exceptions/ApiProblemDetailsFactory.cs:53`). For
`HttpRequestFailedException` that message contains the full upstream URI including the query
(`src/SolTechnology.Core.HTTP/HttpRequestFailedException.cs:20`). DreamTravel puts `&key=…` for
Google in its query strings, so the key reaches API clients and logs (`S4`, `http-api#2`, CWE-209).
Both sides are fixed in one PR because they close one disclosure path.

Former core step 06; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `src/SolTechnology.Core.API/Exceptions/ApiProblemDetailsFactory.cs` — EDIT — fixed titles for 5xx
- `src/SolTechnology.Core.API/Exceptions/DefaultExceptionStatusCodeMapper.cs` — EDIT — only if the title source lives at `:65-72`
- `src/SolTechnology.Core.HTTP/HttpRequestFailedException.cs` — EDIT — `Message` uses scheme + host + path only
- `tests/SolTechnology.Core.API.Tests/` — EDIT — ProblemDetails title tests
- `tests/SolTechnology.Core.HTTP.Tests/` — EDIT — exception message test

## Changes
- `ApiProblemDetailsFactory`: for status ≥ 500 use a fixed title per status (`502` "Upstream dependency failed", `504` "Upstream dependency timed out", other 5xx "An unexpected error occurred"). Keep `exception.Message` only for 4xx. The exception text is still added to `Extensions["exception"]` when `ApiExceptionOptions.IncludeExceptionDetails` is on.
- `HttpRequestFailedException`: the message renders `requestUri.GetLeftPart(UriPartial.Path)`. `RequestUri` keeps the full value for diagnostics in code only.
- Tests:
  - `Create_HttpRequestFailed502_TitleHasNoUri`
  - `Create_ValidationException_TitleIsMessage`
  - `HttpRequestFailedException_Message_OmitsQuery` (input `https://h/p?key=SECRET` → the message has no `SECRET`).

## Acceptance criteria
- [ ] The new tests are green, and `grep -rn "SECRET"` over the test output finds nothing.
- [ ] `dotnet test tests/SolTechnology.Core.API.Tests` and `dotnet test tests/SolTechnology.Core.HTTP.Tests` are green.
- [ ] No public or protected signature changed (P-09 is behavioural only).

## Open questions
- none

## Deviations
