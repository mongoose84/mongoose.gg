# Mongoose.Api.Tests — Local Context

> xUnit integration and unit test project for the Mongoose.gg backend.
> For repo-wide invariants see [CLAUDE.md](../../CLAUDE.md).
> For the source code under test see [server/Mongoose.Api/CLAUDE.md](../Mongoose.Api/CLAUDE.md).

Load [test-strategy.spec.md](../../.github/specs/test-strategy.spec.md) only when changing backend test scope or infrastructure.
Load [architecture.spec.md](../../.github/specs/architecture.spec.md) only when the test depends on route, auth, or response-contract details.
Always read the source file under test and one nearby test file before adding coverage.

## Run Tests

```bash
# From server/ directory
dotnet test Mongoose.Api.Tests/                                                              # All tests
dotnet test Mongoose.Api.Tests/ --filter "FullyQualifiedName~LoginEndpoint"                  # Single class
```

## Test Stack

- Use xUnit with FluentAssertions.
- Use `TestWebApplicationFactory` for HTTP-level tests.
- Match nearby naming, setup, and assertion style in the same folder.

## Endpoint Coverage

- Protected endpoint suites should cover happy path, 401 unauthenticated, 403 forbidden, and 404 not found when those outcomes are part of the contract.
- Use `CreateClient()` for unauthenticated requests and `CreateAuthenticatedClient(userId)` for authenticated requests.
- Reuse seeded fake repositories and shared helpers instead of rebuilding fixtures.

## Test Infrastructure

Key shared files — read these before adding new tests:

- `TestSupport/TestWebApplicationFactory.cs` — spins up an in-process server with fake repositories and configurable services. Entry point for all HTTP-level tests.
- `TestSupport/AuthCookieTestHelper.cs` — helpers for creating authenticated HTTP clients (`CreateAuthenticatedClient(userId)`).
- `TestSupport/EnvironmentVariableScope.cs` — scoped env var overrides that restore original values on dispose. Use for tests that depend on configuration.
- `TestSupport/EnvIsolationCollection.cs` — xUnit collection fixture that prevents parallel execution of env-var-sensitive tests.
- `TestSupport/Fakes/` — shared fake repositories and test doubles.

## Structure

Test folders mirror the source tree in `server/Mongoose.Api/`: a test lives at the same relative path as the class it covers, named `<ClassUnderTest>Tests.cs` (e.g. `Infrastructure/Security/AesEncryptor.cs` → `Infrastructure/Security/AesEncryptorTests.cs`).

```
Application/Endpoints/<Area>/          HTTP-level endpoint tests (TestWebApplicationFactory)
Application/Services/, Extensions/     application orchestration
Core/Services[/Solo]/, Core/Entities/  pure domain-rule unit tests
Infrastructure/<Area>/                 jobs, Riot client/mappers, DB, email, security, WebSocket
TestSupport/                           factory, auth helper, env isolation, secrets gate
TestSupport/Fakes/                     shared fakes
```

All files keep the single `Mongoose.Api.Tests` namespace regardless of folder, so shared helpers need no extra `using` and `--filter "FullyQualifiedName~<Class>"` keeps working.

## Test Quality

- Test behavior and response shape, not private implementation details.
- Use `[Theory]` and `[InlineData]` for parameterized edge cases.
- Keep tests deterministic; avoid sleeps and fragile timing assumptions.
- Keep each test focused on one behavior and use descriptive names.
- Add service, repository, helper, and job tests when the logic is non-trivial.

## Notes

- Fake repositories are seeded in `TestWebApplicationFactory` — reuse existing fakes before adding new ones.
- Tests that manipulate env vars must join `EnvIsolationCollection` to prevent cross-test pollution.
