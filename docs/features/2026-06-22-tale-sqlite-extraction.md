---
status: completed
created: 2026-06-22
completed: 2026-06-23
---

# Tale SQLite Extraction

> Historical delivery record. It may not describe the current system.

## Goal

Remove provider-specific SQLite persistence from the orchestration package and retain it as a
DreamTravel sample implementation.

## Context

Shipping SQLite in the core workflow package imposed database dependencies and configuration on
consumers that only needed in-memory or custom persistence. The native SQLite bundle also carried
the high-severity `CVE-2025-6965` advisory, with no patched bundle available at planning time. The
published Story package suppressed the resulting audit warning, so every consumer inherited a
vulnerable native dependency even though in-memory persistence was the default.

The package already exposed the required extension point: a repository interface, an in-memory
implementation, and generic repository registration. SQLite could therefore move to a consumer
without widening the framework API.

Planning also found an unused Newtonsoft.Json dependency and public documentation describing
non-existent persistence methods. The implementation used `System.Text.Json` and the real builder
registration seam.

## Original decision

Move the SQLite implementation into a new DreamTravel DataLayer project while keeping only the
persistence contract and dependency-free default in the core package.

The new sample project would own:

- SQLite repository and options types using repository acronym casing;
- the SQLite package references and an explicit, documented audit suppression;
- tests against a real file database;
- registration through the existing generic repository method and DI-registered options.

The core package would remove the provider, provider-specific registration overloads, SQLite
packages, audit suppression, and unused Newtonsoft.Json reference. No running DreamTravel host
would switch to SQLite because doing so would add the vulnerable native bundle to the sample's
runtime path; the project, tests, and docs were sufficient to demonstrate custom persistence.

Implementation was sequenced to create and test the new home before deleting the package-owned
provider.

## Alternatives considered

### Keep SQLite and suppress the advisory

Rejected because every package consumer would continue to inherit a high-severity native
dependency regardless of whether SQLite was used.

### Publish `SolTechnology.Core.Tale.SQLite`

Rejected at the time because a new public package would still ship the unpatched dependency and
require its own suppression. It remained the natural future option after a patched provider became
available and multiple consumers demonstrated demand.

### Add another public repository-replacement method

Rejected because the existing generic registration method already replaced the repository and
completed manager wiring.

### Move the provider to DreamTravel

Selected because it removed the dependency and advisory from all published libraries while proving
the existing bring-your-own-repository contract.

## Scope

- Keep `ITaleRepository` as the package boundary.
- Move SQLite repository code and tests to DreamTravel.
- Remove package-owned SQLite registration and dependencies.
- Preserve JSON compatibility and pause/resume behavior.

## Implementation plan

Extract the provider, update registration and sample tests, remove package dependencies, and
refresh documentation.

## Acceptance criteria

- The Tale package has no SQLite dependency.
- DreamTravel can register its own durable repository.
- In-memory persistence remains the package default.
- No SQLite or Newtonsoft.Json package remains in the core orchestration package.
- The source build contains no suppression for the SQLite advisory.
- Real SQLite tests cover repository operations and workflow pause/resume.
- Documentation names only registration methods that exist.

## Expected consequences

### Positive

- Consumers that do not use SQLite stop receiving native database dependencies and audit noise.
- The package's generic persistence seam gains a tested external implementation.
- The dependency graph and public surface become smaller.

### Negative

- Existing pre-1.0 consumers of package-owned SQLite types must vendor or adapt the sample provider.
- SQLite becomes less discoverable from the package alone.
- DreamTravel's provider project retains the known advisory until its dependency can be upgraded.

## Completion summary

SQLite persistence moved to `DreamTravel.SQLite`; package-owned provider code, registration
overloads, three dependencies, and the source audit suppression were removed. The sample registered
the repository directly through the generic public seam, making the planned wrapper extension
unnecessary. Ten component tests covered repository behavior and a complete sample workflow.

During component testing, an engine defect was discovered and fixed: validation failure in an
interactive chapter had incorrectly marked the workflow failed. It now remains paused so the user
can retry input.

The package retained a replaceable repository boundary. Current behavior lives in
[`../architecture/tale-framework.md`](../architecture/tale-framework.md).

## Deviations

- The subsequent Story-to-Tale rename changed all historical type names.
- No consumer-side `UseSQLiteStoryRepository` wrapper shipped; generic repository registration and
	DI options were sufficient.
- Tests were placed in DreamTravel component tests rather than a new unit-test project because they
	used a real SQLite file and complete DI container.
- Interactive-input validation behavior was corrected as an adjacent engine bug found by the new
	end-to-end test.

## Follow-ups

- Consider a reusable provider package only after the native dependency is patched and more than one
	consumer needs it.
- Keep long-lived Tale context changes backward-compatible while instances remain active.
