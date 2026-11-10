# Delivery — multi-step plans and multi-session work

- Long-running agent or session cancelled mid-step → uncommitted work and step state are lost, or a
  half-done step looks done. Commit per step, set the step status in the same commit, and keep the
  feature buildable after each commit.
- Step that waits for an owner action (revocation, environment, reviewer, a setting that needs
  admin rights) → the owner may decline or delay it. Plan an automated fallback where one exists
  (a tag guard instead of a required reviewer), keep the step `in-progress`, and record the
  owner's answer verbatim with its date.
- Contract row numbers (`C-nn`, `P-nn`, `L-nn`) reserved by a plan that is later abandoned → gaps
  that release notes misread as lost rows. Record which numbers are reserved and by whom.
- Cross-cutting page or shared doc left to a later retrospective → it describes the old state for
  weeks (a version, a publish trigger). Update it in the change that alters the behaviour.
- Result checked by reading code or docs only → it fails for the first user. Verify by a real run
  and run each check once against the old revision to see it fail.
