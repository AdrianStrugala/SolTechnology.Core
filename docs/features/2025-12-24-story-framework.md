---
status: completed
created: 2025-12-24
completed: 2026-01-31
---

# Story Framework

> Historical delivery record. The framework was later renamed to Tale and this file does not
> describe the current API.

## Goal

Introduce explicit, typed, resumable orchestration as a replacement for competing Chain and Flow
patterns.

## Context

Workflows were previously expressed with two abstractions: `ChainHandler` for sequential in-memory
work and `FlowHandler` for persisted steps with a separate DSL. This created two mental models for
the same underlying problem of ordered execution, shared state, and pausing. Flow persistence was
bespoke, interactive pause/resume was awkward, and business intent was obscured by framework
ceremony.

The feature set out to replace both models with one narrative abstraction whose orchestration read
like prose while individual steps remained typed, testable, and resolved through DI.

## Original approach

The planned framework used Story terminology:

- `StoryHandler<TInput, TContext, TOutput>` defined an explicit `TellStory()` pipeline.
- `Chapter<TContext>` and `InteractiveChapter<TContext, TInput>` implemented individual steps.
- `StoryManager` owned start, resume, and cancellation for persisted instances and created a fresh
  DI scope for each invocation.
- `IStoryRepository` formed the persistence boundary, with in-memory and SQLite implementations
  initially planned in the package.
- `StoryController` exposed an HTTP lifecycle only for handlers registered through
  `StoryHandlerRegistry`.

The design followed the Tale Code principle before the final Tale naming existed: orchestration
should reveal business order at a glance, and chapter names should describe actions.

## Design guarantees

### Typed lifecycle signaling

Pause and cancellation were represented by dedicated `Error` subtypes rather than message-string
matching. Callers could distinguish an interactive pause from cancellation by type.

### Strongly typed execution

The engine was generic over input, context, and output. Context inherited from a typed base that
exposed input, output, instance ID, and current chapter ID without `dynamic` property access.

### Interactive chapter discovery

Interactive chapters were detected across their inheritance chain so multi-level derived chapter
types remained valid.

### Lifecycle operations

- Start accepted an idempotency key so retries returned the existing instance.
- Repository listing supported status, handler, skip, and take filters for operator tooling.
- Cancellation changed the persisted status and was available through the HTTP surface.

### HTTP and security posture

The original HTTP contract mapped completion to `200 OK`, pause to `202 Accepted`, malformed or
missing instances to `400` or `404`, and workflow failure to `400`. Handler resolution was limited
to explicitly registered types to prevent arbitrary short-name activation. Applications remained
responsible for adding authorization.

## Alternatives considered

### Keep Chain and Flow side by side

Rejected because it preserved duplicate mental models and forced consumers to choose a framework
before expressing the workflow.

### Adopt an external workflow engine

Temporal, Durable Task, and Elsa were rejected for the first release. They would add deployment
and integration requirements before the repository had established its own lightweight,
in-process developer model.

### Build an event-sourced saga

Rejected as valuable but orthogonal. Cross-process compensation was deliberately left outside the
first version.

## Scope

- Add handler, chapter, context, manager, repository, and controller abstractions.
- Support typed lifecycle signaling, idempotent starts, listing, and cancellation.
- Provide in-memory and initially planned SQLite persistence.
- Keep execution sequential and DI-native.
- Document migration from Chain and Flow.

## Implementation plan

Deliver the orchestration engine, registration, persistence, HTTP surface, tests, and consumer
documentation. Migrate the concepts as follows:

| Previous concept | Planned Story concept |
|---|---|
| `ChainHandler<TInput, TState, TOutput>` | `StoryHandler<TInput, TContext, TOutput>` |
| `ChainStep` | `Chapter<TContext>` |
| `Invoke<T>()` | `ReadChapter<T>()` |
| `FlowHandler` | Persisted `StoryHandler` |
| `AddFlows` | `RegisterStories` |

## Acceptance criteria

- Workflow order is readable from one orchestration method.
- Interactive workflows can pause and resume with persisted state.
- Persistence can be replaced by the consuming application.
- Lifecycle outcomes are represented by typed errors.
- Starts can be idempotent and persisted instances can be listed and cancelled.
- Only explicitly registered handlers can be activated through the HTTP surface.
- Tests cover execution, error aggregation, pause/resume, persistence, lifecycle endpoints,
  cancellation, idempotency, and listing.

## Expected consequences

### Positive

- One narrative abstraction covers synchronous and human-in-the-loop workflows.
- Generic input, context, and output keep orchestration strongly typed.
- Chapters remain independently testable and DI-resolved.
- Persistence is replaceable behind a repository interface.

### Limitations accepted for the first release

- Execution is sequential; there is no parallel chapter primitive.
- Durable retry and backoff are not framework concerns; chapters compose their own resilience.
- Cross-process saga compensation is not supported.
- Context is serialized in full, so large batch workflows are out of scope.
- Operator support is limited to repository listing rather than a dashboard or CLI.
- Distributed tracing and handler-version compatibility are deferred.

## Deferred handler versioning

An early strict version check was removed because exact equality would invalidate every in-flight
workflow after any deployment. The preferred future direction was semantic compatibility:

- no comparison by default;
- optional major-version compatibility, where minor and patch changes remain resumable;
- optional strict equality for consumers that explicitly need it;
- asymmetric versioned/unversioned instances require an operator decision.

The historical compatibility proposal was:

| Persisted | Current | Expected result |
|---|---|---|
| `1.0.0` | `1.0.1` | Compatible patch change |
| `1.0.0` | `1.4.2` | Compatible minor change |
| `1.0.0` | `2.0.0` | Typed version-mismatch failure |
| `null` | `null` | Both unversioned |
| `null` | `1.0.0` | Explicit migration decision required |

Append-only chapters and nullable context additions were considered compatible; reordering or
removing chapters and changing persisted context or interactive-input shapes were considered
breaking. This remained a proposal and did not become part of the delivered contract.

## Completion summary

The typed orchestration engine, chapters, contexts, lifecycle management, persistence boundary,
HTTP lifecycle, and broad automated coverage shipped under Story terminology. The model was later
renamed and released as `SolTechnology.Core.Tale`.

The completed implementation retained the core narrative model, typed lifecycle signaling,
idempotency, listing, cancellation, scoped execution, and explicit handler registration. Current
behavior and rationale live in
[`../architecture/tale-framework.md`](../architecture/tale-framework.md); this record intentionally
preserves the design as it was understood during delivery.

## Deviations

- All `Story*` names and `/api/story` routes were replaced by Tale equivalents during release
  1.0.
- SQLite persistence was later removed from the package and moved to DreamTravel.
- Handler versioning described in early plans did not ship.
- Chain and Flow migration happened as part of the later Tale release rather than remaining an
  indefinitely parallel consumer choice.
- Earlier readiness, benchmark, coverage-percentage, and migration-number claims could not be
  reproduced and are not treated as delivered evidence.

## Follow-ups

- Add tracing and metrics only when backed by a concrete observability requirement.
- Consider handler compatibility only for workflows that must survive multiple deployments.
- Keep active handler changes additive and append-only while instances are in flight.

## Historical references

- Tale naming and package release: [`2026-06-29-release-1-0.md`](2026-06-29-release-1-0.md)
- SQLite extraction: [`2026-06-22-tale-sqlite-extraction.md`](2026-06-22-tale-sqlite-extraction.md)
