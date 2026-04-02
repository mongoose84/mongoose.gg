using System.Security.Cryptography;

namespace Mongoose.Api.Infrastructure.Email;

/// <summary>
/// Generates cryptographically secure verification codes
/// </summary>
public static class VerificationCodeGenerator
{
    /// <summary>
    /// Generate a 6-digit verification code using cryptographically secure random number generation
    /// </summary>
    public static string Generate()
    {
        // Generate a random number between 0 and 999999
        var randomNumber = RandomNumberGenerator.GetInt32(0, 1000000);
        // Pad with leading zeros to ensure 6 digits
        return randomNumber.ToString("D6");
    }
}

