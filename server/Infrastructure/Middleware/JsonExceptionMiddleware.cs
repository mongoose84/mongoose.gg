using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mongoose.Api.Infrastructure.Middleware
{
    /// <summary>
    /// Global exception handler middleware for consistent error responses.
    /// Returns generic error messages to clients (security) while logging full details for debugging.
    /// </summary>
    public class JsonExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JsonExceptionMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public JsonExceptionMiddleware(
            RequestDelegate next,
            ILogger<JsonExceptionMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// Handles exceptions by returning generic error messages to clients.
        /// In development, includes detailed error information for debugging.
        /// In production, only returns generic message for security.
        /// </summary>
        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Generate correlation ID for tracking
            var correlationId = context.TraceIdentifier;

            // Log full exception details for debugging (always)
            _logger.LogError(exception,
                "Unhandled exception occurred. CorrelationId={CorrelationId}",
                correlationId);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // Create error response - different for dev vs production
            object errorResponse = _environment.IsDevelopment()
                ? new
                {
                    error = "An unexpected error occurred.",
                    message = exception.Message,
                    details = exception.GetType().Name,
                    correlationId = correlationId,
                    stackTrace = exception.StackTrace
                }
                : new
                {
                    error = "An unexpected error occurred. Please try again later.",
                    correlationId = correlationId
                };

            var json = JsonSerializer.Serialize(errorResponse);
            return context.Response.WriteAsync(json);
        }
    }
}

