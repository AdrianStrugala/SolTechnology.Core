---
spec: 2026-11-02-logging-chronicle
step: 03
status: done
---

# Step 03: Chronicle report model, ASCII tree renderer, and N+1 aggregation

## Summary
This step turns a closed `ChronicleRoot` into the public, read-only `ChronicleReport`. The report
is the structured payload (B6). Its `ToString()` renders the human-readable ASCII tree used in the
log message. Siblings that share a group key collapse into one aggregated line once they reach
`AggregateThreshold`, and a count at or above `NPlusOneThreshold` adds an N+1 hint. Rendering is
pure and has no logging dependency, so golden-string tests pin the format.

## Affected components
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleReport.cs` — NEW — public sealed read-only (CH-06, without `FlightRecords`)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleReportStep.cs` — NEW — public sealed read-only (CH-06)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleReportBuilder.cs` — NEW — internal: snapshot under node locks, order children by start offset, aggregate groups, merge overflow aggregates (step 02), set `IsNPlusOneSuspected`
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleTreeRenderer.cs` — NEW — internal: ASCII tree with a pooled `StringBuilder`
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleOptions.cs` — EDIT — `AggregateThreshold` (3), `NPlusOneThreshold` (10) (CH-04)
- `src/SolTechnology.Core.Logging/Chronicles/ChronicleOptionsValidator.cs` — EDIT — `AggregateThreshold >= 2`, `NPlusOneThreshold >= AggregateThreshold`
- `tests/SolTechnology.Core.Logging.Tests/Chronicles/ChronicleReportBuilderTests.cs` — NEW
- `tests/SolTechnology.Core.Logging.Tests/Chronicles/ChronicleTreeRendererTests.cs` — NEW

## Changes
- Line format: `{indent}{|- or `-}{Name} [@{OffsetMs} ms] [{DurationMs} ms] [{status}]` followed by `Key=Value` attributes.
  - Status is rendered lowercase: `ok`, `failed`, or `cancelled`.
  - A failed step adds `Error=[{type}: {message}]`.
- Aggregated line: `{Name} x{Count} [total {DurationMs} ms] [max {MaxMs} ms] [failed {FailedCount}]`. At or above the N+1 threshold it appends `N+1 suspected`. The children of aggregated steps are not rendered (their count is shown as `+{n} nested`).
- Markers: `[truncated]`, `+{n} steps dropped`, `+{n} deeper steps`, `+{n} attributes dropped`, `+{n} nested`, `(unfinished)`, `+{n} late steps`.
  - `+{n} late steps` and `ChronicleReport.LateStepCount` count steps started under an already
    closed parent step while the root was open (step 01). Steps after root close never reach a
    report.
- Characters are ASCII only. The output starts with a newline, so the tree begins on its own line inside `[{@Chronicle}]`.
- `ChronicleReport.ToString()` returns the rendered tree. The renderer is called once and the
  result cached, because the logger may call `ToString` more than once.
- Tests:
  - Golden strings:
    - `Render_NestedTree_MatchesGolden`.
    - `Render_ParallelSiblings_OrderedByOffset`.
    - `Render_FailedStep_ShowsErrorTypeAndMessage`.
    - `Render_UnfinishedAndLate_ShowMarkers`.
  - `Report_TwoIdenticalSteps_NotAggregated`.
  - `Report_ThreeIdenticalSteps_Aggregated`.
  - `Report_FortyTwoIdenticalSQLSteps_AggregatesWithNPlusOneHint` (group key set directly on the node).
  - `Report_OverflowAggregate_MergedIntoGroupLine`.
  - `Render_OutputIsASCIIOnly` (every char < 128).
- Premortem mitigations (step 00, 2026-11-02):
  - CRM-3: the renderer enforces an internal total budget of 32,768 chars. Lines beyond the
    budget are replaced by one `+{n} lines truncated` marker; the root line and the marker are
    always present. Test `Render_WorstCaseDefaults_AtMost32768Chars` (256 steps × 8 attributes ×
    200-char values, 1,000-char names, default options).
  - CRM-4: the builder sets an internal (not public) `ChronicleReport.HasFailedSteps`. It is true
    when any recorded node is `Failed`, an aggregated line has `FailedCount > 0`, or an overflow
    aggregate has a failed count. Tests `Report_FailedChildUnderOkRoot_HasFailedSteps` and
    `Report_FailedOverflowStep_HasFailedSteps`.

## Acceptance criteria
- [x] `dotnet test tests/SolTechnology.Core.Logging.Tests --filter "FullyQualifiedName~Chronicles"` is green.
- [x] `find src/SolTechnology.Core.Logging/Chronicles -name "*.cs" | xargs wc -l | awk '$1>150 && $2!="total"'` prints nothing.
- [x] CRM-3: `Render_WorstCaseDefaults_AtMost32768Chars` is green.
- [x] CRM-4: `Report_FailedChildUnderOkRoot_HasFailedSteps` and `Report_FailedOverflowStep_HasFailedSteps` are green; `HasFailedSteps` is `internal`.

## Open questions
- none (OQ-1 confirmed by the user 2026-11-02: `CH-04` thresholds, `CH-06` report model)

## Deviations

### The root is the single entry of `ChronicleReport.Steps`

**Original plan:** CH-06 lists `Steps` without saying whether it holds the root.

**Actual implementation:** `Steps` holds one entry, the root step, whose `Children` are the
recorded steps. The root's attributes (HTTP root data, `DetachedFrom` in step 08) and markers
need a `ChronicleReportStep`, and CH-06 gives `ChronicleReport` no `Attributes`. Serilog therefore
sees one more level below `Chronicle` (step 15 documents the depth cap).

### Rendering details not fixed by the plan

**Original plan:** an ASCII tree rendered with a pooled `StringBuilder`; two tests
`Report_TwoIdenticalSteps_NotAggregated` and `Report_ThreeIdenticalSteps_Aggregated`.

**Actual implementation:**

- A plain `StringBuilder` per render: the tree is rendered at most once per emitted report and
  cached, and a static pool would be static mutable state (Guide §9.6).
- The renderer's own characters are ASCII; user text (names, values, messages) is passed through,
  except that line breaks and tabs become spaces so they cannot split the tree. Non-ASCII user text
  is not transliterated.
- Overflow aggregates always render as an aggregated line, even below `AggregateThreshold`,
  because dropped steps have no node to render one by one. An aggregated line's status is
  `Failed` when any of its steps failed, otherwise `Ok`.
- The line data CH-06 does not expose (`max`, the error type and message, the marker counts) stays
  internal on `ChronicleReportStep`; no public member was added.
- The two aggregation tests are one parameterized test,
  `Report_IdenticalSteps_AggregatedFromThreshold` (Guide §8).

