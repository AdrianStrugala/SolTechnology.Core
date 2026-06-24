# SolTechnology.Core.Testing

Foundation NUnit testing helpers shared by every `SolTechnology.Core.*.Testing` companion package and
by consumer test projects. **Reference from test projects only** — never from production assemblies.

> Part of the testing framework defined in
> [ADR-008](adr/008-testing-framework-companions.md). For the testing strategy and how the companion
> packages compose into a component-test harness, see [theQuality.md](theQuality.md).

## What's inside

| Area | Type(s) | Purpose |
|---|---|---|
| Data attributes | `AutoNSubstituteDataAttribute`, `InlineAutoNSubstituteDataAttribute`, `AutoBogusDataAttribute` | AutoFixture-driven NUnit test data with **NSubstitute** auto-faking (Moq is forbidden by the repo anti-stack). |
| Realistic data | `BogusCustomization`, `BogusCustomization<T>` | Opt-in **Bogus** integration: member-aware realistic strings, or a fully-controlled `Faker<T>`. |
| Specimens | `DateOnlyCustomization` | `DateOnly` support. |
| **Result assertions** | `ResultAssertions` (extensions) | `ShouldBeSuccess()`, `ShouldBeSuccess<T>()`, `ShouldBeFailure()`, `ShouldBeFailure<TError>()` — surface `Error.Message`/`Code`/`Type` on assertion failure instead of bare booleans. |
| **Substitutes** | `Ct` | `Ct.Any` — concise alias for `Arg.Any<CancellationToken>()` (cuts visual noise in NSubstitute setups). |
| Eventual consistency | `Retry.UntilConditionMet` | Poll an operation until a condition is met (queues, projections, async handlers). |
| Container lifetime | `TestContainersContext`, `ContainerLifecycleHelper` | Shared docker network, `TESTCONTAINERS_REUSE` reuse policy (Testcontainers-native), restart-if-stopped, and readiness probes (host login / AMQP). |
| Log assertions | `InMemorySinkAssertions` | Query a Serilog `InMemorySink` for emitted messages/levels. |

## Realistic data with Bogus

Bogus complements AutoFixture — AutoFixture builds the object graph and fakes dependencies; Bogus
fills strings with realistic, member-aware values. Three ways to plug it in:

```csharp
// 1. Convenience attribute — realistic strings everywhere in the test parameters.
[Test, AutoBogusData]
public void Creates_customer(Customer customer) { /* customer.Email looks like an email */ }

// 2. Same thing, explicit — compose with other customizations.
[Test, AutoNSubstituteData(typeof(BogusCustomization))]
public void Creates_customer(Customer customer) { }

// 3. Full control for a specific type via a Faker<T>.
var faker = new Faker<Customer>().RuleFor(c => c.Iban, f => f.Finance.Iban());
fixture.Customize(new BogusCustomization<Customer>(faker));
```

For deterministic data, build a seeded `Faker<T>` (`new Faker<T>().UseSeed(1234)`) and pass it to
`BogusCustomization<T>` — this avoids Bogus' process-wide global seed.

## Container reuse — two scopes

**Within a single run:** put a single assembly-level NUnit `[SetUpFixture]` with `[OneTimeSetUp]` that
boots the containers once. Every test class in the assembly shares them for free — no extra machinery
needed.

**Across runs (local iteration speed-up):** set `TESTCONTAINERS_REUSE=true` (env var or `.runsettings`).
`TestContainersContext` then builds the network/containers with Testcontainers' native
`.WithReuse(true)` + stable names, so a re-run finds the still-running container instead of paying the
boot cost again; dispose becomes a no-op. Default is **off** so CI stays hermetic. Ryuk is disabled to
run under Docker Desktop Enhanced Container Isolation. `ContainerLifecycleHelper.EnsureRunningAsync`
restarts a reused container that was stopped externally (e.g. by Docker Desktop) between runs.

## Mocking library

These helpers use **NSubstitute** via `AutoFixture.AutoNSubstitute`. The attribute is named
`AutoNSubstituteDataAttribute` (not `AutoMoqData`) because `Moq` is on the repo anti-stack list.

## Result assertions

Extension methods on `Result` / `Result<T>` that produce **actionable failure messages** when the
assertion fails — showing `Error.Message`, `Error.Type`, and `Recoverable` — instead of a bare
"Expected True but was False".

```csharp
using SolTechnology.Core.Testing.Assertions;

// Success — unwraps Data for further chaining
var data = result.ShouldBeSuccess();
data.Should().Be(expectedValue);

// Non-generic success
result.ShouldBeSuccess();

// Failure — returns Error for further assertions
var error = result.ShouldBeFailure();
error.Message.Should().Contain("not found");

// Typed failure — asserts subtype + returns it strongly typed
var notFound = result.ShouldBeFailure<NotFoundError>();
notFound.Message.Should().Contain("42");
```

## `Ct.Any` — CancellationToken matcher alias

Cuts visual noise when verifying NSubstitute calls that accept a `CancellationToken`:

```csharp
using SolTechnology.Core.Testing.Substitutes;

// Before
dispatcher.Received(1).Dispatch(@event, Arg.Any<CancellationToken>());

// After
dispatcher.Received(1).Dispatch(@event, Ct.Any);
```


