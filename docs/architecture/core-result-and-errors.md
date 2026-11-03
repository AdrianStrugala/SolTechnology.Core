# Result and errors

`SolTechnology.Core` is the base package every other module builds on. It contains only the
`Result` / `Result<T>` outcome types and the `Error` hierarchy, and it has no package
dependencies. Consumer-facing usage is in [`docs/Core.md`](../Core.md).

## Results are built only through factories

`Result.IsSuccess` and `Result.Error` can be set only inside the `SolTechnology.Core` assembly
(`private protected init`), and `Result<T>.Data` only inside `Result<T>` (`private init`). The
static factories (`Success`, `Fail`, `SuccessAsTask`, `FailAsTask`) and the implicit conversions
from a value or an `Error` are the only public way to build a result. This rules out contradictory
states such as a successful result that also carries an error, which downstream layers (the API
`ResultConversionFilter`, CQRS behaviors, Tale) would otherwise have to guess about.

Every factory on `Result<T>` returns a `Result<T>` or `Task<Result<T>>`; the generic type hides the
base `Fail`/`FailAsTask` overloads with `new` so a typed handler never receives an untyped result.

## JSON contract

`Result`, `Result<T>` and `Error` serialize with System.Text.Json. The read-only properties carry
`[JsonInclude]`, so JSON written by 1.x deserializes unchanged and the 2.0 output is byte-identical
to 1.x. Consequences:

- OpenAPI generators mark `isSuccess` and `data` as read-only, which is correct for a
  response-only type.
- The contract is System.Text.Json only. Newtonsoft-serialized payloads (MessageBus messages,
  Hangfire job arguments) must not carry a `Result`.

## Error polymorphism

`Error` declares a fixed set of discriminators (`error`, `notFound`, `conflict`, `validation`,
`unauthorized`, `forbidden`, `aggregate`, `timeout`, `deadlock`) and
`[JsonPolymorphic(UnknownDerivedTypeHandling = FallBackToNearestAncestor)]`. A subtype without a
discriminator serializes as its nearest registered ancestor instead of throwing
`NotSupportedException`.

The base package cannot reference the modules that define their own errors (for example Tale), so
a closed list could never cover them. The fallback keeps serialization total; the trade-off is that
an unregistered subtype loses its runtime type and extra properties on a round-trip. A module that
must round-trip its own subtypes registers them in the serializer options it uses; the base list
stays limited to the base package's own types.

Tale does this for its own errors. The public, read-only `TaleJsonOptions.Default` adds a type-info
modifier that registers `TalePausedError` as `talePaused` and `TaleCancelledError` as
`taleCancelled` on `Error`. `TaleInstance.Clone()` and DreamTravel's SQLite repository serialize
chapter history with it, and custom Tale repositories must too; with default options a stored Tale
error comes back as a plain `Error`. See [Tale framework](tale-framework.md).

`AggregateError` restores its `InnerErrors` from JSON through a private `[JsonConstructor]` that
keeps the already-built message as-is. The public constructors build the message from the base
message plus each inner message; a public JSON constructor would compete with them in overload
resolution.

## Versioning

The base package versions in lockstep with every other `SolTechnology.Core.*` package. Each package
ships a `buildTransitive` target that warns with `SOLCORE001` when the resolved
`SolTechnology.Core.*` packages span more than one major version, because a mixed graph compiles
but fails at runtime. See [Package release](package-release.md).
