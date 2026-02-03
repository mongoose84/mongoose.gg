using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Application.DTOs.Feedback;
using RiotProxy.Core.Interfaces;

namespace RiotProxy.Application.Endpoints.Feedback;

/// <summary>
/// Feedback Endpoint
/// Accepts structured user feedback (bugs and feature requests) and creates
/// corresponding GitHub issues in the internal backlog repository.
/// The client never sees GitHub tokens, repo names, or issue URLs.
/// </summary>
public sealed class FeedbackEndpoint : IEndpoint
{
    public string Route { get; }
    
    // Valid feedback types
    private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "bug",
        "feature"
    };
    
    // Maximum field lengths
    private const int MaxSummaryLength = 200;
    private const int MaxDetailsLength = 5000;
    private const int MaxRouteLength = 200;
    private const int MaxEnvironmentLength = 50;
    private const int MaxBrowserLength = 100;
    private const int MaxOsLength = 100;

    public FeedbackEndpoint(string basePath)
    {
        Route = basePath + "/feedback";
    }

    public void Configure(WebApplication app)
    {
        var endpoint = app.MapPost(Route, async (
            HttpContext httpContext,
            [FromBody] FeedbackDto.FeedbackRequest request,
            [FromServices] IGitHubService gitHubService,
            [FromServices] ILogger<FeedbackEndpoint> logger
        ) =>
        {
            try
            {
                // Extract user context if authenticated (optional - feedback can work without auth)
                long? userId = null;
                if (httpContext.User?.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (long.TryParse(userIdClaim, out var parsedUserId))
                    {
                        userId = parsedUserId;
                    }
                }

                // Validate type
                if (string.IsNullOrWhiteSpace(request.Type))
                {
                    return Results.BadRequest(new { error = "type is required" });
                }
                
                var normalizedType = request.Type.Trim().ToLowerInvariant();
                if (!ValidTypes.Contains(normalizedType))
                {
                    return Results.BadRequest(new { error = "type must be 'bug' or 'feature'" });
                }

                // Validate summary
                if (string.IsNullOrWhiteSpace(request.Summary))
                {
                    return Results.BadRequest(new { error = "summary is required" });
                }
                
                if (request.Summary.Length > MaxSummaryLength)
                {
                    return Results.BadRequest(new { error = $"summary must be at most {MaxSummaryLength} characters" });
                }

                // Validate details based on type
                if (normalizedType == "bug" && string.IsNullOrWhiteSpace(request.Details))
                {
                    return Results.BadRequest(new { error = "details is required for bug reports" });
                }
                
                if (!string.IsNullOrEmpty(request.Details) && request.Details.Length > MaxDetailsLength)
                {
                    return Results.BadRequest(new { error = $"details must be at most {MaxDetailsLength} characters" });
                }

                // Validate optional fields
                if (!string.IsNullOrEmpty(request.Route) && request.Route.Length > MaxRouteLength)
                {
                    return Results.BadRequest(new { error = $"route must be at most {MaxRouteLength} characters" });
                }
                
                if (!string.IsNullOrEmpty(request.Environment) && request.Environment.Length > MaxEnvironmentLength)
                {
                    return Results.BadRequest(new { error = $"environment must be at most {MaxEnvironmentLength} characters" });
                }
                
                if (!string.IsNullOrEmpty(request.Browser) && request.Browser.Length > MaxBrowserLength)
                {
                    return Results.BadRequest(new { error = $"browser must be at most {MaxBrowserLength} characters" });
                }
                
                if (!string.IsNullOrEmpty(request.Os) && request.Os.Length > MaxOsLength)
                {
                    return Results.BadRequest(new { error = $"os must be at most {MaxOsLength} characters" });
                }

                // Build GitHub issue title
                var titlePrefix = normalizedType == "bug" ? "[Bug]" : "[Feature Request]";
                var issueTitle = $"{titlePrefix} {request.Summary.Trim()}";

                // Build GitHub issue body
                var issueBody = BuildIssueBody(request, normalizedType, userId);

                // Determine labels
                var labels = normalizedType == "bug"
                    ? new[] { "bug", "user-feedback" }
                    : new[] { "enhancement", "user-feedback" };

                // Create GitHub issue
                var result = await gitHubService.CreateIssueAsync(issueTitle, issueBody, labels);

                if (!result.Success)
                {
                    logger.LogWarning("Failed to create GitHub issue: {Error}", result.ErrorMessage);
                    return Results.Json(
                        new { error = "Unable to submit feedback. Please try again later." },
                        statusCode: 503);
                }

                logger.LogInformation(
                    "Feedback submitted successfully. Type: {Type}, UserId: {UserId}",
                    normalizedType, userId?.ToString() ?? "anonymous");

                return Results.Accepted(value: new FeedbackDto.FeedbackResponse(
                    Success: true,
                    Message: "Thank you for your feedback!"
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in FeedbackEndpoint");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });

        // No authorization required - allow anonymous feedback
        // However, if user is authenticated, we capture their userId
    }

    /// <summary>
    /// Builds the GitHub issue body in markdown format.
    /// </summary>
    private static string BuildIssueBody(
        FeedbackDto.FeedbackRequest request,
        string normalizedType,
        long? userId)
    {
        var sb = new StringBuilder();

        // Description section
        sb.AppendLine("## Description");
        sb.AppendLine();
        sb.AppendLine(request.Details ?? "_No details provided_");
        sb.AppendLine();

        // Metadata section
        sb.AppendLine("## Metadata");
        sb.AppendLine();
        sb.AppendLine($"| Field | Value |");
        sb.AppendLine($"|-------|-------|");
        sb.AppendLine($"| **Type** | {normalizedType} |");

        if (!string.IsNullOrWhiteSpace(request.Route))
        {
            sb.AppendLine($"| **Route** | `{request.Route}` |");
        }

        if (!string.IsNullOrWhiteSpace(request.Environment))
        {
            sb.AppendLine($"| **Environment** | {request.Environment} |");
        }

        if (!string.IsNullOrWhiteSpace(request.Browser))
        {
            sb.AppendLine($"| **Browser** | {request.Browser} |");
        }

        if (!string.IsNullOrWhiteSpace(request.Os))
        {
            sb.AppendLine($"| **OS** | {request.Os} |");
        }

        if (userId.HasValue)
        {
            sb.AppendLine($"| **User ID** | {userId.Value} |");
        }
        else
        {
            sb.AppendLine($"| **User ID** | _anonymous_ |");
        }

        sb.AppendLine($"| **Submitted At** | {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC |");

        return sb.ToString();
    }
}

