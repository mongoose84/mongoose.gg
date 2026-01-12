using System.Security.Cryptography;
using System.Text;

namespace RiotProxy.Infrastructure.Security;

/// <summary>
/// AES-256 email encryptor with deterministic encryption for database lookups.
/// Uses HMAC-SHA256 to derive a consistent IV from the email, ensuring the same
/// email always produces the same ciphertext while maintaining security.
/// </summary>
public sealed class AesEmailEncryptor : IEmailEncryptor
{
    private readonly byte[] _encryptionKey;
    private readonly byte[] _ivKey;

    /// <summary>
    /// Creates a new AES email encryptor.
    /// </summary>
    /// <param name="encryptionKey">Base64-encoded 256-bit (32 byte) encryption key</param>
    /// <param name="ivKey">Base64-encoded key for IV derivation (can be same as encryption key or different)</param>
    public AesEmailEncryptor(string encryptionKey, string? ivKey = null)
    {
        if (string.IsNullOrWhiteSpace(encryptionKey))
            throw new ArgumentException("Encryption key is required", nameof(encryptionKey));

        _encryptionKey = Convert.FromBase64String(encryptionKey);
        if (_encryptionKey.Length != 32)
            throw new ArgumentException("Encryption key must be 256 bits (32 bytes)", nameof(encryptionKey));

        // Use separate key for IV derivation, or derive from encryption key
        _ivKey = string.IsNullOrWhiteSpace(ivKey)
            ? SHA256.HashData(_encryptionKey)
            : Convert.FromBase64String(ivKey);
    }

    public string Encrypt(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        // Normalize email for consistent encryption
        var normalizedEmail = email.ToLowerInvariant().Trim();
        var plaintext = Encoding.UTF8.GetBytes(normalizedEmail);

        // Derive deterministic IV from email using HMAC
        var iv = DeriveIv(normalizedEmail);

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

        // Combine IV + ciphertext for storage (IV is needed for decryption)
        var result = new byte[iv.Length + ciphertext.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(ciphertext, 0, result, iv.Length, ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string encryptedEmail)
    {
        if (string.IsNullOrWhiteSpace(encryptedEmail))
            throw new ArgumentException("Encrypted email cannot be empty", nameof(encryptedEmail));

        var combined = Convert.FromBase64String(encryptedEmail);
        if (combined.Length < 17) // 16 byte IV + at least 1 byte ciphertext
            throw new ArgumentException("Invalid encrypted email format", nameof(encryptedEmail));

        // Extract IV and ciphertext
        var iv = new byte[16];
        var ciphertext = new byte[combined.Length - 16];
        Buffer.BlockCopy(combined, 0, iv, 0, 16);
        Buffer.BlockCopy(combined, 16, ciphertext, 0, ciphertext.Length);

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var plaintext = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

        return Encoding.UTF8.GetString(plaintext);
    }

    /// <summary>
    /// Derives a deterministic 16-byte IV from the email using HMAC-SHA256.
    /// This ensures the same email always gets the same IV, making encryption deterministic.
    /// </summary>
    private byte[] DeriveIv(string normalizedEmail)
    {
        using var hmac = new HMACSHA256(_ivKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(normalizedEmail));
        // Take first 16 bytes for AES IV
        var iv = new byte[16];
        Buffer.BlockCopy(hash, 0, iv, 0, 16);
        return iv;
    }

    /// <summary>
    /// Generates a new random 256-bit encryption key.
    /// Use this to generate keys for configuration.
    /// </summary>
    public static string GenerateKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }
}

