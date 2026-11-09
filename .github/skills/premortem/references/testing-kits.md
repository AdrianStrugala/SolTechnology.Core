# Testing kits — `src/SolTechnology.Core.*.Testing/`

- Fixture method renamed or made async-only → consumer test suites stop compiling; no test in
  this repository may cover the kit itself.
- Container image, tag, or Testcontainers version bump → consumer CI pulls a different image or
  breaks on Docker hosts without the new prerequisites (Ryuk, ports).
- Static or shared fixture state → parallel test classes interfere or leak containers.
- Stricter defaults (e.g. WireMock partial mapping off) → loose consumer tests start failing.
- Test host not disposed → port or container exhaustion in long consumer test runs.
- Vulnerable transitive pulled by the kit → consumer test projects fail restore with `NU190x`.
- Process-wide Testcontainers setting changed in code (e.g. `TestcontainersSettings.ResourceReaperEnabled`
  in a static constructor) → every fixture in the test process inherits it; with Ryuk off a
  `WithCleanUp(true)` resource has no reaper session id, so dispose only stops it and stopped containers
  pile up.
- A Testcontainers module creates helper resources (the Service Bus emulator's MSSQL sidecar and network)
  → disposing the main container does not remove them; the fixture must create, own and dispose them,
  and must attempt every removal even when one fails.
- Stricter default shipped on one entry point only (WireMock strict matching on `WireMockStartup` but not
  `WireMockFixture`) → consumers of the other entry point have no opt-out; also expect hidden stub bugs
  (WireMock splits query values on commas) in the repository's own consumers.
- Fixture method renamed → also check the package `<Description>` (NuGet metadata), module pages that are
  not the kit's own, and `.github` references.
