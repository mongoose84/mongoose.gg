namespace Mongoose.Api.Tests;

/// <summary>
/// One <see cref="TestWebApplicationFactory"/> shared by every test in a class
/// (<c>IClassFixture&lt;SharedFactoryFixture&gt;</c>), so the host boots once instead of per test.
/// Use it ONLY for suites whose tests never seed, override, or read back fake state — the fakes are
/// mutable, so a shared factory lets one test's data leak into another.
/// </summary>
/// <remarks>
/// A public wrapper is needed because the factory is internal and xUnit test classes must be public.
/// </remarks>
public sealed class SharedFactoryFixture : IDisposable
{
    private Task<string>? _authCookie;

    internal TestWebApplicationFactory Factory { get; } = new();

    /// <summary>
    /// The seeded user's auth cookie, logged in once per class. Reuse it instead of calling
    /// <see cref="AuthCookieTestHelper.LoginAsync"/> per test: the shared host keeps the login rate
    /// limiter's state, so repeated logins start returning 429.
    /// </summary>
    internal Task<string> GetAuthCookieAsync() => _authCookie ??= AuthCookieTestHelper.LoginAsync(Factory);

    public void Dispose() => Factory.Dispose();
}
