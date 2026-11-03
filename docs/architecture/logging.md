# Logging

`SolTechnology.Core.Logging` adds request logging, correlation, scope enrichment, timing
diagnostics, PII masking and the operation-lifecycle log events on top of
`Microsoft.Extensions.Logging`. It targets the ASP.NET Core shared framework. Consumer-facing usage
is in [`docs/Log.md`](../Log.md).

## One registration path

Both `AddSolLogging(Action<LoggingOptions>?)` and `AddSolLogging(IConfiguration)` call the same
internal `LoggingServiceRegistrations.RegisterCore`, which registers `ICorrelationIdService`,
`TimeProvider.System`, the scoped `ITimingService`, the built-in `RequestHeadersEnricher` and the
validated `LoggingOptions` (`ValidateOnStart`). The overloads differ only in how options are filled:
`Configure` or `Bind` to `Logging:Core`. A single path keeps the overloads from drifting apart: an
overload that misses a service `LoggingMiddleware` resolves per request fails every request. The shared
registrations use `TryAdd*` or an explicit "already registered" check, so repeated calls and
consumer overrides are safe. `ModuleResolutionTests` resolves every service under `ValidateOnBuild` and
`ValidateScopes` and sends a request through `UseSolLogging()` for both overloads.

## Request pipeline

`LoggingMiddleware` runs per request in this order:

1. Resolve the correlation id, store it in `ICorrelationIdService`, and echo it when the response
   starts.
1. Skip everything else for `SkipPaths`; the correlation id is still echoed.
1. Build the log scope: the correlation id plus every `ILogScopeEnricher`. A failing enricher is
   logged and skipped. The JSON body is parsed once, only when an `IRequiresRequestBody` enricher
   asks for it, only for `application/json` within `MaxLoggedJsonBodyBytes`, and the stream is
   rewound for model binding (`RequestBodyBuffer`).
1. Write `Started`, call the rest of the pipeline, then write exactly one outcome entry.

| Outcome | Entry | Level |
|---|---|---|
| Response completed | `Finished … -> [{StatusCode}]`, with `timings` when any were recorded | Information below 400, Warning for 4xx, Error for 5xx |
| Unhandled exception | `failed after`, with the exception; status set to 500 if the response has not started | Error |
| Client abort (`ConnectionResetException`, or cancellation of `RequestAborted`) | `aborted by client after`, with the exception | Warning |

A logged exception is never followed by a `Finished` entry: a second Error entry for the same
failure would double error-rate alerts. Every middleware entry is a `[LoggerMessage]` method on the
internal `RequestEnvelopeLogger` with event ids 2141–2148; templates bracket every placeholder
(Coding Guide §11).

`SkipPaths` and `LogDetail` endpoint lists match per path segment and case-insensitively
(`LoggingPathMatcher`, `PathString.StartsWithSegments`): `/health` covers `/health` and
`/health/ready` but not `/healthcare`. A trailing `/` in a prefix is ignored, and a prefix without a
leading `/` never matches.

## Correlation ids

The id is the current `Activity` trace id when one exists; otherwise the inbound `X-Correlation-Id`
header; otherwise a generated 32-character hex value. The inbound header is accepted only when it is
at most 128 characters from `[A-Za-z0-9._:-]` (`CorrelationIdValidator`). The value is echoed on the
response and written into every log scope, so anything else (CR/LF, markup, separators) is replaced
by a generated id, and the rejection reason logged as a Warning never contains the rejected value.

`traceparent` is echoed only when the correlation id is the current trace id; an id taken from
`X-Correlation-Id` has no trace context, so no span id is invented for it. The same
`TryBuildTraceParentFromCurrentActivity` formats the header for the outbound handler in
`SolTechnology.Core.HTTP`.

## Timings and masking

`ITimingService` is scoped: each request gets its own instance, and the middleware resets it before
`Started`. `GetTimings()` returns a read-only snapshot, so the logged map cannot change after it is
taken and callers cannot mutate the service's state.

`PiiMask.Partial` keeps `keepChars` characters at each end only when the value is longer than
`4 * keepChars`, so at most half of a value is revealed; shorter values are fully masked, and a
negative `keepChars` throws `ArgumentOutOfRangeException`.

## Operation events and tracing

`LoggerExtensions` adds `OperationStarted`, `OperationSucceeded` and `OperationFailed` to every
`ILogger` through an `extension(ILogger)` block. They emit event ids 2137–2140 through
`[LoggerMessage]` methods; a caller's message is always a `[{Message}]` argument, never a template.

`CoreLoggingActivitySources.OperationsName` is the public contract for tracing: hosts subscribe with
`AddSource(OperationsName)`. The `ActivitySource` field itself is internal. `SolTechnology.Core.CQRS`
is a separately versioned package, so it creates its own process-wide `ActivitySource` with the
same name instead of reading Logging internals through `InternalsVisibleTo`; a listener subscribed
to the name receives activities from both sources.

CQRS references Logging, so it also depends on the ASP.NET Core shared framework. Packages version
in lockstep and warn with `SOLCORE001` on a mixed major-version graph; see
[Package release](package-release.md) and [CQRS](cqrs.md).
