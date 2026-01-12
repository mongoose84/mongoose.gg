namespace RiotProxy.Infrastructure.Security;

/// <summary>
/// Service for encrypting and decrypting email addresses.
/// Uses deterministic encryption so the same email always produces the same ciphertext,
/// enabling efficient database lookups.
/// </summary>
public interface IEmailEncryptor
{
    /// <summary>
    /// Encrypts an email address. The same email will always produce the same ciphertext.
    /// </summary>
    /// <param name="email">The plaintext email address</param>
    /// <returns>Base64-encoded encrypted email</returns>
    string Encrypt(string email);

    /// <summary>
    /// Decrypts an encrypted email address back to plaintext.
    /// </summary>
    /// <param name="encryptedEmail">Base64-encoded encrypted email</param>
    /// <returns>The plaintext email address</returns>
    string Decrypt(string encryptedEmail);
}

