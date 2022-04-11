### Overview

The SolTechnology.Core.Guards library provides functionality needed for code contracts. It is based on fluent api providing checks for the most popular data types.

### Registration

For installing the library, reference **SolTechnology.Core.Guards** nuget package

### Configuration

No configuration is needed.


### Usage

1) Example

```csharp
string competitionWinner = "";
Guards.String(competitionWinner, nameof(competitionWinner)).NotNull().NotEmpty();
```
Invokes NotNull() and NotEmpty() checks on the "competitionWinner" string. If any of those conditions are not met, the ArgumentException is thrown.


2) Supported types

- decimal
- double
- float
- int
- long
- string
