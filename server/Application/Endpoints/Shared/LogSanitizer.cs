namespace RiotProxy.Application.Endpoints.Shared;

/// <summary>
/// Utility class for sanitizing user input before logging.
/// Prevents log injection/forgery attacks by removing control characters.
/// </summary>
public static class LogSanitizer
{
    /// <summary>
    /// Sanitizes input for safe logging by removing newlines and control characters.
    /// Prevents log injection/forgery attacks where malicious users could craft inputs
    /// containing newlines to forge log entries or corrupt log files.
    /// </summary>
    /// <param name="input">The potentially unsafe user input</param>
    /// <returns>Sanitized string safe for logging, or empty string if input is null/empty</returns>
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return input
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace("\t", " ");
    }
}

