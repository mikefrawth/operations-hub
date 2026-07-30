# Test projects guide

`OperationsHub.UnitTests` covers deterministic Domain and Application behavior without network, filesystem, or database dependencies. `OperationsHub.IntegrationTests` covers the assembled web host and real MySQL behavior.

- Mirror production namespaces or features so ownership is obvious.
- Give tests sentence-like names that state the behavior and outcome; avoid underscores because repository analyzers enforce public-member naming.
- Keep arrange/act/assert sections readable without comments that repeat the code.
- Tests must be deterministic and isolated; use UTC and controlled clocks for time-dependent rules.
- Do not weaken assertions to accommodate broken behavior.
- Do not use EF Core InMemory for database semantics.
- Cover authorization, antiforgery, and rate-limit metadata when adding or changing protected endpoints.
- Container-backed tests must clean up their own data and explain environmental prerequisites.

Run:

```powershell
.\eng\dotnet.cmd test
.\eng\dotnet.cmd test tests/OperationsHub.UnitTests
.\eng\dotnet.cmd test tests/OperationsHub.IntegrationTests
```
