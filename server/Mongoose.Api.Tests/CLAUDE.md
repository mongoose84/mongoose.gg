# Mongoose.Api.Tests — Local Context

> xUnit integration and unit test project for the Mongoose.gg backend.
> For repo-wide invariants see [CLAUDE.md](../../CLAUDE.md).
> For test rules, coverage expectations, and quality guidance see [backend-test.instructions.md](../../.github/instructions/backend-test.instructions.md).
> For the source code under test see [server/Mongoose.Api/CLAUDE.md](../Mongoose.Api/CLAUDE.md).

## Run Tests

```bash
# From server/ directory
dotnet test Mongoose.Api.Tests/                                                              # All tests
dotnet test Mongoose.Api.Tests/ --filter "FullyQualifiedName~LoginEndpoint"                  # Single class
```

## Test Infrastructure

Key shared files — read these before adding new tests:

- `TestWebApplicationFactory.cs` — spins up an in-process server with fake repositories and configurable services. Entry point for all HTTP-level tests.
- `AuthCookieTestHelper.cs` — helpers for creating authenticated HTTP clients (`CreateAuthenticatedClient(userId)`).
- `EnvironmentVariableScope.cs` — scoped env var overrides that restore original values on dispose. Use for tests that depend on configuration.
- `EnvIsolationCollection.cs` — xUnit collection fixture that prevents parallel execution of env-var-sensitive tests.

## Structure

All test files live flat in `server/Mongoose.Api.Tests/` — no subdirectories. File naming mirrors the class under test: `LoginEndpointTests.cs`, `AesEncryptorTests.cs`, etc.

## Notes

- Fake repositories are seeded in `TestWebApplicationFactory` — reuse existing fakes before adding new ones.
- Use `CreateClient()` for unauthenticated requests, `CreateAuthenticatedClient(userId)` for authenticated.
- Tests that manipulate env vars must join `EnvIsolationCollection` to prevent cross-test pollution.
