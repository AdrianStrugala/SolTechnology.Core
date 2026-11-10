# CI, docs and samples — `.github/`, `README.md`, `docs/`, `sample-tale-code-apps/`

- Action referenced by a moving tag (`@v4`) → a retagged release runs next to `NUGET_API_KEY`. Pin
  the commit SHA with a `# vX.Y.Z` comment; resolve it with `git ls-remote` of `refs/tags/vX.Y.Z`
  and `^{}` (an annotated tag's own SHA is not the commit).
- Dependabot group that mixes published dependency floors with test-only or sample packages, or a
  NuGet major left on → one merge raises a consumer floor (NU1605) and `canonical-versions.md`
  drifts. Ignore majors, keep floor groups pure, and check membership against
  `Directory.Packages.props`.
- Actions bumped in separate PRs → `upload-artifact` and `download-artifact` end on different
  majors, and only the tag-time publish breaks. One `github-actions` group.
- A job that `publish` needs gets `continue-on-error` or an `if:` to unblock a red run → the
  protection is gone, or a skipped job silently skips the publish. Fix the test or fixture.
- README or docs example checked only by reading → it fails for the first visitor. Run it for real:
  a quick start in a project outside the repository tree (no root `Directory.*.props`, no Central
  Package Management) against a local pack feed; a sample from a fresh clone (generated artifacts
  such as a dacpac, amd64-only images on Apple silicon, keychain prompts, ports); a pasted report
  or log regenerated after any change to what produces it.
- Rewrite of a sample or the Coding Guide → kept jokes (Guide §9.10) and "report only" log
  templates (Guide §15) get "cleaned". Grep them before and after.
- Sample type moved between projects (a Domain record into a LogicLayer `Events/` folder) → the
  Guide, `CLAUDE.md` and the skills still teach the old place; edit them in the same change.
- A doc promises a setting that needs admin rights (Private Vulnerability Reporting, content
  reports) → the promise is false until the owner acts. Word the file so it holds either way and
  list the setting as an owner action.
