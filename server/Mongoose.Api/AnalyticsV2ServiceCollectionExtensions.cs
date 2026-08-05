// Dependency Injection Configuration for Analytics V2
// Add to Program.cs or IServiceCollection extension method

using Mongoose.Api.Application.Endpoints.Analytics;
using Mongoose.Api.Core.Interfaces;
using Mongoose.Api.Infrastructure.Database;
using Mongoose.Api.Infrastructure.Database.Repositories;
using Mongoose.Api.Infrastructure.Telemetry;

namespace Mongoose.Api;

/// <summary>
/// Extension method to register Analytics V2 services
/// Call: services.AddAnalyticsV2Services(configuration);
/// </summary>
public static class AnalyticsV2ServiceCollectionExtensions
{
    public static IServiceCollection AddAnalyticsV2Services(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register Event Schema Registry (YAML loader)
        var schemaPath = Path.Combine(
            AppContext.BaseDirectory,
            "Application/Telemetry/event-schema.yml");
        
        services.AddSingleton<IEventSchemaRegistry>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<EventSchemaRegistry>>();
            var registry = new EventSchemaRegistry(logger, schemaPath);
            // Load schema synchronously at startup
            registry.ReloadAsync().GetAwaiter().GetResult();
            return registry;
        });

        // Register Event Validator
        services.AddSingleton<IEventValidator>(sp =>
        {
            var registry = sp.GetRequiredService<IEventSchemaRegistry>();
            var logger = sp.GetRequiredService<ILogger<EventValidator>>();
            return new EventValidator(registry, logger);
        });

        // Register V2 Repository
        services.AddScoped<IAnalyticsEventsV2Repository>(sp =>
        {
            var factory = sp.GetRequiredService<IDbConnectionFactory>();
            return new AnalyticsEventsV2Repository(factory);
        });

        // Register V2 Endpoint (already handled by IEndpoint auto-discovery)
        // But if manual: services.AddScoped<AnalyticsEndpointV2>();

        return services;
    }
}

/*
 * Integration Example in Program.cs:
 * 
 *   var builder = WebApplicationBuilder.CreateBuilder(args);
 *   
 *   // ... other services ...
 *   
 *   // Add Analytics V2
 *   builder.Services.AddAnalyticsV2Services(builder.Configuration);
 *   
 *   // Continue with app build
 *   var app = builder.Build();
 *   
 *   // ... endpoint registration ...
 *   
 *   app.Run();
 */
