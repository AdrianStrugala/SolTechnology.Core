# Diagrams

Sequence and component diagrams for SolTechnology.Core. One Markdown file per diagram,
authored via the [`diagram` agent](../../.github/agents/diagram.agent.md).

## Index

| Diagram | Type | Covers |
|---|---|---|
| [`story-framework-components.md`](./story-framework-components.md) | Component | Story core types after the railway→Tale migration — `StoryHandler` → `Tale` (builder/plan) → `StoryEngine` → `Chapter` → `Context`, with the plan-vs-interpreter split. |
| [`story-handle-sequence.md`](./story-handle-sequence.md) | Sequence | One `StoryHandler.Handle()` call: build plan → `StoryEngine.Run` → `ExecuteChapter` → `Chapter.Read` mutates `Context` → `GetResult` projects `Output`. |
| [`tale-code-flow.md`](./tale-code-flow.md) | Flowchart | DreamTravel's `CalculateBestPathTale` as a won/lost-track flow — `Open` → `Expect` → `Read` chapters → `Otherwise` recovery → `WhenLost` → `Finale`. Embeddable companion to `taleCodeFlow.drawio`. |

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

