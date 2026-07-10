using Mongoose.Api.Application.Endpoints.Analytics;
using Mongoose.Api.Infrastructure.Services.Analytics;
using Microsoft.Extensions.DependencyInjection;

namespace Mongoose.Api;

/// <summary>
/// Dependency Injection Extension for Analytics Phase 2 (Async Ingestion)
/// 
/// Wire up:
/// - Queue processor (background service)
/// - Abuse guards (rate limiting)
/// - Pipeline monitor (metrics collection)
/// - Async endpoint
/// 
/// Call: services.AddAnalyticsPhase2Services();
/// </summary>
public static class AnalyticsPhase2ServiceCollectionExtensions
{
  public static IServiceCollection AddAnalyticsPhase2Services(
    this IServiceCollection services,
    AnalyticsQueueOptions? queueOptions = null,
    AnalyticsAbuseGuardsOptions? abuseOptions = null)
  {
    // Register queue processor (background service)
    services.AddSingleton(queueOptions ?? new AnalyticsQueueOptions());
    services.AddHostedService<AnalyticsQueueProcessor>();
    services.AddSingleton<IAnalyticsQueueProcessor>(sp =>
      sp.GetRequiredService<AnalyticsQueueProcessor>());
    
    // Register abuse guards (rate limiting)
    services.AddSingleton(abuseOptions ?? new AnalyticsAbuseGuardsOptions());
    services.AddSingleton<IAnalyticsAbuseGuards>(sp =>
      new AnalyticsAbuseGuards(
        sp.GetRequiredService<IMemoryCache>(),
        sp.GetRequiredService<ILogger<AnalyticsAbuseGuards>>(),
        sp.GetRequiredService<AnalyticsAbuseGuardsOptions>()));
    
    // Register pipeline monitor
    services.AddSingleton<IAnalyticsPipelineMonitor>(sp =>
      new AnalyticsPipelineMonitor(
        sp.GetRequiredService<ILogger<AnalyticsPipelineMonitor>>()));
    
    // Async endpoint already auto-discovered via IEndpoint
    
    return services;
  }
}

/*
 * Integration Example in Program.cs:
 * 
 *   var builder = WebApplicationBuilder.CreateBuilder(args);
 *   
 *   // Add Phase 1 (versioned schema + validation)
 *   builder.Services.AddAnalyticsV2Services(builder.Configuration);
 *   
 *   // Add Phase 2 (async ingestion + queue)
 *   builder.Services.AddAnalyticsPhase2Services(
 *     queueOptions: new AnalyticsQueueOptions { WorkerCount = 4, MaxQueueDepth = 10000 },
 *     abuseOptions: new AnalyticsAbuseGuardsOptions { MaxEventsPerIpPerWindow = 1000 });
 *   
 *   var app = builder.Build();
 *   // ...
 *   app.Run();
 */
