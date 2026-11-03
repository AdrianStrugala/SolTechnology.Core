## SolTechnology.Core.AUID

A 64-bit, human-readable, time-ordered identifier (`Auid`) for entities and events — half the size
of a GUID, with the entity type and creation second visible in the ID itself
(`ORD_20241205123456_012345`).

### Features

- Compact 64-bit value (`long`) instead of a 128-bit GUID; store it as `BIGINT`.
- Human-readable string `COD_YYYYMMDDHHmmss_RANDOM`, formatted culture-invariantly.
- 3-letter code per entity type, derived from a type name, a caller file name, or given explicitly.
- Sortable by creation second within one code.
- Strict parsing: malformed strings, codes outside `AAA`–`ZZZ` and timestamps outside
  2001-01-01 to 2137-02-07 are rejected.
- `System.Text.Json` support for values and dictionary keys, plus a `TypeConverter` for model and
  configuration binding.
- Deterministic tests through a `TimeProvider` overload — no process-wide clock.
- Zero allocations for creation; formatting allocates only the resulting string.

### Registration

No registration or DI needed: reference the package and import the namespace.

```csharp
using SolTechnology.Core.AUID;

var orderId = Auid.New<Order>();
```

### Configuration

No configuration needed.

### Usage

#### Creating IDs

`New<T>()` derives the code from the type name, `New("USR")` takes it explicitly, and `New()` derives
it from the calling source file.

```csharp
using SolTechnology.Core.AUID;

var orderId = Auid.New<Order>();   // ORD_20241205123456_012345
var userId = Auid.New("USR");      // USR_20241205123458_012346
var fileId = Auid.New();           // called from OrderService.cs -> ORD_...
```

The code is the first letter of the name followed by its next consonants (`A`, `E`, `I`, `O`, `U`,
`Y` are skipped), padded with `X`:

| Name | Code |
|---|---|
| `Order`, `OrderService` | `ORD` |
| `User` | `USR` |
| `City` | `CTX` |
| `AI` | `AXX` |

An explicit code must be exactly three uppercase letters `A`–`Z`; anything else throws
`ArgumentException`.

#### Controlling the clock

Pass a `TimeProvider` to take the timestamp from it instead of `TimeProvider.System`.

```csharp
using SolTechnology.Core.AUID;

var clock = TimeProvider.System;   // or any TimeProvider, e.g. FakeTimeProvider in tests
var orderId = Auid.New<Order>(clock);
var userId = Auid.New("USR", clock);
var defaultCodeId = Auid.New(null, clock);   // XXX_...
```

#### Parsing

`Parse` throws `FormatException` for invalid input; `TryParse` returns `false`.

```csharp
using SolTechnology.Core.AUID;

var auid = Auid.Parse("ORD_20241205123456_012345");
if (Auid.TryParse("ORD_20241205123456_012345", null, out var parsed))
{
    Console.WriteLine(parsed.Value);   // raw 64-bit value
}
```

`TryParse` returns `false` when:

| Input | Example |
|---|---|
| Wrong length or separators | `ORD-20241205123456-012345` |
| Code not three uppercase letters | `ord_20241205123456_012345` |
| Timestamp not a valid date | `ORD_20241332123456_012345` |
| Timestamp before 2001-01-01 or after 2137-02-07 06:28:15 UTC | `ORD_21380101000000_000001` |
| Random part above 131071 | `ORD_20241205123456_999999` |

#### Raw 64-bit value

`Auid` converts implicitly to `long` and explicitly from `long`; `Parse(long)` validates the code bits.

```csharp
using SolTechnology.Core.AUID;

long raw = Auid.New("ORD");        // implicit, e.g. for a BIGINT column
var fromColumn = (Auid)raw;        // explicit, no validation
var checkedId = Auid.Parse(raw);   // FormatException when the code bits exceed ZZZ
```

`Value` can be negative: codes from `YGE` to `ZZZ` set the sign bit.

#### JSON and binding

`Auid` serializes as its string form through the built-in `AuidJsonConverter`, also as a
dictionary key.

```csharp
using System.Text.Json;
using SolTechnology.Core.AUID;

var totals = new Dictionary<Auid, decimal> { [Auid.New("ORD")] = 99.90m };
var json = JsonSerializer.Serialize(totals);   // {"ORD_20241205123456_012345":99.90}
var back = JsonSerializer.Deserialize<Dictionary<Auid, decimal>>(json);
```

| Input | `AuidJsonConverter` | `AuidTypeConverter` |
|---|---|---|
| Valid string | `Auid` | `Auid` |
| Invalid or empty string | `JsonException` with the JSON path | `NotSupportedException` naming the value |
| Non-string token | `JsonException` | — |

`Auid.Empty` (value `0`) serializes as `AAA_20010101000000_000000` and round-trips.

#### Uniqueness and ordering

Two AUIDs with the same code and the same second differ only in their 17 random bits (131,072 values).
The chance that a burst of IDs created for one code in one second contains a duplicate is:

| IDs per second, per code | Collision probability |
|---|---|
| 51 | ~1% |
| ~426 | ~50% |

Different codes or different seconds never collide. Above these rates, add a unique constraint
in storage or use a different ID scheme.

Ordering (`CompareTo`, `<`, `>` and sorting by `Value`) follows creation time only for IDs with the
same code; IDs created in the same second are in random order. Across codes the code bits dominate,
and codes from `YGE` to `ZZZ` set the sign bit of the 64-bit value, so they sort before `AAA`.

#### String format

| Part | Length | Content |
|---|---|---|
| Code | 3 | `AAA`–`ZZZ` (15 bits) |
| Separator | 1 | `_` |
| Timestamp | 14 | `yyyyMMddHHmmss` in UTC, Gregorian calendar (32 bits, seconds since 2001-01-01) |
| Separator | 1 | `_` |
| Random | 6 | `000000`–`131071`, zero-padded (17 bits) |

### Testing

Use `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` to pin the timestamp.

```csharp
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;
using SolTechnology.Core.AUID;

public class OrderIdTests
{
    [Test]
    public void New_WithFakeClock_EncodesItsTime()
    {
        // Arrange
        var clock = new FakeTimeProvider(new DateTimeOffset(2024, 12, 5, 12, 34, 56, TimeSpan.Zero));

        // Act
        var orderId = Auid.New("ORD", clock);

        // Assert
        orderId.ToString().Should().StartWith("ORD_20241205123456_");
    }
}
```

### Conventions

- Do give each entity type its own code; uniqueness and ordering hold per code.
- Do store the `long` value in the database and the string form in APIs and logs.
- Do pass a `TimeProvider` in tests instead of waiting for the clock.
- Don't use an AUID as a secret or a security token: the random part comes from `Random.Shared`.
- Don't create more than a few dozen IDs per second for one code without a unique constraint.
- Don't generate AUIDs after 2137-02-07 06:28:15 UTC; `New` throws `InvalidOperationException`.
