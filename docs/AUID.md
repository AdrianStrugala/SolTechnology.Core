### Overview

The **SolTechnology.Core.AUID** library provides a high-performance 64-bit unique identifier (AUID - Application Unique ID) designed for scenarios where GUIDs are too large and auto-increment IDs are insufficient.

AUID structure: **[Code: 15 bits] [Timestamp: 32 bits] [Random: 17 bits]** = 64 bits total

- **Code (15 bits)**: 3-letter identifier (AAA-ZZZ) for entity type
- **Timestamp (32 bits)**: Seconds since 2001-01-01 UTC (valid until year 2137)
- **Random (17 bits)**: Collision prevention within same second (0-131071)

### Features

- Zero-allocation design using `Span<T>` and `stackalloc`
- Type-safe with automatic code generation from type names
- Human-readable string format: `COD_YYYYMMDDHHmmss_RANDOM` (e.g., `ORD_20241205123456_012345`)
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
// Example output: ORD_20241205123456_012345
```

#### 2. Generate with explicit code

```csharp
var userId = Auid.New("USR");
// Creates AUID with code "USR"
// Example output: USR_20241205123458_012346
```

#### 3. Generate with caller file inference

```csharp
var id = Auid.New();
// Automatically infers code from source file name
// Called from "OrderService.cs" -> generates code "ORS"
```

#### 4. Parse from string

```csharp
var auid = Auid.Parse("ORD_20241205123456_012345");
if (Auid.TryParse("ORD_20241205123456_012345", null, out var result))
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
- `City` -> `CTX`
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

Format: `COD_YYYYMMDDHHmmss_RANDOM`
- Code: 3 uppercase letters (AAA-ZZZ)
- Timestamp: 14 digits representing date/time (YYYYMMDDHHmmss in UTC)
- Random: 6 decimal digits (000000-131071, zero-padded)
- Total length: 25 characters

Example: `ORD_20241205123456_012345`
- `ORD` = Order entity type
- `20241205123456` = December 5, 2024 at 12:34:56 UTC
- `012345` = Random collision prevention number

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
