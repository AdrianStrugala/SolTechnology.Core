---
spec: 2026-07-06-opentelemetry-support
step: 06
status: to-do
---

# Step 06: Documentation

## Summary

Updates the module docs so the shipped telemetry is discoverable: `docs/Log.md` gains the
`AddSolTelemetry` guide and the stable-names contract table; each instrumented module doc
gains a short Observability subsection that links back to `docs/Log.md` instead of
repeating it (Guide §18: cross-link, don't duplicate).

## Affected components

- `docs/Log.md` — EDIT — `AddSolTelemetry` section, names-contract table, metrics coverage
- `docs/Bus.md` — EDIT — Observability subsection
- `docs/Tale.md` — EDIT — Observability subsection
- `docs/SQL.md` — EDIT — Observability subsection (incl. step 04's vendor-source outcome)
- `docs/Cache.md` — EDIT — Observability subsection (incl. Redis-spans opt-in recipe)

## Changes

- `docs/Log.md` (keep Guide §18 fixed section order —
  Features/Registration/Configuration/Usage/Testing/Conventions):
  - Features: add the one-call telemetry bullet.
  - Registration: `builder.AddSolTelemetry()` snippet replaces the hand-rolled
    `AddOpenTelemetry().WithTracing(...AddSource(...))` example.
  - Configuration: `TelemetryOptions` table (Name | Default | Purpose) for section
    `"Telemetry"`.
  - Usage: OTLP gating rules (`OtlpEndpoint` → `OTEL_EXPORTER_OTLP_ENDPOINT` → none);
    `EnableOtlpExporter=false` escape hatch; pairing note (`AddSolTelemetry` does not call
    `AddSolLogging` — register both); correlation note (ids become W3C trace-derived).
  - Stable-names contract table: every `SolTechnology.Core.*` source/meter + instruments
    + tags from ADR-015, with the MAJOR-bump rule.
- `docs/Bus.md` / `docs/Tale.md` / `docs/SQL.md` / `docs/Cache.md`: Observability
  subsection — what the module emits (source/meter, spans, instruments, tags), one link
  to `docs/Log.md` for wiring. No wiring snippets duplicated.
- Mermaid/markdown hygiene per Guide §21; any new diagram MUST go through the `diagram`
  agent (none planned).

## Acceptance criteria

- [ ] [documentation-cleanup](../../../../.github/skills/documentation-cleanup/SKILL.md)
      skill pass is clean (read the SKILL.md before invoking).
- [ ] All links resolve; section order in `docs/Log.md` unchanged.
- [ ] Every instrument/tag name in docs matches the code literals exactly.

## Open questions

- none

## Deviations

<!-- Empty at authoring time. Filled by implement-plan when reality diverges from the plan. -->
