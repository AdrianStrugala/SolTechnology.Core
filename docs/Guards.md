### Overview

The SolTechnology.Core.Guards library provides functionality needed for code contracts. It is based on fluent api providing checks for the most popular data types.

### Registration

For installing the library, reference **SolTechnology.Core.Guards** nuget package

### Configuration

No configuration is needed.


### Usage

1) Example

```csharp
var guards = new Guards();
guards.Int(playerApiId, nameof(playerApiId), x=> x.NotZero())
      .String(name, nameof(name), x=> x.NotNull().NotEmpty())
      .ThrowOnError();

```
Invokes chain of checks on the given parameters. If one or more conditions are not met, the exception is thrown.


2) Supported types

- decimal
- double
- float
- int
- long
- string
- object