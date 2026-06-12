# ADR-010: Production-pattern adoption programme

> **Status:** Proposed
> **Decision Date:** 2026-06-12
> **Decision Maker:** Repository maintainers
> **Stakeholders:** Consumers of `SolTechnology.Core.Cache` / `.SQL` / `.Logging` / `.MessageBus`
> / `.Hangfire` / `.Testing` (NuGet + DreamTravel)

---

## Context

A catalogue of patterns, libraries and practices was harvested from **a sample production
application** and proposed for adoption into the
`SolTechnology.Core.*` libraries. Three of those proposals — the Hangfire correlation-id,
result-aware smart-retry, and prevent-overlap job filters — are **already shipped** under
[ADR-009](009-hangfire-persistent-events-and-jobs.md)
(`src/SolTechnology.Core.Hangfire/Filters/`). This ADR plans the **remaining backlog**.

The backlog spans six surfaces and is too large for a single decision:

| Surface | Backlog items | Nature |
|---|---|---|
| Cache | distributed (Redis) tier, resilient cache-aside, Scrutor decorator helper, cache-key/invalidator | new external dependency + new public API |
| SQL | pluggable connection-string providers, SQL-state → `Result` translation, `Result`-returning repository convention, (conditional) EF `EntityBase` companion | new provider seam + possible new package |
| Logging | one correlation primitive across transports, `ILogger` scope helpers, PII masking | additive public API |
| MessageBus | broker-agnostic `IMessageProcessor<T>` seam, scope-per-message + correlation, dead-letter-with-reason, shared pipeline | larger design call + possible new transport dependency |
| Cross-cutting | `TimeProvider`, static `JsonSerializerOptions`, `ValidateOnStart` everywhere, one `Result` type + `MapError`, `[ExcludeFromCodeCoverage]`, primary-ctor consistency | coding-guide rules + mechanical application |
| Testing | AutoFixture UTC specimen, NSubstitute data-attribute factory | additive companion API |
| Hangfire | recommended retry-backoff / worker-count defaults + `MigrateHangfire()` convenience | mostly documentation + one helper |

**Constraints that shape the decision:**

- **Each surface has a distinct semver profile, a distinct dependency footprint, and a distinct
  consumer set.** Bundling them couples unrelated version bumps and makes any single premortem
  incoherent (a premortem cannot reason about "the change" when "the change" is six unrelated
  things).
- **Several surfaces are blocked on decisions that cannot be guessed** (Redis client choice, EF
  companion yes/no, RabbitMQ in-scope-or-later, the single-`Result`-type question). Authoring a
  full code-level decomposition for those surfaces *now* would require fabricating answers.
- [ADR-006](006-implementation-plan-workflow.md) fixes "one ADR = one decision" and the
  `to-do/` → `reviewed/` → `done/` folder model.
- [`CLAUDE.md` §1](../../CLAUDE.md) requires every new external NuGet dependency added to a
  `src/SolTechnology.Core.*` package to be checked and its impact reported.

**Evidence from the current code** (so the programme is scoped, not assumed):

- `Result` already carries the `Map`/`Bind`/`Tap`/`Match`/`Ensure` combinators
  ([`ResultExtensions.cs`](../../src/SolTechnology.Core.CQRS/ResultExtensions.cs)); only `MapError`
  is missing. `Error.Recoverable` already exists
  ([`Errors/Error.cs`](../../src/SolTechnology.Core.CQRS/Errors/Error.cs)). There is exactly **one**
  `Result` type in the solution — the second type referenced by the source catalogue
  (`CSharpFunctionalExtensions`) never entered this repo.
- Raw `DateTime.UtcNow` in production library code is confined to
  [`AUID`](../../src/SolTechnology.Core.AUID/Auid.cs) and
  [`Story`](../../src/SolTechnology.Core.Story/Orchestration/StoryEngine.cs); the Cache / SQL /
  Logging / MessageBus / Hangfire modules are already clock-clean.
- `.ValidateOnStart()` is wired only in
  [`Logging`](../../src/SolTechnology.Core.Logging/ModuleInstaller.cs) and
  [`HTTP`](../../src/SolTechnology.Core.HTTP/ModuleInstaller.cs); `AddCache` / `AddSQL` /
  `AddMessageBus` skip it.
- `ICorrelationIdService`
  ([`Logging/Correlations`](../../src/SolTechnology.Core.Logging/Correlations/ICorrelationIdService.cs))
  is already consumed by HTTP and the shipped Hangfire `CorrelationIdJobFilter`, but **not** by
  [`MessageBusReceiver`](../../src/SolTechnology.Core.MessageBus/Receive/MessageBusReceiver.cs),
  which already does scope-per-message (`CreateAsyncScope`) and dead-letter-with-reason.
- `Scrutor` (5.0.2) and `StackExchange.Redis` (2.8.16) exist in the repo (DreamTravel and
  `SolTechnology.Core.Redis.Testing` respectively) but are **absent** from every
  `src/SolTechnology.Core.*` runtime package.

**Affected modules:** `SolTechnology.Core.Cache`, `.SQL`, `.Logging`, `.MessageBus`, `.Hangfire`,
`.Testing`, `.CQRS` (the `MapError` addition). **Affected sample apps:** none required by this ADR;
DreamTravel may adopt the capabilities opportunistically once each child ADR ships.

## Decision

Adopt the backlog as a **programme of per-module child ADRs under this umbrella ADR**, rather than
one mega-ADR or a flat pile of unrelated ADRs.

1. **ADR-010 (this) is the programme ADR.** Its single decision is *how the backlog is structured,
   sequenced and gated.* It owns the cross-cutting concerns that no single child ADR can: the
   workstream map, the sequence, the shared open questions, and the dependency-impact ledger. It
   ships **no production code**.
2. **One child ADR per module surface.** Each child ADR makes one coherent decision, carries its
   own semver impact, runs its own [`blue-red-team`](../../.github/skills/blue-red-team/SKILL.md)
   and its own [`premortem`](../../.github/skills/premortem/SKILL.md), and seeds its own
   `to-do/` plan. Provisional numbering (assigned next-free per the
   [index](README.md) at authoring time):

   | Child ADR | Workstream | Items | Module(s) | Candidate new dependency | Semver |
   |---|---|---|---|---|---|
   | 011 | Logging: unified correlation + scope helpers + PII masking | L1, L2, L3 | Logging | none | MINOR |
   | 012 | Cache: distributed Redis tier + resilient cache-aside + decorator + invalidator | C1, C2, C3, C4 | Cache | Redis client, Scrutor | MINOR |
   | 013 | SQL: connection-string providers + SQL-state translation + `Result` convention | S1, S2, S4 | SQL | `Azure.Identity` (managed identity) | MINOR |
   | 014 | Cross-cutting coding-guide rules | G1, G2, G3, G5 (`MapError`), G6, G7 | cross-cutting + CQRS | none | PATCH–MINOR |
   | 015 | MessageBus: broker-agnostic seam + scope/correlation + dead-letter + shared pipeline | M1, M2, M3, M4 | MessageBus | `RabbitMQ.Client` (only if transport is in scope) | MINOR–MAJOR |
   | 016 | Testing companions: UTC `DateTime` specimen + NSubstitute data attribute | T1, T2 | Testing | none | MINOR |
   | 017 | Hangfire: recommended retry/worker defaults + `MigrateHangfire()` | H4 | Hangfire | none | MINOR (mostly docs) |
   | 018 (conditional) | SQL EF companion: `EntityBase` + auto UTC timestamps | S3 | new `SolTechnology.Core.SQL.EntityFramework` | EF Core (+ provider) | MINOR (new package) |

3. **Lower-priority items ride inside their module ADR, not as separate ADRs.** C3/C4 sit inside
   ADR-012, S2/S4 inside ADR-013, L2/L3 inside ADR-011 — sequenced as *later code steps within that
   ADR's own plan*. Splitting one module's design across two ADRs would fragment a single coherent
   decision and violates ADR-006's "one ADR = one decision".
4. **A new top-level `src/` package or folder requires explicit maintainer confirmation**
   ([`CLAUDE.md` §1](../../CLAUDE.md)). The only candidate here is `SolTechnology.Core.SQL.EntityFramework`
   (ADR-018, S3); it is **conditional** on the EF open question and is authored only if approved.
5. **Two nested gates, not one.** This programme's premortem (the final step of *this* plan)
   validates the structure before **any** workstream code is implemented; then each child ADR's own
   premortem gates its own code. No `src/` edit happens before both gates for its workstream return
   *Go* / *Go with mitigations*.
6. **Origin is described generically.** No child ADR, step file or summary names the source
   codebase. Patterns are attributed to "a sample production application".

## Alternatives Considered

The argument below is the condensed [`blue-red-team`](../../.github/skills/blue-red-team/SKILL.md)
output for the **structural** decision (how to split), not for the individual workstreams — each of
those re-runs blue/red in its own ADR.

1. **One mega ADR-010 covering all six surfaces, step files = code PRs (pure umbrella-with-steps).**
   *Pros:* one place to read; matches the literal "one ADR + step files" shape.
   *Cons:* violates "one ADR = one decision"; couples six independent semver bumps (a Cache MINOR
   and a MessageBus MAJOR in one ADR); produces a 25-plus-step summary; makes the mandatory premortem
   incoherent (it would have to imagine six unrelated failures at once); forces the EF-companion
   open question to be answered before the Cache work can even be planned. **Rejected.**

2. **A flat set of per-module ADRs (011–018) with no umbrella.**
   *Pros:* each ADR is a clean single decision.
   *Cons:* no home for the cross-cutting sequencing, the shared open questions, or the
   dependency-impact ledger; the programme is invisible as a programme — a reviewer cannot see that
   Logging must precede MessageBus, or that four packages are pending the same four answers; "where
   do I start?" has no single answer. **Rejected.**

3. **Umbrella programme ADR + per-module child ADRs (chosen).** ADR-010 records the structure,
   sequence, open questions and dependency ledger; child ADRs make the per-module decisions and
   carry their own gates. *Pros:* honours "one ADR = one decision"; isolates each semver bump and
   each premortem; lets blocked surfaces wait on their answer without blocking the unblocked ones;
   mirrors how [ADR-006](006-implementation-plan-workflow.md) spawned its skills/agents as steps.
   *Cons:* two layers of ADR; child-ADR numbers are provisional until authored. Mitigated by the
   index being the single source of truth for numbering.

**Cruxes:** (a) *Is the backlog one decision or many?* — settled: the surfaces have independent
semver, dependencies and consumers, so they are many. (b) *Can the surfaces be fully decomposed into
code steps now?* — settled: no, four are blocked on open questions, so detailed design is deferred
to each child ADR. (c) *Do the lower-star items (C3/C4, S2/S4, L2/L3) need their own ADRs?* —
settled: no, they belong to their module's single design and ride as later steps inside it.

## Structural recommendation (explicit)

**Author this umbrella ADR-010 now; author each child ADR just-in-time, in sequence, after its
open questions are resolved.** The step files in this plan are *workstream-launch* steps — each one
authors one child ADR and seeds its plan — because the honest decomposition of a programme that is
blocked on four un-guessable decisions is a set of launch-and-gate steps, not a fabricated pile of
code steps. This keeps every detailed design decision in the ADR that owns it, behind that ADR's own
premortem.

## Sequencing

Derived from the source catalogue's own ordering and the dependency evidence above:

1. **Logging (ADR-011)** — foundational; the unified `ICorrelationIdService` contract (L1) is the
   connective tissue MessageBus (M2) consumes.
2. **Cache (ADR-012)** — `SolTechnology.Core.Redis.Testing` already exists, so the test rig is ready
   the day the runtime tier lands.
3. **SQL connection providers (ADR-013)** — self-contained production capability; default stays the
   static connection string, so existing consumers are untouched.
4. **Cross-cutting (ADR-014)** — fold `TimeProvider` / static `JsonSerializerOptions` /
   `ValidateOnStart` / `MapError` into the guide and apply opportunistically; `MapError` (G5) is the
   only `Result` combinator MessageBus's railway-style handlers will want.
5. **MessageBus (ADR-015)** — the largest design call; depends on ADR-011 (correlation) and the
   single-`Result` confirmation from ADR-014.
6. **Follow-ups (ADR-016 Testing, ADR-017 Hangfire, conditional ADR-018 SQL EF)** — small, low-risk,
   no inter-dependencies.

## Open questions (resolve before authoring the dependent child ADR)

These are surfaced rather than guessed; each blocks the child ADR noted.

1. **Redis client for C1 (ADR-012).** `Microsoft.Extensions.Caching.StackExchangeRedis` (an
   `IDistributedCache` implementation, matching the proposal's "over `IDistributedCache`") vs raw
   `StackExchange.Redis`. The repo already pins `StackExchange.Redis` 2.8.16 in the test companion.
2. **EF companion for S3 (ADR-018).** Introduce a new `SolTechnology.Core.SQL.EntityFramework`
   package (EF Core into a today-Dapper-only module — a new `src/` package, gated by
   [`CLAUDE.md` §1](../../CLAUDE.md)) **or** keep `EntityBase` + UTC-timestamp guidance documentation-only.
3. **RabbitMQ transport for M1 (ADR-015).** Ship a working `RabbitMQ.Client` transport now, or land
   the broker-agnostic `IMessageProcessor<T>` seam with the `MessageBrokerType` switch
   (`Disabled`/`RabbitMq`/`ServiceBus`) and only the existing ServiceBus transport, deferring
   RabbitMQ. Affects whether `RabbitMQ.Client` is added now.
4. **Single `Result` type (ADR-014).** Confirm `SolTechnology.Core.CQRS.Result` is the one canonical
   `Result` (no second type is in the repo) and that the remaining gap is adding `MapError` (+ any
   companion such as `Compensate`); interop with external `Result` libraries stays the consumer's
   boundary concern.
5. **`ValidateOnStart` everywhere (G3, ADR-014).** Extending `.ValidateDataAnnotations().ValidateOnStart()`
   to `AddCache` / `AddSQL` / `AddMessageBus` turns previously-tolerated bad config into a
   **host-start failure**. Confirm this fail-fast behaviour change is acceptable (premortem-worthy in
   ADR-014).

## Dependency-impact ledger ([`CLAUDE.md` §1](../../CLAUDE.md))

Every candidate below is a **new external dependency for a `src/SolTechnology.Core.*` runtime
package** and must go through the [`package-management`](../../.github/skills/package-management/SKILL.md)
+ [`dependency-audit`](../../.github/skills/dependency-audit/SKILL.md) flow and be reported in its
child ADR. None appears in [`nuget-stats.json`](../../nuget-stats.json) (that file tracks published
`SolTechnology.*` packages, not third-party deps).

| Candidate | Child ADR | Already in repo? | Note |
|---|---|---|---|
| Redis client | 012 | `StackExchange.Redis` 2.8.16 in the test companion only | runtime Cache gains a new dep |
| `Scrutor` | 012 | 5.0.2 in DreamTravel only | runtime Cache gains a new dep for `AddCachedDecorator<,>` |
| `Azure.Identity` | 013 | no | only the managed-identity provider needs it |
| `RabbitMQ.Client` | 015 | no | only if open question 3 puts the transport in scope |
| EF Core (+ provider) | 018 | EF 8.0.11 in DreamTravel only | new package `SolTechnology.Core.SQL.EntityFramework`; conditional |

## Source-defect guard-rails

The source catalogue's §9 defects are carried as explicit guard-rails on the relevant child ADRs so
each capability ships with the lesson encoded, not the bug:

- **Logging template/argument mismatch** → ADR-011 + ADR-014: the masking/scope helpers must not
  reintroduce template-vs-argument drift; the coding-guide rule (`ClaudeCodingGuide.md` §11) makes
  the mismatch a review-checklist item.
- **Uncached managed-identity token** → ADR-013 (S1): the managed-identity provider **MUST** cache
  the AAD token until near expiry; per-call token fetches are the defect to avoid.
- **"Mask" that destroys the value** → ADR-011 (L3): the masking contract **MUST** be explicit
  (partial-mask vs `MaskToZero`); a mask that always returns `0`/empty is a foot-gun, not a mask.

## Consequences

**Positive:**
- Each module surface ships independently, on its own semver, behind its own premortem.
- Blocked surfaces wait on their specific answer without holding up the unblocked ones.
- One visible programme: sequence, open questions and dependency exposure live in one place.
- Detailed design stays in the ADR that owns it — no design decision is fabricated in this umbrella.

**Negative:**
- Two layers of ADR; child-ADR numbers are provisional until authored (index is authoritative).
- The umbrella adds a planning hop before any code; deliberate, given the open questions.
- Eight follow-on ADRs (some small) is more ADR overhead than one document — accepted as the cost of
  coherent, independently-revertible decisions.

**Semver impact:** **PATCH** (this ADR is documentation/process only). Each child ADR declares its
own impact per the workstream table.

## Related

- [ADR-006](006-implementation-plan-workflow.md) — plan-folder layout and folder-state model this
  programme follows.
- [ADR-009](009-hangfire-persistent-events-and-jobs.md) — the shipped Hangfire filters (H1–H3) this
  programme explicitly does **not** re-plan.
- [ADR-007](007-cqrs-production-hardening.md) — the in-house `Result` / mediator that G5 (`MapError`)
  and H2 (smart retry) build on.
- [`CLAUDE.md` §1 / §5](../../CLAUDE.md) — dependency-impact reporting and the fix-at-source flow for
  every candidate dependency in the ledger.
- [`docs/ClaudeCodingGuide.md`](../ClaudeCodingGuide.md) §2 / §9 / §11 / §13 — the conventions each
  child ADR's code must satisfy.

## Implementation plan

Multi-step plan in [`010-production-pattern-adoption-programme/`](010-production-pattern-adoption-programme/summary.md).
Each step launches one child ADR; the final step gates the programme with a premortem.

