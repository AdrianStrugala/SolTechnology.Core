---
adr: 012-production-pattern-adoption-wave-2
step: 01 of 24
status: done
---

# Step 01: A6 — Security-headers middleware (`Core.Api`)

## Summary
Add a tiny ASP.NET Core middleware to `SolTechnology.Core.Api` that stamps a strict baseline set of
security headers on every response, exposed as `app.UseSecurityHeaders()`. Cheap, high-value
hardening for any host. Separate PR because it is a self-contained middleware + options pair with no
dependency on any other wave-2 step.

## Affected components
- `src/SolTechnology.Core.API/Security/SecurityHeadersMiddleware.cs` — new middleware (writes the
  headers on the outbound response).
- `src/SolTechnology.Core.API/Security/SecurityHeadersOptions.cs` — new options class (ships in the
  same step as the middleware that consumes it).
- `src/SolTechnology.Core.API/Security/SecurityHeadersApplicationBuilderExtensions.cs` — new
  `UseSecurityHeaders(this IApplicationBuilder, Action<SecurityHeadersOptions>?)` extension.
- `docs/Api.md` — document the middleware, defaults, and the Swagger/Redoc relaxation knob.
- `tests/SolTechnology.Core.API.Tests/` — header-assertion tests (default + relaxed).

## Details
- Default header set:
  - `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`
  - `X-Content-Type-Options: nosniff`
  - `Referrer-Policy: no-referrer` (or `strict-origin-when-cross-origin` — pick one default and
    document it).
- `SecurityHeadersOptions` exposes a way to **relax the CSP for Swagger/Redoc hosts** (those UIs
  need `style-src`/`script-src`/`img-src` for inline assets). Model this as either a per-path-prefix
  relaxation (e.g. `RelaxedPathPrefixes` defaulting to the Swagger/Redoc route prefixes) or a
  caller-supplied CSP override string. Keep the strict default; relaxation is explicit opt-in.
- Only set a header if it is not already present, so a host that sets its own CSP upstream is not
  clobbered (use `Headers.TryAdd` / check before assignment).
- Middleware is registration-light: no DI service required beyond `IOptions<SecurityHeadersOptions>`
  if options are bound; prefer the `UseSecurityHeaders(Action<...>)` inline-config shape so no
  `AddSecurityHeaders()` is required (matches the lightweight nature of the feature).
- Do **not** wire it into `AddApiCore` automatically — it is a pipeline (`Use…`) concern the host
  opts into, like `UseSwaggerWithVersioning`.

## Acceptance criteria
- `app.UseSecurityHeaders()` adds all three headers with the documented strict defaults on every
  response (including error responses produced by the ProblemDetails pipeline).
- A relaxation path exists and is documented for Swagger/Redoc; the strict CSP remains the default.
- Pre-existing headers set upstream are not overwritten.
- `docs/Api.md` has a "Security headers" subsection with the registration snippet and the defaults
  table.
- Tests assert both the strict default set and the relaxed-path behaviour.

## Open questions
- `Referrer-Policy` default value: `no-referrer` vs `strict-origin-when-cross-origin` — decide and
  document. Recommend `no-referrer` to match the otherwise-strict baseline.


