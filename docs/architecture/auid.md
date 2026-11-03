# AUID

`SolTechnology.Core.AUID` ships `Auid`, a 64-bit identifier with a readable string form, and its
`AuidJsonConverter` and `AuidTypeConverter`. All three live in the `SolTechnology.Core.AUID`
namespace and have no dependencies beyond the base class library. The Tale framework uses `Auid`
for tale instance IDs. Consumer usage lives in [`docs/AUID.md`](../AUID.md).

## Layout

| Bits | Part | Range |
|---|---|---|
| 63–49 | Code, three letters `A`–`Z` in base 26 | `AAA` (0) to `ZZZ` (17,575) |
| 48–17 | Seconds since 2001-01-01 UTC | up to 2137-02-07 06:28:15 UTC |
| 16–0 | Random from `Random.Shared` | 0 to 131,071 |

The string form `COD_YYYYMMDDHHmmss_RANDOM` is always 25 characters. The 64-bit value fits a
`BIGINT` column and an index half the size of a GUID's; the string form names the entity type and
the creation second without a lookup. `Auid.Empty` is value 0 and formats as
`AAA_20010101000000_000000`.

## Creation and time

`Auid.New` takes the timestamp from `TimeProvider.System`; the overloads
`New(string? code, TimeProvider)` and `New<T>(TimeProvider)` take it from the caller. There is no
process-wide clock: one library cannot change the time another library sees, and parallel tests
pass their own `FakeTimeProvider`. A null code with an explicit `TimeProvider` falls back to `XXX`,
because that overload has no caller-file parameter to infer a code from.

The timestamp is `GetUtcNow().UtcTicks`, so a `TimeProvider` that reports a non-zero offset still
encodes the UTC instant and the range checks do not depend on the offset. A time after the 32-bit
limit throws `InvalidOperationException`; its Polish message is a deliberate joke and stays (Coding
Guide §9.10). A time before the 2001-01-01 epoch (for example a default `FakeTimeProvider`, which
starts at 2000-01-01) throws `InvalidOperationException` with a separate English message instead of
wrapping into the time bits.

## Formatting and parsing

`ToString` formats with `CultureInfo.InvariantCulture`, so the year is Gregorian under every
current culture. 1.x formatted with the current culture's calendar, so hosts running under, for
example, `th-TH` (Thai Buddhist) or `ar-SA` (Umm al-Qura) wrote year-shifted strings. Those strings
fall outside the valid time range and 2.0 rejects them, so records that store them must be
migrated before they can be read.

`TryParse(string?, …)` returns `false` for a wrong length or separator, a code outside `A`–`Z`,
an invalid date, a time outside the 32-bit range, or a random part above 131,071. The range check
matters because an out-of-range time would otherwise shift into the code bits and produce a
different, valid-looking ID. `TryParse(long, …)` accepts any value whose code bits are at most
`ZZZ`; the explicit `(Auid)long` cast does not validate.

## Converters

| Input | `AuidJsonConverter` | `AuidTypeConverter.ConvertFrom` |
|---|---|---|
| Valid string | `Auid` | `Auid` |
| Invalid or empty string | `JsonException`; System.Text.Json adds the JSON path | `FormatException` naming the value |
| Non-string token | `JsonException` | base `TypeConverter` behaviour |

`AuidTypeConverter` throws `FormatException`, as 1.x did through `Auid.Parse`: MVC model binding
turns only `FormatException` and `OverflowException` into "The value '…' is not valid…"; any
other exception becomes a generic invalid-input error. Both converters are `sealed`. `AuidJsonConverter` also implements `ReadAsPropertyName` and
`WriteAsPropertyName`, so `Dictionary<Auid, T>` serializes with string keys. An empty string is an
error rather than `Auid.Empty`, because the converter writes `Auid.Empty` as
`AAA_20010101000000_000000` and an empty value therefore signals bad input. Persisted Tale
contexts store `Auid` as this string, not as a type name, so moving the type into a namespace left
stored JSON readable.

## Uniqueness and ordering

IDs with different codes or different seconds never collide. Within one code and one second only
the 17 random bits differ, so a burst of `n` IDs collides with probability about
`1 - exp(-n² / 262,144)`: about 1% at 51 IDs and 50% at about 426 IDs. The library does not add a
per-process counter; consumers above those rates need a unique constraint or another scheme.

`CompareTo` and the equality members use the signed 64-bit `Value`. Within one code the order is
by creation second, with random order inside a second. Across codes the code bits dominate, and
codes from `YGE` (16,384) upward set the sign bit, so they sort before `AAA`.

## Source layout

`Auid` is a `readonly partial struct` split by concern: layout and equality in `Auid.cs`,
factories in `Auid.New.cs`, code and value encoding in `Auid.Encoding.cs`, `Auid.Format.cs`, and
`Auid.Parse.cs`. Creation and formatting allocate nothing beyond the result string. The package
ships its XML documentation.
