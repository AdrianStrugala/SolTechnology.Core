## SolTechnology.Core

Model success and failure as values: `Result`, `Result<T>` and a small `Error` hierarchy that every
other `SolTechnology.Core.*` package builds on. No dependencies, no registration.

### Features

- `Result` and `Result<T>` carry either a success (with data) or an `Error`, never both.
- Typed errors (`NotFoundError`, `ValidationError`, `ConflictError`, …) that downstream layers map
  deterministically, for example to HTTP status codes in `SolTechnology.Core.Api`.
- Implicit conversions: return a value or an error straight from a method that returns `Result<T>`.
- Task-returning factories (`SuccessAsTask`, `FailAsTask`) for handlers that complete synchronously.
- `AggregateError` combines several failures into one.
- Polymorphic System.Text.Json serialization with stable discriminators; unknown subtypes fall back
  to their nearest registered ancestor instead of throwing.
- Zero package dependencies.

### Registration

No registration needed. Reference the package and use the types.

```bash
dotnet add package SolTechnology.Core
```

### Configuration

No configuration needed.

### Usage

#### Return a result

Build results through the factories or the implicit conversions; their state is read-only and
`Result`/`Result<T>` have no public constructor.

```csharp
using SolTechnology.Core;
using SolTechnology.Core.Errors;

public record City(string Name);

public class CityCatalog
{
    private readonly Dictionary<string, City> _cities = new() { ["wroclaw"] = new City("Wroclaw") };

    public Result<City> Find(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new ValidationError
            {
                Message = "City name is required.",
                Errors = new Dictionary<string, string[]> { ["name"] = ["Required"] }
            };
        }

        if (!_cities.TryGetValue(name.ToLowerInvariant(), out var city))
        {
            return new NotFoundError { Message = $"City '{name}' was not found." };
        }

        return city;
    }

    public Task<Result<City>> FindAsync(string name) => Task.FromResult(Find(name));

    public Task<Result> Remove(string name) =>
        _cities.Remove(name.ToLowerInvariant())
            ? Result.SuccessAsTask()
            : Result.FailAsTask(new NotFoundError { Message = $"City '{name}' was not found." });
}
```

#### Consume a result

Check `IsSuccess` or `IsFailure`, then read `Data` or `Error`.

```csharp
var result = new CityCatalog().Find("Wroclaw");

if (result.IsFailure)
{
    Console.WriteLine($"Failed: {result.Error!.Message}");
    return;
}

Console.WriteLine($"Found {result.Data!.Name}");
```

#### Errors

Pick the subtype whose meaning matches the failure; `Error` itself is the generic fallback.

| Type | Meaning | HTTP status in `SolTechnology.Core.Api` |
|---|---|---|
| `ValidationError` | Input is invalid; `Errors` maps field name → messages | 400 |
| `UnauthorizedError` | Caller is not authenticated | 401 |
| `ForbiddenError` | Caller is authenticated but not allowed | 403 |
| `NotFoundError` | The requested resource does not exist | 404 |
| `ConflictError` | The request conflicts with current state | 409 |
| `TimeoutError` | A dependency did not answer in time; usually `Recoverable = true` | 500 |
| `DeadlockError` | A database deadlock; usually `Recoverable = true` | 500 |
| `AggregateError` | Several failures; `InnerErrors` lists them | 500 |
| `Error` | Anything else | 500 |

Every error carries `Message`, an optional `Description`, `Recoverable` (retrying may succeed) and an
optional `CorrelationId`. `Error.From(exception)` copies only the exception message, never the
stack trace.

```csharp
var error = new AggregateError("Import failed.",
[
    new NotFoundError { Message = "Row 3: unknown city." },
    new ValidationError { Message = "Row 7: missing name." }
]);

Console.WriteLine(error.Message);      // Import failed. (Row 3: unknown city.) (Row 7: missing name.)
Console.WriteLine(error.InnerErrors.Count); // 2
```

#### JSON

`Result` and `Error` serialize with System.Text.Json; errors carry a `$type` discriminator.

```csharp
using System.Text.Json;

var json = JsonSerializer.Serialize(Result<int>.Fail(new NotFoundError { Message = "Missing" }));
// {"Data":0,"IsSuccess":false,"Error":{"$type":"notFound","Message":"Missing","Description":null,"Recoverable":false,"CorrelationId":null}}

var restored = JsonSerializer.Deserialize<Result<int>>(json)!;
// restored.Error is NotFoundError
```

| Discriminator | Type |
|---|---|
| `error` | `Error` |
| `notFound` | `NotFoundError` |
| `conflict` | `ConflictError` |
| `validation` | `ValidationError` |
| `unauthorized` | `UnauthorizedError` |
| `forbidden` | `ForbiddenError` |
| `aggregate` | `AggregateError` |
| `timeout` | `TimeoutError` |
| `deadlock` | `DeadlockError` |

Your own `Error` subtypes serialize as their nearest registered ancestor (`$type: "error"` for a
direct subclass), so their extra properties are not written. Deserialization throws `JsonException`
when the JSON contradicts itself: a failure without an `Error` (including `{}`) or a success with
one. The `Result`/`Error` JSON contract is
System.Text.Json only: do not send a `Result` inside Newtonsoft-serialized payloads (for example
MessageBus messages or Hangfire job arguments), where the discriminators and read-only state do not
round-trip.

### Testing

`SolTechnology.Core.Testing` ships FluentAssertions-style helpers for results.

```csharp
using SolTechnology.Core.Errors;
using SolTechnology.Core.Testing.Assertions;

[Test]
public void Find_UnknownCity_ReturnsNotFound()
{
    // Arrange
    var catalog = new CityCatalog();

    // Act
    var result = catalog.Find("Atlantis");

    // Assert
    result.ShouldBeFailure<NotFoundError>();
}
```

### Conventions

- Return `Result`/`Result<T>` for expected failures; throw only for bugs and broken invariants.
- Use the most specific `Error` subtype; it decides the HTTP status and retry behaviour downstream.
- Set `Recoverable = true` only when retrying the same call can succeed.
- Keep secrets and personal data out of `Message` and `Description`; they reach API clients and logs.
- Upgrade every `SolTechnology.Core.*` package together; mixed major versions warn with `SOLCORE001`.
