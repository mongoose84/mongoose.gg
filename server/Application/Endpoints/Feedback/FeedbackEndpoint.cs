using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Mongoose.Api.Application.DTOs.Feedback;
using Mongoose.Api.Application.Endpoints.Shared;
using Mongoose.Api.Core.Interfaces;

namespace Mongoose.Api.Application.Endpoints.Feedback;

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

    // Rate limiting configuration
    private const int RateLimitRequests = 5;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromHours(1);

    /// <summary>
    /// Sanitizes user input for safe inclusion in Markdown content.
    /// Escapes Markdown syntax characters to prevent formatting injection.
    /// </summary>
    private static string SanitizeMarkdownContent(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Escape Markdown syntax characters that could be used for injection:
        // - # for headers
        // - * and _ for bold/italic
        // - [ ] for links/images
        // - ` for code
        // - > for blockquotes
        // - - and + for lists
        // - | for tables
        var result = input
            .Replace("\\", "\\\\")  // Escape backslashes first
            .Replace("#", "\\#")
            .Replace("*", "\\*")
            .Replace("_", "\\_")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("`", "\\`")
            .Replace(">", "\\>")
            .Replace("|", "\\|")
            .Replace("-", "\\-")
            .Replace("+", "\\+");

        // Also escape lines that start with numbers followed by period (ordered lists)
        // This is a simple approach - replace period after digits at line start
        var lines = result.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.Length > 0 && char.IsDigit(trimmed[0]))
            {
                var dotIndex = trimmed.IndexOf('.');
                if (dotIndex > 0 && dotIndex < 4) // e.g., "1.", "12.", "123."
                {
                    lines[i] = lines[i].Replace(".", "\\.");
                }
            }
        }

        return string.Join('\n', lines);
    }

    /// <summary>
    /// Escapes user input for safe inclusion in Markdown table cells.
    /// Prevents Markdown injection that could break table formatting.
    /// </summary>
    private static string EscapeMarkdownTableCell(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Escape characters that could break Markdown table formatting:
        // - | (pipe) is the table cell delimiter
        // - \ (backslash) is the escape character
        // - newlines would break the table row
        return input
            .Replace("\\", "\\\\")  // Escape backslashes first
            .Replace("|", "\\|")    // Escape pipe characters
            .Replace("\r\n", " ")   // Replace Windows newlines with space
            .Replace("\r", " ")     // Replace old Mac newlines with space
            .Replace("\n", " ")     // Replace Unix newlines with space
            .Replace("\t", " ");    // Replace tabs with space
    }

    /// <summary>
    /// Extracts the client IP address from the HTTP context.
    /// Checks X-Forwarded-For header first (for proxies/load balancers),
    /// then falls back to the direct connection IP.
    /// </summary>
    private static string? GetClientIpAddress(HttpContext context)
    {
        // Check X-Forwarded-For header first (for proxies/load balancers)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs; the first is the original client
            return forwardedFor.Split(',')[0].Trim();
        }

        // Fall back to direct connection IP
        return context.Connection.RemoteIpAddress?.ToString();
    }

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
            [FromServices] IRateLimiter rateLimiter,
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
                

                // Check rate limit before processing
                var clientIp = GetClientIpAddress(httpContext);
                var rateLimitResult = await rateLimiter.CheckEndpointAsync(
                    "feedback",
                    clientIp,
                    userId,
                    RateLimitRequests,
                    RateLimitWindow);

                if (!rateLimitResult.IsAllowed)
                {
                    var sanitizedClientIp = LogSanitizer.Sanitize(clientIp);
                    var sanitizedUserId = userId.HasValue ? userId.Value.ToString() : "anonymous";

                    logger.LogWarning(
                        "Rate limit exceeded for feedback endpoint. IP: {IP}, UserId: {UserId}",
                        sanitizedClientIp ?? "unknown",
                        sanitizedUserId);

                    // Add rate limit headers
                    httpContext.Response.Headers["X-RateLimit-Remaining"] = "0";
                    if (rateLimitResult.RetryAfter.HasValue)
                    {
                        httpContext.Response.Headers["Retry-After"] =
                            ((int)rateLimitResult.RetryAfter.Value.TotalSeconds).ToString();
                    }

                    return Results.Json(
                        new { error = "Too many feedback submissions. Please try again later." },
                        statusCode: 429);
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
                // Sanitize summary for title - remove newlines and control characters
                var sanitizedSummary = LogSanitizer.Sanitize(request.Summary.Trim());
                var titlePrefix = normalizedType == "bug" ? "[Bug]" : "[Feature Request]";
                var issueTitle = $"{titlePrefix} {sanitizedSummary}";

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
                    logger.LogWarning("Failed to create GitHub issue: {Error}", LogSanitizer.Sanitize(result.ErrorMessage));
                    return Results.Json(
                        new { error = "Unable to submit feedback. Please try again later." },
                        statusCode: 503);
                }

                logger.LogInformation(
                    "Feedback submitted successfully. Type: {Type}, UserId: {UserId}",
                    LogSanitizer.Sanitize(normalizedType), userId?.ToString() ?? "anonymous");

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

        // Description section - sanitize user input to prevent Markdown injection
        sb.AppendLine("## Description");
        sb.AppendLine();
        string sanitizedDetails;
        if (string.IsNullOrWhiteSpace(request.Details))
        {
            // Provide context-appropriate placeholder based on feedback type
            sanitizedDetails = normalizedType == "feature"
                ? $"_See summary: {SanitizeMarkdownContent(request.Summary)}_"
                : "_No details provided_";
        }
        else
        {
            sanitizedDetails = SanitizeMarkdownContent(request.Details);
        }
        sb.AppendLine(sanitizedDetails);
        sb.AppendLine();

        // Metadata section
        sb.AppendLine("## Metadata");
        sb.AppendLine();
        sb.AppendLine($"| Field | Value |");
        sb.AppendLine($"|-------|-------|");
        sb.AppendLine($"| **Type** | {EscapeMarkdownTableCell(normalizedType)} |");

        if (!string.IsNullOrWhiteSpace(request.Route))
        {
            sb.AppendLine($"| **Route** | `{EscapeMarkdownTableCell(request.Route)}` |");
        }

        if (!string.IsNullOrWhiteSpace(request.Environment))
        {
            sb.AppendLine($"| **Environment** | {EscapeMarkdownTableCell(request.Environment)} |");
        }

        if (!string.IsNullOrWhiteSpace(request.Browser))
        {
            sb.AppendLine($"| **Browser** | {EscapeMarkdownTableCell(request.Browser)} |");
        }

        if (!string.IsNullOrWhiteSpace(request.Os))
        {
            sb.AppendLine($"| **OS** | {EscapeMarkdownTableCell(request.Os)} |");
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

