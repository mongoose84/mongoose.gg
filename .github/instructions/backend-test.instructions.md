---
applyTo: "server/Mongoose.Api.Tests/**/*.cs"
description: "Backend test guidance for xUnit integration and unit tests in the Mongoose.Api.Tests project. Use when writing or editing backend tests, test infrastructure, or endpoint coverage in server/Mongoose.Api.Tests/."
---
# Backend Test Guidelines

## Context Loading
Review these BEFORE writing backend tests:
- [Test Strategy Spec](../specs/test-strategy.spec.md) — testing pyramid, backend coverage priorities, infrastructure expectations
- [Architecture Spec](../specs/architecture.spec.md) — endpoint routes, auth behavior, DTOs, and response contracts
- The source file under test — understand the code path, branches, and edge cases before adding tests
- Existing tests in `server/Mongoose.Api.Tests/` — match naming, setup, and assertion style

## Backend Test Stack

- **Frameworks**: xUnit + FluentAssertions
- **Project**: `.NET 10`
- **Primary infrastructure**: `TestWebApplicationFactory`

## Endpoint Integration Tests

Every protected endpoint test suite should cover these cases unless the endpoint contract makes one inapplicable:

1. Happy path — authenticated user gets expected result
2. 401 Unauthorized — unauthenticated request is rejected
3. 403 Forbidden — authenticated user cannot access another user's data
4. 404 Not Found — missing Riot account or missing resource returns the expected not-found response

### Standard Pattern
```csharp
public class MyEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public MyEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ReturnsData_WhenAuthenticated()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 1);

        var response = await client.GetAsync("/api/v2/resource/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## Test Infrastructure Rules

- Use `CreateClient()` for unauthenticated requests
- Use `CreateAuthenticatedClient(userId)` for session-authenticated requests
- Reuse seeded fake repositories from `TestWebApplicationFactory` where possible
- Use `[Theory]` and `[InlineData]` for parameterized edge cases
- Assert with `FluentAssertions`, not raw `Assert`
- Prefer deterministic assertions; avoid `Thread.Sleep` and timing-sensitive tests

## Unit And Service Tests

- Test behavior and outputs, not private implementation details
- Cover both nominal and edge-case inputs
- Keep each `[Fact]` or `[Theory]` focused on one behavior
- Use descriptive test names such as `Get_Returns403_WhenAccessingOtherUsersData`
- Add mapper, helper, service, job, and repository tests when logic is non-trivial

## Quality Checklist

- [ ] Tests follow the style of nearby files in `server/Mongoose.Api.Tests/`
- [ ] Endpoint tests use `TestWebApplicationFactory` when exercising HTTP behavior
- [ ] Assertions verify status code and meaningful response content
- [ ] Parameterized edge cases use `[Theory]` where appropriate
- [ ] New or changed backend behavior has corresponding test coverage