using System;
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Mongoose.Api.Application.Endpoints;
using Mongoose.Api.Application.Endpoints.Auth;
using Mongoose.Api.Application.Extensions;
using Xunit;

namespace Mongoose.Api.Tests;

public sealed class EndpointDiscoveryExtensionTests
{
    [Fact]
    public void DiscoverEndpoints_ReturnsDiscoveredEndpoints_WithConfiguredBasePath()
    {
        const string basePath = "/api/v2";

        var endpoints = EndpointDiscoveryExtension.DiscoverEndpoints(basePath);

        endpoints.Should().NotBeEmpty();
        endpoints.Should().Contain(e => e.GetType() == typeof(LoginEndpoint));
        endpoints.Should().Contain(e => e.Route == "/");
        endpoints
            .Where(e => e.Route != "/")
            .Should().OnlyContain(e => e.Route.StartsWith(basePath, StringComparison.Ordinal));
    }

    [Fact]
    public void DiscoverEndpoints_Throws_WhenEndpointDoesNotHaveStringConstructor()
    {
        Action act = () => EndpointDiscoveryExtension.DiscoverEndpoints(
            "/api/v2",
            typeof(InvalidConstructorEndpoint).Assembly);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must have a constructor that accepts a string (basePath) parameter*");
    }

    [Fact]
    public async Task ApplicationStartup_MapsDiscoveredRoutes()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/v2/users/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed class InvalidConstructorEndpoint : IEndpoint
    {
        public string Route => "/invalid";

        public void Configure(WebApplication app)
        {
        }
    }
}
