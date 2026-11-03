---
spec: 2026-11-02-core-2-0-flagship-hardening
step: 06
status: in-progress
---

# Step 06: Remove leaked-secret artifacts and personal settings; record revocation

## Summary
`TraveligEcoSalesman_legacy_desktop_version.zip` contains two live-format Google API keys (`S1`).
History also holds four Google keys and a `RootManageSharedAccessKey` for
`talecodeservicebus.servicebus.windows.net` (`S2`). Per D1, the owner revokes them and the tree
drops the artifacts, but history is not rewritten. Personal IDE and agent settings are also
tracked, one of them with a local `sa` connection string (`repo#A11`).

Former core step 09; moved here by the per-library regroup (user, 2026-11-02).

## Affected components
- `sample-tale-code-apps/DreamTravel/TraveligEcoSalesman_legacy_desktop_version.zip` — DELETE (`git rm`)
- `sample-tale-code-apps/DreamTravel/DreamTravels_legacy_angular_version_2.zip` — DELETE (`git rm`, legacy zip, `repo#A12`)
- `.idea/.idea.SolTechnology.Core/.idea/*` — DELETE from the index (`git rm --cached -r .idea`)
- `.claude/settings.local.json` — DELETE from the index (`git rm --cached`; the local file stays for `CLAUDE.md §3`)
- `src/.claude/settings.local.json` — DELETE from the index
- `.vscode/settings.json` — DELETE from the index
- `.gitignore` — EDIT — add `.idea/`, `**/.claude/*.local.json`, `.vscode/settings.json`, `*.zip` under `sample-tale-code-apps/`

## Changes
- `git rm` both zips and `git rm --cached` the settings files in one commit: `chore(repo): remove leaked legacy zips and personal settings`.
- Owner manual actions (D1), each recorded with a date under Deviations:
  - GCP: revoke `AIzaSyBg…` and `AIzaSyCd…` (zip), plus the four Google keys in commits `f347f871`, `acb13d74`, `e9d706a6`, `53b2f389`…`75450447`.
  - Azure: regenerate both keys of `RootManageSharedAccessKey` on `talecodeservicebus`, or delete the namespace.
- No `git filter-repo`, no force-push (D1, `CLAUDE.md §2`).

## Acceptance criteria
- [x] `git ls-files | grep -E "\.zip$|\.idea/|settings\.local\.json|\.vscode/settings\.json"` prints nothing.
- [x] `git check-ignore .idea/x .claude/settings.local.json` reports both as ignored.
- [ ] Owner confirmation with dates recorded for all 6 Google keys and the SAS key.
- [x] `dotnet build SolTechnology.Core.slnx` is green.

## Open questions
- none

## Deviations

### Waiting for owner revocation

**Original plan:** the step closes once the owner records the revocation dates.

**Actual implementation:** the tree change is committed (both zips removed, personal settings untracked, `.gitignore` extended, build green). The step stays `in-progress` until the owner confirms revocation of the 6 Google keys and the `talecodeservicebus` SAS key.
