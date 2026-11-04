---
spec: 2026-11-02-core-2-0-api-auth
step: 03
status: done
---

# Step 03: Keep upstream URIs and query strings out of `ProblemDetails` and exception messages

## Summary
`ApiProblemDetailsFactory` copies `exception.Message` into `ProblemDetails.Title`
(`src/SolTechnology.Core.API/Exceptions/ApiProblemDetailsFactory.cs:53`). For
`HttpRequestFailedException` that message contains the full upstream URI including the query
(`src/SolTechnology.Core.HTTP/HttpRequestFailedException.cs:20`). DreamTravel puts `&key=…` for
Google in its query strings, so the key reaches API clients and logs (`S4`, `http-api#2`, CWE-209).
Both sides are fixed in one PR because they close one disclosure path.

Former core step 06; moved here by the per-library regroup (user, 2026-11-02). Renumbered from 02 on 2026-11-03 (new step 01, `C-28`).

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
- [x] The new tests are green, and `grep -rn "SECRET"` over the test output finds nothing.
- [x] `dotnet test tests/SolTechnology.Core.API.Tests` and `dotnet test tests/SolTechnology.Core.HTTP.Tests` are green.
- [x] No public or protected signature changed (P-09 is behavioural only).

## Open questions
- none

## Deviations

### The message drops user info and works for relative URIs

**Original plan:** the message renders `requestUri.GetLeftPart(UriPartial.Path)`.

**Actual implementation:** `GetLeftPart(UriPartial.Path)` keeps `user:password@` and throws for a relative URI, which
`RequestUri` allows. A private `WithoutQuery` helper renders `GetComponents(SchemeAndServer | Path)` for absolute URIs
and the part before `?` or `#` for relative ones. `HttpRequestFailedException_Message_OmitsQuery` covers the planned
input plus user info with a fragment and a relative URI; its cases carry a `TestName`, so `SECRET` does not appear in
the detailed test output (0 hits in `dotnet test --logger "console;verbosity=detailed"` for both projects).

### Test shapes for the ProblemDetails titles

**Original plan:** `Create_HttpRequestFailed502_TitleHasNoUri` and `Create_ValidationException_TitleIsMessage`.

**Actual implementation:** the factory is internal, so the new `ApiProblemDetailsFactoryTests` drive it through the
public `ExceptionFilter` with the real `DefaultExceptionStatusCodeMapper`; `SolTechnology.Core.API.Tests` now references
`SolTechnology.Core.HTTP` to throw a real `HttpRequestFailedException`. `Create_HttpRequestFailed502_TitleHasNoUri` also
covers 504 and checks that the opt-in `Extensions["exception"].message` has no query; a new
`Create_Other5xx_TitleIsGeneric` covers 500 and 501. A FluentValidation `ValidationException` already had the fixed
title "One or more validation errors occurred." (unchanged), so `Create_ValidationException_TitleIsMessage` uses the other
400 input, an `ArgumentException`. The 502/504/5xx cases failed before the fix and pass; the 400 case is a regression
guard and passed before and after. `docs/Api.md`'s diagnostic-detail example now shows the fixed 500 title; step 08
rewrites the rest of the file.

