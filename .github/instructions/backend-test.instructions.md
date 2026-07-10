# Backend Test Rules

> Scope: `server/Mongoose.Api.Tests/**/*.cs`

Load [test-strategy.spec.md](../specs/test-strategy.spec.md) only when changing backend test scope or infrastructure.
Load [architecture.spec.md](../specs/architecture.spec.md) only when the test depends on route, auth, or response-contract details.
Always read the source file under test and one nearby test file before adding coverage.

## Test Stack

- Use xUnit with FluentAssertions.
- Use `TestWebApplicationFactory` for HTTP-level tests.
- Match nearby naming, setup, and assertion style in `server/Mongoose.Api.Tests/`.

## Endpoint Coverage

- Protected endpoint suites should cover happy path, 401 unauthenticated, 403 forbidden, and 404 not found when those outcomes are part of the contract.
- Use `CreateClient()` for unauthenticated requests and `CreateAuthenticatedClient(userId)` for authenticated requests.
- Reuse seeded fake repositories and shared helpers instead of rebuilding fixtures.

## Test Quality

- Test behavior and response shape, not private implementation details.
- Use `[Theory]` and `[InlineData]` for parameterized edge cases.
- Keep tests deterministic; avoid sleeps and fragile timing assumptions.
- Keep each test focused on one behavior and use descriptive names.
- Add service, repository, helper, and job tests when the logic is non-trivial.