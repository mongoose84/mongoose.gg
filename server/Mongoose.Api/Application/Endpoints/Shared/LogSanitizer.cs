using System.Security.Cryptography;
using System.Text;

namespace Mongoose.Api.Application.Endpoints.Shared;

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

    /// <summary>
    /// Creates a non-reversible, deterministic hash representation for log fields.
    /// Use this for identifiers that should not be persisted in clear text.
    /// </summary>
    /// <param name="input">The potentially sensitive value to represent in logs</param>
    /// <param name="emptyValue">Value to return when input is null/empty</param>
    /// <returns>Stable hash token suitable for logs, or emptyValue when input is missing</returns>
    public static string HashForLog(string? input, string emptyValue = "empty")
    {
        if (string.IsNullOrWhiteSpace(input))
            return emptyValue;

        var sanitized = Sanitize(input);
        if (string.IsNullOrEmpty(sanitized))
            return emptyValue;

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sanitized));
        return $"sha256:{Convert.ToHexStringLower(hash[..8])}";
    }
}

