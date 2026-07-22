# Public Documentation Guide

Authoring conventions for user-facing module pages under `docs/<Module>.md`. These pages are what
a developer reads on GitHub before deciding to pull the package. Keep them technical, dense, and
example-driven; do not write marketing prose or internal incident retrospectives.

## Canonical structure

Do not reorder these sections or invent new top-level sections:

```text
## SolTechnology.Core.<Module>
<one-paragraph lead: what the package gives you, in one sentence plus one elaboration>

### Features
- 5-9 concrete, user-observable capabilities

### Registration
<one-call happy path, then the lower-level composition path when it exists>

### Configuration
<Name | Default | Purpose table plus one binding snippet; "no config needed" when true>

### Usage
<subsections per capability, each with a one-line lead and code example>
<tables for behavior matrices and mappings>

### Testing
<shipped fixture or helpers, with one Arrange/Act/Assert example>

### Conventions
<short, imperative consumer DOs and DON'Ts>

### What ships in DI
<optional: include when AddXxx registers more than two services>

### Working with AI Agent
<optional: include when the module has a companion skill under .github/skills/>
```

Content that does not fit belongs in current architecture under `docs/architecture/`, the dated
feature record, or inline XML documentation on a public type.

## Hard rules

1. Do not include essays, war stories, or incident retrospectives. State what each option does and
   its default.
2. Describe user-observable benefits under `### Features`, not implementation details. Put DI
   implementation details under `### What ships in DI` or in XML documentation.
3. Keep lead-in text under `###` and `####` headings to one sentence. Let the following code block
   carry the detail.
4. Make code examples runnable. Use `// ...` only to replace unrelated boilerplate; the remaining
   snippet must compile in the consumer project.
5. Use tables for status mappings, behavior matrices, and option defaults.
6. Do not use `> Tip:` or `> Note:` blockquotes. Keep material information in the body and delete
   noise.
7. Keep one page per module. Do not split one module across pages or merge multiple modules. When a
   page exceeds 300 lines, assess whether the module itself needs splitting.
8. Cross-link instead of duplicating content shared by module pages.
9. Treat module pages as companions to architecture, not replacements. They explain package use;
   current rationale belongs under `docs/architecture/`, and delivery history belongs in dated
   feature records.
10. Do not rewrite module pages during a project-documentation migration. Change only links or
    content directly required by the task; audit public API accuracy as separate explicit scope.
11. Use absolute GitHub URLs in `### Working with AI Agent`. NuGet.org cannot resolve repository-
    relative `.github/` links. The consumer opts into the companion skill; never auto-install it.
    Follow the [package-companion skill policy](../.github/skills/README.md#package-companion-skills).

## Refactoring an existing page

- Remove `Overview`, `Introduction`, and `About` headings; fold their useful content into the lead.
- Remove prose that paraphrases the heading or the code block below it.
- Replace numbered headings that are not a real sequence with a table or flat `####` subsections.
- Move `Manual setup`, `Behind the scenes`, and internal implementation explanations into XML
  documentation or current architecture. Keep only consumer-facing interactions in the module page.

## Reference implementation

[`Api.md`](Api.md) is the canonical example. Other module docs, including `Log.md`, `Bus.md`, and
`HTTP-Production-Checklist.md`, are migrating toward this shape; align one only when the task
already touches its surrounding module.