# Testing kits — `src/SolTechnology.Core.*.Testing/`

- Fixture method renamed or made async-only → consumer test suites stop compiling; no test in
  this repository may cover the kit itself.
- Container image, tag, or Testcontainers version bump → consumer CI pulls a different image or
  breaks on Docker hosts without the new prerequisites (Ryuk, ports).
- Static or shared fixture state → parallel test classes interfere or leak containers.
- Stricter defaults (e.g. WireMock partial mapping off) → loose consumer tests start failing.
- Test host not disposed → port or container exhaustion in long consumer test runs.
- Vulnerable transitive pulled by the kit → consumer test projects fail restore with `NU190x`.
