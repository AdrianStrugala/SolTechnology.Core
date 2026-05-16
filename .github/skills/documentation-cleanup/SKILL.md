---
name: documentation-cleanup
description: Validate and fix documentation integrity in SolTechnology.Core — module/doc parity, indexes, tables, Mermaid diagrams, links, ADR formatting, numbered lists.
user-invocable: true
---

# Documentation Cleanup

Adapted from the aiex documentation-cleanup skill for the
[docs/](../../../docs/) folder layout of SolTechnology.Core.

## Process Overview

1. Identify files to validate
2. Check module ↔ doc parity
3. Check indexes
4. Validate tables and Mermaid diagrams
5. Validate ADR structure
6. Check links and formatting
7. Fix numbered lists

## Documentation References

- [README.md](../../../README.md) — top-level module index
- [docs/](../../../docs/) — per-module docs and reviews
- [docs/adr/](../../../docs/adr/) — Architecture Decision Records

## Step-by-Step Instructions

### 1. Identify Files

Collect every `*.md` under [docs/](../../../docs/) and the workspace root.

### 2. Module ↔ Doc Parity

For each folder `src/SolTechnology.Core.<Module>/`:

- A documentation file exists under [docs/](../../../docs/) (the file name is set by
  [README.md](../../../README.md) — e.g. `Story` → `docs/Story.md`,
  `MessageBus` → `docs/Bus.md`).
- The module is listed in the README table with a NuGet badge.
- A review template exists under [docs/reviews/](../../../docs/reviews/) when the module is
  publicly consumed; missing review templates are reported as gaps, not failures.

### 3. Check Indexes

For files that contain an index (README, [docs/Story.md](../../../docs/Story.md), etc.):

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

### 5. Validate ADR Structure

For every file under [docs/adr/](../../../docs/adr/):

- File name matches `NNN-kebab-title.md`, numbering monotonic with no gaps.
- Required sections present: Status, Context, Decision, Consequences.
- Recommended sections noted as gaps when missing: Alternatives Considered, Stakeholders.
- ADR is linked from at least one other doc or from `CLAUDE.md` / `README.md`.

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

## Standard Output Format

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

##### ADR Issues
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

