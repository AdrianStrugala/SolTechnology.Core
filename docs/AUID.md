### Overview

The **SolTechnology.Core.AUID** library provides a high-performance 64-bit unique identifier (AUID - Application Unique ID) designed for scenarios where GUIDs are too large and auto-increment IDs are insufficient.

AUID structure: **[Code: 15 bits] [Timestamp: 32 bits] [Random: 17 bits]** = 64 bits total

- **Code (15 bits)**: 3-letter identifier (AAA-ZZZ) for entity type
- **Timestamp (32 bits)**: Seconds since 2001-01-01 UTC (valid until year 2137)
- **Random (17 bits)**: Collision prevention within same second

### Features

- Zero-allocation design using `Span<T>` and `stackalloc`
- Type-safe with automatic code generation from type names
- Human-readable string format: `CODE_TIMESTAMP_RANDOM` (e.g., `ORD_2B1A3F12_1A2B3`)
- Sortable by creation time
- Compact 64-bit storage (vs 128-bit GUID)
- Built-in type converter for JSON serialization

### Installation

```bash
dotnet add package SolTechnology.Core.AUID
```

### Usage

#### 1. Generate from generic type

```csharp
var orderId = Auid.New<Order>();
// Creates AUID with code "ORD" derived from type name
// Example output: ORD_2B1A3F12_1A2B3
```

#### 2. Generate with explicit code

```csharp
var userId = Auid.New("USR");
// Creates AUID with code "USR"
// Example output: USR_2B1A3F14_3C4D5
```

#### 3. Generate with caller file inference

```csharp
var id = Auid.New();
// Automatically infers code from source file name
// Called from "OrderService.cs" -> generates code "ORS"
```

#### 4. Parse from string

```csharp
var auid = Auid.Parse("ORD_2B1A3F12_1A2B3");
if (Auid.TryParse("ORD_2B1A3F12_1A2B3", null, out var result))
{
    Console.WriteLine(result.Value); // Access raw 64-bit value
}
```

#### 5. Parse from long

```csharp
long rawValue = 123456789L;
var auid = Auid.Parse(rawValue);
```

### Code Generation Rules

When using `Auid.New<T>()` or `Auid.New()`, the library generates a 3-letter code:

1. First character: First letter of name (uppercase)
2. Next characters: Next consonants from name (skip vowels)
3. Padding: Fill with 'X' if needed

Examples:
- `Order` -> `ORD`
- `User` -> `USR`
- `City` -> `CTY`
- `AI` -> `AXX` (padded)

### Validation

- Code must be exactly 3 uppercase letters (A-Z)
- Maximum date: **February 7, 2137** (32-bit timestamp limit)
- Attempting to generate AUID after year 2137 throws `InvalidOperationException`

### Performance

- Zero allocations for creation and formatting (uses `Span<T>`)
- Aggressive inlining for hot paths
- Fast parsing with zero-allocation span operations

### String Format

Format: `CODE_TIMESTAMP_RANDOM`
- Code: 3 uppercase letters
- Timestamp: 8 hex digits
- Random: 5 hex digits
- Total length: 18 characters

Example: `ORD_2B1A3F12_1A2B3`

### Use Cases

- Database primary keys (more compact than GUID)
- Distributed ID generation without coordination
- Sortable IDs by creation time
- Human-readable entity identification
- API response identifiers

### Limitations

- Maximum date: Year 2137 (32-bit timestamp overflow)
- Clock skew may affect ordering across servers
- Not cryptographically secure (uses `Random.Shared`)
- Limited to 17,576 unique codes (26³)
