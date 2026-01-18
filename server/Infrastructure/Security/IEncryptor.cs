namespace RiotProxy.Infrastructure.Security;

/// <summary>
/// Service for encrypting and decrypting strings.
/// Uses deterministic encryption so the same input always produces the same ciphertext,
/// enabling efficient database lookups.
/// </summary>
public interface IEncryptor
{
    /// <summary>
    /// Encrypts a string with normalization (lowercase + trim).
    /// The same input will always produce the same ciphertext.
    /// Use for emails where case-insensitivity is desired.
    /// </summary>
    /// <param name="input">The plaintext string</param>
    /// <returns>Base64-encoded encrypted string</returns>
    string Encrypt(string input);

    /// <summary>
    /// Encrypts a string preserving original case.
    /// Uses normalized version for IV derivation (enabling case-insensitive lookups)
    /// but stores the original casing in the ciphertext.
    /// Use for usernames where display casing should be preserved.
    /// </summary>
    /// <param name="input">The plaintext string</param>
    /// <returns>Base64-encoded encrypted string</returns>
    string EncryptPreserveCase(string input);

    /// <summary>
    /// Decrypts an encrypted string back to plaintext.
    /// </summary>
    /// <param name="encryptedInput">Base64-encoded encrypted string</param>
    /// <returns>The plaintext string</returns>
    string Decrypt(string encryptedInput);
}

