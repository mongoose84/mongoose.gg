using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Mongoose.Api.Tests;

public class SoloPerformanceEndpointTests
{
    [Fact]
    public async Task Solo_performance_requires_authentication()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/solo/dashboard/1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

