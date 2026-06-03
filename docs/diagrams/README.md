# Diagrams

Sequence and component diagrams for SolTechnology.Core. One Markdown file per diagram,
authored via the [`diagram` agent](../../.github/agents/diagram.agent.md).

## Conventions

- **Mermaid only.** No PlantUML, no inline colour styling — renderer-specific colours break
  GitHub / IDE diffs.
- **Five layer boxes:** `Presentation`, `Logic`, `Data`, `Domain`, `External`. Boxes appear
  in that order. If a component does not fit, it is `External`.
- **Filename:** kebab-case (`<flow-name>.md`). Updates create `<flow-name>-v2.md`,
  `<flow-name>-v3.md`, … — the original is immutable so older docs / ADRs / reviews keep
  pointing at the version they were written against.
- **Participants are real C# types.** Type names from `src/SolTechnology.Core.*` (or the
  sample apps) only. No invented labels.

See [`diagram.agent.md`](../../.github/agents/diagram.agent.md) for the full template and
self-check list.

