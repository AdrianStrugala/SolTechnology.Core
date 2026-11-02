---
spec: 2026-11-02-core-2-0-flagship-hardening
review: done (2026-11-02)
premortem: go-with-mitigations (2026-11-02)
---

# SolTechnology.Core 2.0 — flagship hardening — Implementation Summary

Tracks temporary implementation state for
[`../2026-11-02-core-2-0-flagship-hardening.md`](../2026-11-02-core-2-0-flagship-hardening.md).

Gate order: `review` (plan-reviewer) → step `00` premortem → steps `01`–`70` → `71` retrospective.
The user answered `Q1`–`Q10`, confirmed `C-01`–`C-24`, and rejected `C-25` on 2026-11-02, then
answered the plan-review questions `RQ1`–`RQ12` and confirmed the additive `C-26`/`C-27` the same
day (see the feature brief). No step is `blocked`. Former steps 74 (stats bot off `master`), 76
(delete Guards and Scheduler), and 81 (DreamTravel `UseNuGet` switch) were removed before review.

Plan-review findings applied on 2026-11-02. On the same day the feature was split (RQ8, user):
post-review steps 71–77 moved to
[`../2026-11-02-flagship-showcase-hygiene.md`](../2026-11-02-flagship-showcase-hygiene.md) and
78–82 to
[`../2026-11-02-org-transfer-and-2-0-release.md`](../2026-11-02-org-transfer-and-2-0-release.md);
the retrospective is now step 71. The completion review of the split plan finished on 2026-11-02.

## Steps

| # | Title | File | Status |
|---|---|---|---|
| 00 | Run premortem (gate) | [`steps/00-run-premortem.md`](steps/00-run-premortem.md) | done |
| 01 | Clear NU1903 — Testcontainers and DreamTravel parent bumps, Pkcs override in SQL | [`steps/01-clear-nu1903-at-source.md`](steps/01-clear-nu1903-at-source.md) | done |
| 02 | Fail CI on any test-project failure | [`steps/02-fail-ci-on-any-test-failure.md`](steps/02-fail-ci-on-any-test-failure.md) | to-do |
| 03 | Publish to nuget.org only from `v*` tags behind a protected environment | [`steps/03-publish-only-from-tags.md`](steps/03-publish-only-from-tags.md) | to-do |
| 04 | Bump every package to 2.0.0 | [`steps/04-bump-version-2-0-0.md`](steps/04-bump-version-2-0-0.md) | to-do |
| 05 | HTTP — never retry unsafe verbs after an exception or timeout | [`steps/05-http-no-unsafe-verb-retry-on-exception.md`](steps/05-http-no-unsafe-verb-retry-on-exception.md) | to-do |
| 06 | Keep upstream URIs and query strings out of `ProblemDetails` and exception messages | [`steps/06-no-upstream-uri-in-problem-details.md`](steps/06-no-upstream-uri-in-problem-details.md) | to-do |
| 07 | Authentication — constant-time API-key comparison and per-instance logger | [`steps/07-constant-time-api-key-comparison.md`](steps/07-constant-time-api-key-comparison.md) | to-do |
| 08 | DreamTravel — Development-only developer exception page | [`steps/08-dreamtravel-dev-only-exception-page-and-credentials.md`](steps/08-dreamtravel-dev-only-exception-page-and-credentials.md) | to-do |
| 09 | Remove leaked-secret artifacts and personal settings; record revocation | [`steps/09-remove-leaked-secret-artifacts.md`](steps/09-remove-leaked-secret-artifacts.md) | to-do |
| 10 | DI resolution tests — application modules | [`steps/10-di-resolution-tests-application-modules.md`](steps/10-di-resolution-tests-application-modules.md) | to-do |
| 11 | DI resolution tests — infrastructure modules | [`steps/11-di-resolution-tests-infrastructure-modules.md`](steps/11-di-resolution-tests-infrastructure-modules.md) | to-do |
| 12 | MessageBus — resolvable broker, registered health-check client, correlation service | [`steps/12-messagebus-resolvable-broker-and-health-check.md`](steps/12-messagebus-resolvable-broker-and-health-check.md) | to-do |
| 13 | Hangfire — implement `preventOverlap`; register the correlation service | [`steps/13-hangfire-prevent-overlap-and-correlation.md`](steps/13-hangfire-prevent-overlap-and-correlation.md) | to-do |
| 14 | Logging — `AddSolLogging(IConfiguration)` registers its middleware dependencies | [`steps/14-logging-configuration-overload-registrations.md`](steps/14-logging-configuration-overload-registrations.md) | to-do |
| 15 | SQL — `Begin` on `IUnitOfWork` | [`steps/15-sql-unit-of-work-begin-on-interface.md`](steps/15-sql-unit-of-work-begin-on-interface.md) | to-do |
| 16 | Blob — thread-safe container cache with async creation | [`steps/16-blob-thread-safe-connection-factory.md`](steps/16-blob-thread-safe-connection-factory.md) | to-do |
| 17 | Core — open `Error` polymorphism; `AggregateError` round-trip | [`steps/17-core-open-error-polymorphism.md`](steps/17-core-open-error-polymorphism.md) | to-do |
| 18 | Tale — per-run state isolation; persist only `TaleManager`-driven runs | [`steps/18-tale-per-run-state-and-scoped-persistence.md`](steps/18-tale-per-run-state-and-scoped-persistence.md) | to-do |
| 19 | AUID — culture-invariant formatting, range-checked parsing, English overflow message | [`steps/19-auid-invariant-format-and-range-checked-parse.md`](steps/19-auid-invariant-format-and-range-checked-parse.md) | to-do |
| 20 | HTTP — request pipeline correctness | [`steps/20-http-request-pipeline-correctness.md`](steps/20-http-request-pipeline-correctness.md) | to-do |
| 21 | HTTP — upstream health-check disposal and options validation | [`steps/21-http-health-check-and-options-validation.md`](steps/21-http-health-check-and-options-validation.md) | to-do |
| 22 | Api — keep 201/202 in `ResultConversionFilter`; real filter registration | [`steps/22-api-result-filter-status-preservation.md`](steps/22-api-result-filter-status-preservation.md) | to-do |
| 23 | CQRS — descriptive missing-handler errors and interface-based dispatch | [`steps/23-cqrs-dispatch-correctness.md`](steps/23-cqrs-dispatch-correctness.md) | to-do |
| 24 | CQRS — log failed `Result`s as failures | [`steps/24-cqrs-log-failed-results.md`](steps/24-cqrs-log-failed-results.md) | to-do |
| 25 | Tale engine — caller-only cancellation, no resume of failed tales, deterministic tests | [`steps/25-tale-engine-cancellation-and-resume.md`](steps/25-tale-engine-cancellation-and-resume.md) | to-do |
| 26 | Tale — registration and schema safety, correct log scope | [`steps/26-tale-registration-and-schema-safety.md`](steps/26-tale-registration-and-schema-safety.md) | to-do |
| 27 | Tale controller — consistent JSON options, no internal detail in responses | [`steps/27-tale-controller-hardening.md`](steps/27-tale-controller-hardening.md) | to-do |
| 28 | Cache — local cache fault eviction, single flight, and a correct local lock | [`steps/28-cache-local-cache-and-lock-correctness.md`](steps/28-cache-local-cache-and-lock-correctness.md) | to-do |
| 29 | Cache — type-namespaced keys, validated non-empty `InstanceName`, validated options | [`steps/29-cache-key-isolation-and-config-validation.md`](steps/29-cache-key-isolation-and-config-validation.md) | to-do |
| 30 | Logging — segment-aware path matching; one Error entry per failure | [`steps/30-logging-middleware-path-and-error-entries.md`](steps/30-logging-middleware-path-and-error-entries.md) | to-do |
| 31 | Logging — sanitize inbound correlation IDs; safe timings and masking | [`steps/31-logging-correlation-and-masking-input.md`](steps/31-logging-correlation-and-masking-input.md) | to-do |
| 32 | Hangfire — restore ambient correlation state; async, cancellable event dispatch | [`steps/32-hangfire-execution-context.md`](steps/32-hangfire-execution-context.md) | to-do |
| 33 | SQL — transient-only connection retry, safer error translation, options validation | [`steps/33-sql-connection-retry-and-validation.md`](steps/33-sql-connection-retry-and-validation.md) | to-do |
| 34 | MessageBus — receiver settlement matches its contract; publish→receive round-trip | [`steps/34-messagebus-receiver-settlement.md`](steps/34-messagebus-receiver-settlement.md) | to-do |
| 35 | AUID — standards-compliant converters; honest uniqueness and ordering docs | [`steps/35-auid-converters-and-docs.md`](steps/35-auid-converters-and-docs.md) | to-do |
| 36 | API and HTTP testing kits — dispose the test host; strict WireMock by default | [`steps/36-http-api-testing-kit-fixes.md`](steps/36-http-api-testing-kit-fixes.md) | to-do |
| 37 | Container fixtures — per-instance state; honour Ryuk settings | [`steps/37-container-fixture-lifecycle.md`](steps/37-container-fixture-lifecycle.md) | to-do |
| 38 | Core — immutable `Result` and a typed `Result<T>.FailAsTask` | [`steps/38-core-result-immutability.md`](steps/38-core-result-immutability.md) | to-do |
| 39 | Tale — `CancellationToken` on chapters | [`steps/39-tale-chapter-cancellation-token.md`](steps/39-tale-chapter-cancellation-token.md) | to-do |
| 40 | Tale — `ITaleRepository` 2.0 signatures (cancellation and names) | [`steps/40-tale-repository-cancellation-and-names.md`](steps/40-tale-repository-cancellation-and-names.md) | to-do |
| 41 | Tale — optimistic concurrency on `ITaleRepository.Save` | [`steps/41-tale-repository-optimistic-concurrency.md`](steps/41-tale-repository-optimistic-concurrency.md) | to-do |
| 42 | Tale — chapter history keyed by step index and full type name | [`steps/42-tale-chapter-history-key.md`](steps/42-tale-chapter-history-key.md) | to-do |
| 43 | Tale — explicit HTTP exposure; internal plumbing types | [`steps/43-tale-http-exposure-opt-in.md`](steps/43-tale-http-exposure-opt-in.md) | to-do |
| 44 | AUID — `SolTechnology.Core.AUID` namespace; no process-wide `TimeProvider` | [`steps/44-auid-namespace-and-time-provider.md`](steps/44-auid-namespace-and-time-provider.md) | to-do |
| 45 | HTTP — own namespace for `HttpClientExtensions`; factory-based upstream health check | [`steps/45-http-namespace-and-health-check-factory.md`](steps/45-http-namespace-and-health-check-factory.md) | to-do |
| 46 | HTTP — drop `Async` from `RequestBuilder` verbs (optional) | [`steps/46-http-request-builder-async-suffix.md`](steps/46-http-request-builder-async-suffix.md) | to-do |
| 47 | Cache — drop `Async` from lock and idempotency contracts | [`steps/47-cache-async-suffix.md`](steps/47-cache-async-suffix.md) | to-do |
| 48 | MessageBus — internal implementation types | [`steps/48-messagebus-internal-implementations.md`](steps/48-messagebus-internal-implementations.md) | to-do |
| 49 | SQL — internal implementations; safe-by-default DACPAC deployment | [`steps/49-sql-internal-implementations-and-safe-dacpac.md`](steps/49-sql-internal-implementations-and-safe-dacpac.md) | to-do |
| 50 | Blob — 2.0 surface (own namespace, cancellation, atomic metadata, internal factory) | [`steps/50-blob-2-0-surface.md`](steps/50-blob-2-0-surface.md) | to-do |
| 51 | Hangfire — recurring job contract for 2.0 | [`steps/51-hangfire-job-contracts.md`](steps/51-hangfire-job-contracts.md) | to-do |
| 52 | Logging, Authentication, and Api — public surface cleanup | [`steps/52-logging-auth-api-public-surface.md`](steps/52-logging-auth-api-public-surface.md) | to-do |
| 53 | Testing kits — 2.0 surface (optional `Async` renames, WireMock fields) | [`steps/53-testing-kit-surface.md`](steps/53-testing-kit-surface.md) | to-do |
| 54 | Packaging — a truly zero-dependency `SolTechnology.Core` | [`steps/54-zero-dependency-base-package.md`](steps/54-zero-dependency-base-package.md) | to-do |
| 55 | XML docs shipped — Core, CQRS, Tale, AUID | [`steps/55-xml-docs-core-cqrs-tale-auid.md`](steps/55-xml-docs-core-cqrs-tale-auid.md) | to-do |
| 56 | XML docs shipped — HTTP, HTTP.Testing, Api, API.Testing, Authentication | [`steps/56-xml-docs-http-api-auth.md`](steps/56-xml-docs-http-api-auth.md) | to-do |
| 57 | XML docs shipped — Cache, MessageBus, SQL, Blob, Hangfire | [`steps/57-xml-docs-cache-bus-sql-blob-hangfire.md`](steps/57-xml-docs-cache-bus-sql-blob-hangfire.md) | to-do |
| 58 | XML docs shipped — testing kits, then one central switch | [`steps/58-xml-docs-testing-kits-and-central-switch.md`](steps/58-xml-docs-testing-kits-and-central-switch.md) | to-do |
| 59 | Module docs — Api, Clients, HTTP checklist, API/HTTP testing, Auth | [`steps/59-docs-api-clients-http-auth.md`](steps/59-docs-api-clients-http-auth.md) | to-do |
| 60 | Module docs — Core, CQRS, Tale, AUID, Cache, Log, Redis.Testing | [`steps/60-docs-core-cqrs-tale-auid-cache-log.md`](steps/60-docs-core-cqrs-tale-auid-cache-log.md) | to-do |
| 61 | Module docs — Bus, Hangfire, SQL, Blob, Testing kits | [`steps/61-docs-bus-hangfire-sql-blob-testing.md`](steps/61-docs-bus-hangfire-sql-blob-testing.md) | to-do |
| 62 | Guide conformance — Tale engine split | [`steps/62-conform-tale-engine.md`](steps/62-conform-tale-engine.md) | to-do |
| 63 | Guide conformance — Tale controller, manager, handler, `Tale`, and tests | [`steps/63-conform-tale-controller-manager-handler.md`](steps/63-conform-tale-controller-manager-handler.md) | to-do |
| 64 | Guide conformance — HTTP | [`steps/64-conform-http.md`](steps/64-conform-http.md) | to-do |
| 65 | Guide conformance — Api, Authentication file names, HTTP.Testing | [`steps/65-conform-api-auth-http-testing.md`](steps/65-conform-api-auth-http-testing.md) | to-do |
| 66 | Guide conformance — CQRS, Core, Authentication | [`steps/66-conform-cqrs-core-auth.md`](steps/66-conform-cqrs-core-auth.md) | to-do |
| 67 | Guide conformance — Logging, Cache, AUID | [`steps/67-conform-logging-cache-auid.md`](steps/67-conform-logging-cache-auid.md) | to-do |
| 68 | Guide conformance — MessageBus and ServiceBus.Testing | [`steps/68-conform-messagebus-servicebus-testing.md`](steps/68-conform-messagebus-servicebus-testing.md) | to-do |
| 69 | Guide conformance — SQL, Blob, Hangfire, and the remaining testing kits | [`steps/69-conform-sql-blob-hangfire-testing.md`](steps/69-conform-sql-blob-hangfire-testing.md) | to-do |
| 70 | Log templates — `[{Placeholder}]` everywhere (log contract change L-03) | [`steps/70-log-template-brackets.md`](steps/70-log-template-brackets.md) | to-do |
| 71 | Retrospective | [`steps/71-retrospective.md`](steps/71-retrospective.md) | to-do |
