---
name: documentation-cleanup
description: Validate and fix documentation integrity in SolTechnology.Core — current architecture, dated feature records, module/doc parity, indexes, tables, Mermaid diagrams, links, and numbered lists.
---

# Documentation Cleanup

Validate and fix documentation integrity across the [docs/](../../../docs/) folder.

## Process overview

1. Identify files to validate
2. Check module ↔ doc parity
3. Check indexes
4. Validate tables and Mermaid diagrams
5. Validate architecture and feature records
6. Check links and formatting
7. Fix numbered lists

## Documentation references

- [README.md](../../../README.md) — top-level module index
- [docs/](../../../docs/) — per-module docs and reviews
- [docs/architecture/](../../../docs/architecture/) — current system and rationale
- [docs/features/](../../../docs/features/) — historical delivery records

## Step-by-step procedure

### 1. Identify files

Collect every `*.md` under [docs/](../../../docs/) and the workspace root.

### 2. Module ↔ Doc Parity

For each folder `src/SolTechnology.Core.<Module>/`:

- A documentation file exists under [docs/](../../../docs/) (the file name is set by
  [README.md](../../../README.md) — e.g. `Tale` → `docs/Tale.md`,
  `MessageBus` → `docs/Bus.md`).
- The module is listed in the README table with a NuGet badge.

### 3. Check Indexes

For files that contain an index (README, [docs/Tale.md](../../../docs/Tale.md), etc.):

- Every index entry has a matching section in the document.
- Every top-level section is listed in the index.
- Index links use angle brackets for any path containing spaces: `[Text](<path/file.md>)`.
- Index hierarchy matches the document hierarchy.

### 4. Validate Tables and Mermaid Diagrams

For each table:

- Header row present, separator row matches column count, every data row matches column count.

For each Mermaid block:

- Syntax parses (no stray characters).
- `classDef` declared before first use.
- Node names with spaces use `<br>` line breaks: `Node[Name<br>With<br>Spaces]`.

### 5. Validate Architecture and Feature Records

For every file under [docs/architecture/](../../../docs/architecture/):

- It describes current behavior and rationale, verified against source and tests.
- It contains no obsolete status, implementation tracker, or historical narrative.
- It links to code and module docs instead of copying contracts.

For every durable file directly under [docs/features/](../../../docs/features/):

- Filename matches `YYYY-MM-DD-<kebab-name>.md`.
- Frontmatter contains `status`, `created`, and `completed`.
- Status is `planning | planned | in-progress | blocked | completed | abandoned`.
- Completed and abandoned records have a completion date and completion summary.
- The record warns that it may not describe the current system.
- No durable link points into a deleted or unrelated working folder.

Flag `docs/adr/`, `docs/features/README.md`, numbered feature filenames, or hand-maintained feature
status indexes as obsolete workflow artifacts.

### 6. Check Links and Formatting

- Paths with spaces use angle brackets: `[Text](<path/file.md>)`.
- Relative paths resolve from the file's location.
- Linked files exist on disk.
- Module docs use relative links into `src/SolTechnology.Core.*` only when the file is stable
  (interfaces, `ModuleInstaller.cs`, public entrypoints).

### 7. Fix Numbered Lists

- Use `1.` for every item (markdown auto-numbers).
- Nested lists indented by 4 spaces.
- Hierarchy consistent within a document.

Ignore backtick / inline-code formatting issues unless they break rendering.

## Output format

### Documentation Cleanup Report

#### Files Validated
<placeholder>List with brief status.</placeholder>

#### Issues Found

##### Module ↔ Doc Parity
<placeholder>Modules without a doc, docs without a module, README gaps.</placeholder>

##### Index Issues
<placeholder></placeholder>

##### Table / Mermaid Issues
<placeholder></placeholder>

##### Architecture / Feature Issues
<placeholder></placeholder>

##### Link Issues
<placeholder></placeholder>

##### Numbered List Issues
<placeholder></placeholder>

#### Fixes Applied
<placeholder>Per-file summary.</placeholder>

#### Summary
**Files validated**: <placeholder>count</placeholder>
**Issues found**: <placeholder>count</placeholder>
**Fixes applied**: <placeholder>count</placeholder>

