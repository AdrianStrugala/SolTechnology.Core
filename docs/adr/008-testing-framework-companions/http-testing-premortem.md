# Premortem + hardening — `SolTechnology.Core.HTTP.Testing` for large/parallel suites

Companion to step 04 (the `Faker` → `HTTP.Testing` migration). Run after the move, before declaring the
package "production-grade for large apps". Evidence-based on the ported code and the real KYC fork
(`tests/tests-kyc/AiiaKyc.FakeServer`).

## Imagined failure

A team adopts the package for ~800 component tests with NUnit parallelism on. CI flakes: some runs hit
`bind: address already in use :2137`, some return another test's stub (mapping bleed), one team loses a day
to a cryptic `Sequence contains no matching element` after registering two fakes of the same base type.

## Scenarios → disposition

| # | Scenario | Trigger (original) | Sev | Disposition |
|---|---|---|---|---|
| 1 | Fixed port `2137` collides in parallel | `WireMockFixture.Initialize(port=2137)` | H | **Fixed** — default port `0` (dynamic); expose `Url`/`Port`. |
| 2 | `Dispose()` did `Reset()`, never stopped the server → port leak | `WireMockFixture.Dispose` | H | **Fixed** — `Dispose()` now `Stop()`+`Dispose()`; added `Reset()` for between-test. |
| 3 | No `IAsyncDisposable` (inconsistent with `SQLFixture`) | — | L | **Fixed** — implements `IAsyncDisposable`. |
| 4 | `Fake<T>()` used `.First()` on `BaseType` reflection → cryptic throw / wrong stub / depth-1 only | `WireMockStartup.GetFakeApi` | M | **Fixed** — type-safe `OfType<IFakeApiBuilderWithRequest<T>>()`, `Single` semantics, named errors, duplicate detection. |
| 5 | Reflection `WithRequest` (`GetMethod(name)!.Invoke`) → runtime `Ambiguous/TargetInvocation`, no arg checking | `FakeApiBase.WithRequest` | M | **Fixed earlier (step 04 follow-up)** — `WithRequest(Action<TApiClient>)` direct call: full IntelliSense + compile-time arg checks. |
| 6 | Public `IResponseBuilder` methods threw `NotImplementedException` (ProtoBuf, JSON-factory, trailing headers, cert proxy) | `JsonResponseBuilderDecorator` | M | **Fixed** — all delegate to the wrapped builder. |
| 7 | `WithBodyAsJson` default `JsonSerializerOptions` (PascalCase, no enum-as-string) ≠ real client | `JsonResponseBuilderDecorator.WithBodyAsJson` | M | **Accepted (follow-up)** — changing casing is a behavioural change needing consumer review; deferred to a dedicated change. DreamTravel uses raw `WithBody`, so unaffected today. |
| 8 | Admin interface on + console logger hardcoded → noise/cost at scale | `WireMockStartup.Run` | M | **Fixed** — admin off + null logger by default; `Run(WireMockServerSettings)` overload to override. |
| 9 | No way to assert a client actually called | — | M | **Fixed** — `LogEntries` exposed on the fixture (parity with the KYC fork). |
| 10 | Shared `WireMockServer`/`_fakeServices` mutated without sync under parallelism | `WireMockStartup` | M | **Accepted + documented** — one fixture per assembly, arrange on the test thread; recorded in `docs/HTTP.Testing.md`. |

## Code changes (this pass)

- `WireMockFixture.cs` — dynamic-port default; `Url`/`Port`/`LogEntries`; `Reset()` (between-test) split
  from `Dispose()` (stop + free port); `IAsyncDisposable`; guard accessor with a clear "call Initialize
  first" error.
- `WireMockStartup.cs` — `Url`/`Port`; `DefaultSettings` (dynamic port, admin off, null logger) +
  `Run(WireMockServerSettings)` override; type-safe `GetFakeApi<T>` with named not-found / duplicate errors.
- `JsonResponseBuilderDecorator.cs` — every `NotImplementedException` now delegates to the wrapped builder.
- `docs/HTTP.Testing.md` — refreshed usage (dynamic port wiring, direct-call DSL, `Reset` vs `Dispose`,
  parallel-suite notes).
- `DreamTravel/tests/Component/ComponentTestsFixture.cs` + `appsettings.tests.json` — dogfood the dynamic
  port: inject `HTTPClients:Google:BaseAddress = {WireMockFixture.Url}/google/` (same idiom as the SQL
  connection string), removing the hardcoded `:2137`.

## Follow-up: single-parent fake (ergonomics)

`FakeApiBase<TApiClient>` → **`FakeApiBase`** (non-generic). The generic was redundant: a fake had to write
the client interface twice — `: FakeApiBase<IGoogleClient>, IGoogleClient`. The request/response builder
moved into an internal `FakeApiBuilder<TApiClient>` returned by `Fake<T>()`, so the base no longer needs
the type parameter. A fake now reads `: FakeApiBase, IGoogleClient` — interface named **once**. C# can't
collapse it further (a class cannot inherit its own type parameter, and the fake must implement the
interface — that's where the matcher bodies live). `WithRequest`/`WithResponse` keep full IntelliSense and
compile-time argument checks. `Provider` is now `protected internal` so the builder can read it. Accepted
breaking change (pre-1.0, test-only); the one in-repo consumer (DreamTravel `GoogleFakeApi`) was migrated
in the same change. Validated: HTTP.Testing `-c Release` clean; DreamTravel component tests **5/5 pass**.

## Decision

**Go.** All `H` scenarios fixed; `#7`/`#10` accepted with rationale and documented. Validation is
build- and test-based (`dotnet build src/SolTechnology.Core.HTTP.Testing` clean; `dotnet test` on the
DreamTravel component suite → 5/5 passed).

