---
name: test-writing
description: Author or extend tests for a SolTechnology.Core module or sample app. Use when implementing behaviour that needs coverage, fixing a `Result.Failure` path without a negative test, or adding a regression test for a bug. Picks the right test project, framework (NUnit for all projects), assertion library, mocking style, and file layout. Never invents a new test project.
---

# Test Writing

Add or extend tests under `tests/` (core libs) or `sample-tale-code-apps/<app>/tests/`.
Encodes the conventions from [`docs/ClaudeCodingGuide.md`](../../../docs/ClaudeCodingGuide.md) §8
and the canonical test stack discovered in `tests/*.csproj`.

## When to use

- A behavioural change in `src/SolTechnology.Core.*` has no test covering it.
- A new `Result.Failure` branch ships without a negative test.
- A bug fix needs a regression test pinned to the failing scenario.
- The `code-review` skill flags missing coverage in §5.
- New module — wire up the test project alongside its first feature.

## When NOT to use

- Pure refactor with zero behavioural change and existing tests still green.
- Doc-only / ADR-only PR.
- Generated code where tests would mirror the generator output.

## Test stack — by location

| Location | Framework | Assertions | Mocks | Data |
|---|---|---|---|---|
| `tests/SolTechnology.Core.<Module>.Tests/` | **NUnit** 4.x | **FluentAssertions** 6.12.x / 7.0 | **NSubstitute** 5.x | **AutoFixture** + `AutoNSubstituteCustomization` |
| `sample-tale-code-apps/DreamTravel/tests/{Unit,Component,EndToEnd}/` | NUnit | FluentAssertions | NSubstitute | AutoFixture |
| `sample-tale-code-apps/TaleCode/tests/` | xUnit *(legacy — do not propagate)* | FluentAssertions | NSubstitute | AutoFixture |

**Default stack is NUnit + NSubstitute + FluentAssertions + AutoFixture.** All test projects use NUnit.
where it already exists; new test projects use NUnit. Tests are the explicit exception to
`ClaudeCodingGuide §15` — FluentAssertions is forbidden in `src/`, mandatory in tests. Same for
NSubstitute over Moq.

## Procedure

### 1. Locate the test project

For a module `src/SolTechnology.Core.<Module>/` the test project is
`tests/SolTechnology.Core.<Module>.Tests/`. Do not create a new project unless the module is
new — and only after confirming with the user (`CLAUDE.md §2` forbidden action: new top-level
folder).

For sample apps, match the existing split:

- DreamTravel: `tests/Unit/<Project>.UnitTests/` mirrors the production folder; component tests
  live in `tests/Component/` using `WebApplicationFactory<Program>` + Testcontainers.
- TaleCode: tests live alongside under `tests/<Project>.Tests/`.

### 2. Pick the test type

| Question | Answer → Type |
|---|---|
| Pure algorithm, domain invariant, or single chapter logic? | Unit |
| Crosses two or more layers (handler + DbContext + HTTP client)? | Component (`WebApplicationFactory` + Testcontainers) |
| Real environment smoke test? | EndToEnd |
| Public NuGet surface of `SolTechnology.Core.<Module>`? | Module test project (NUnit) |

Never write a unit test that mocks `IMediator`, `HttpClient`, `DbContext`, or `IRepository` to
assert "the handler called X" — that is a §8 anti-pattern. Promote to Component instead.

### 3. Name the test class and file

- One test class per production type. File mirrors the production folder:
  `tests/SolTechnology.Core.HTTP.Tests/Pipeline/RetryPolicyTests.cs` ↔
  `src/SolTechnology.Core.HTTP/Pipeline/RetryPolicy.cs`.
- Field for the system under test: `_sut`.
- Dependencies on the fixture frozen via `fixture.Freeze<TDep>()`.

### 4. Name the test method

Pattern: `Method_Scenario_ExpectedOutcome`.

- `Execute_ShouldPopulateContextWithRoadData`
- `Resume_AfterPause_CompletesStory_AndPersistsTerminalState`
- `Send_WhenBrokerThrows_ReturnsResultFailureWithBrokerError`

### 5. Structure the test body

Mandatory `// Arrange`, `// Act`, `// Assert` comments — the **one** place in the codebase where
restating *what* in a comment is required (§8). Skip a phase only when it is genuinely empty.

```csharp
[Test]
public async Task Send_WhenBrokerThrows_ReturnsResultFailure()
{
    // Arrange
    var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
    var broker = fixture.Freeze<IMessageBusBroker>();
    broker.SendAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>())
          .ThrowsAsync(new ServiceBusException("transient"));
    var sut = fixture.Create<MessageBusSender>();

    // Act
    var result = await sut.SendAsync(new TestMessage(), CancellationToken.None);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be("MessageBus.SendFailed");
    await broker.Received(1).SendAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>());
}
```

### 6. Parameterise instead of duplicating

- NUnit: `[TestCase]` / `[TestCaseSource]`.
- xUnit (legacy): `[Theory]` + `[InlineData]` / `[MemberData]`.

Two tests differing only in input → one parameterised test.

### 7. Density beats count

Multiple related assertions inside one `// Assert` block are encouraged. One arrange / one act /
one assert *block*. **A test earns its place only if removing it would let a real regression
through.** Tests that mirror the implementation shape are noise.

### 8. Negative paths

Every `Result.Failure` branch in production code needs a test. If the diff adds a new error
code, the test must assert the exact `Error.Code` string (it is part of the public contract).

### 9. Run the right runner

- Core libs (whole suite): `./.github/runTests.ps1`.
- Single project: `dotnet test tests/SolTechnology.Core.<Module>.Tests/`.
- Sample app: `cd sample-tale-code-apps/<App> && dotnet test`.

## Pre-yield checklist

- [ ] Test lives under `tests/` (core) or `sample-tale-code-apps/<app>/tests/`, never under `src/`.
- [ ] Framework matches the project's existing references (NUnit by default; xUnit only in projects already on it).
- [ ] FluentAssertions + NSubstitute + AutoFixture used; no Moq, no plain `Assert.True(...)`.
- [ ] `_sut` field name, `// Arrange` / `// Act` / `// Assert` comments present.
- [ ] No mocking of `IMediator` / `HttpClient` / `DbContext` / `IRepository` in a unit test —
      if needed, the test was promoted to Component.
- [ ] Every new `Result.Failure` path has a negative test asserting the exact `Error.Code`.
- [ ] Parameterised, not duplicated.
- [ ] `dotnet test <project>` green.

## Constraints

- DO NOT introduce a new assertion or mocking library. The repo is FluentAssertions + NSubstitute.
  A new library is a `CLAUDE.md §2` forbidden action.
- DO NOT mock the world in a unit test to verify call patterns — `code-review` will reject it.
- DO NOT skip `// Arrange` / `// Act` / `// Assert` comments. They are the one mandated
  `restate-the-what` comments per §8.
- DO NOT mirror the implementation in test shape. A test is paid for by the regression it would
  catch.
- DO NOT pin to a higher FluentAssertions / NSubstitute / xUnit version than the project
  already uses without invoking `package-management`.
- DO NOT invent a freehand test layout when this skill is unavailable. STOP and tell the user
  `test-writing` is required (CLAUDE.md §3). Freehand tests are how mock-IMediator anti-patterns
  re-enter the codebase.

