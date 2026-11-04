---
spec: 2026-11-02-core-2-0-api-auth
review: skipped (mechanical rename requested by the owner; the rest of the plan was reviewed 2026-11-02 — user, 2026-11-03)
premortem: skipped (step 01 is a mechanical rename requested by the owner; steps 02–11 covered by the core premortem go-with-mitigations 2026-11-02 — user, 2026-11-03)
---

# SolTechnology.Core 2.0 — Api and Auth — Implementation Summary

Tracks temporary implementation state for
[`../2026-11-02-core-2-0-api-auth.md`](../2026-11-02-core-2-0-api-auth.md).

Gate order: steps `01`–`10` → `11` retrospective (both gates skipped by the user, see frontmatter). Step 01 starts after core 02–04 of
[`../2026-11-02-core-2-0-flagship-hardening.md`](../2026-11-02-core-2-0-flagship-hardening.md)
are `done` and after these library features are `completed`: `result`; `logging`; `http`. No step
is `blocked`.

Gate history:

- 2026-11-02: `review` was `done (2026-11-02)`; `premortem` was `skipped (covered by core premortem
  go-with-mitigations 2026-11-02 in 2026-11-02-core-2-0-flagship-hardening — user, 2026-11-02)`.
  That skip still covers steps 02–10 (former 01–09); their `RM-n` mitigations stay verbatim in the
  steps that carry them.
- 2026-11-03 (scope change, user: rename `SolTechnology.Core.Authentication` to
  `SolTechnology.Core.Auth`, `C-28`): step 01 was inserted and former steps 01–10 became 02–11.
  `review` is `pending` again because the plan gained a breaking public API step after review.
  `premortem` is `pending` because step 01 edits a `ModuleInstaller.cs` and the core premortem
  predates `C-28`.
- 2026-11-03: the user skipped both gates for the rename ("skip": a mechanical rename they requested); step 00 was removed.

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 01 | Auth — rename SolTechnology.Core.Authentication to SolTechnology.Core.Auth | [`steps/01-rename-authentication-to-auth.md`](steps/01-rename-authentication-to-auth.md) | done |
| 02 | DI resolution test — Api and Auth | [`steps/02-di-resolution-test.md`](steps/02-di-resolution-test.md) | done |
| 03 | Keep upstream URIs and query strings out of `ProblemDetails` and exception messages | [`steps/03-no-upstream-uri-in-problem-details.md`](steps/03-no-upstream-uri-in-problem-details.md) | done |
| 04 | Auth — constant-time API-key comparison and per-instance logger | [`steps/04-constant-time-api-key-comparison.md`](steps/04-constant-time-api-key-comparison.md) | done |
| 05 | Api — keep 201/202 in `ResultConversionFilter`; real filter registration | [`steps/05-api-result-filter-status-preservation.md`](steps/05-api-result-filter-status-preservation.md) | done |
| 06 | Auth and Api — public surface cleanup | [`steps/06-auth-api-public-surface.md`](steps/06-auth-api-public-surface.md) | done |
| 07 | XML docs shipped — API, Auth | [`steps/07-xml-docs.md`](steps/07-xml-docs.md) | done |
| 08 | Module docs — Api and Auth | [`steps/08-docs-api-auth.md`](steps/08-docs-api-auth.md) | done |
| 09 | Guide conformance — Api and Auth | [`steps/09-conform-api-auth.md`](steps/09-conform-api-auth.md) | done |
| 10 | Log templates — `[{Placeholder}]` in Api (L-03) | [`steps/10-log-template-brackets.md`](steps/10-log-template-brackets.md) | done |
| 11 | Retrospective | [`steps/11-retrospective.md`](steps/11-retrospective.md) | to-do |
