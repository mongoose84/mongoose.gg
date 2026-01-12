using RiotProxy.Infrastructure.Security;
using Xunit;

namespace RiotProxy.Tests;

public class AesEmailEncryptorTests
{
    private readonly string _validKey = AesEmailEncryptor.GenerateKey();

    [Fact]
    public void Encrypt_Decrypt_RoundTrip_ReturnsOriginalEmail()
    {
        // Arrange
        var encryptor = new AesEmailEncryptor(_validKey);
        var originalEmail = "test@example.com";

        // Act
        var encrypted = encryptor.Encrypt(originalEmail);
        var decrypted = encryptor.Decrypt(encrypted);

        // Assert
        Assert.Equal(originalEmail, decrypted);
    }

    [Fact]
    public void Encrypt_SameEmail_ProducesSameCiphertext()
    {
        // Arrange
        var encryptor = new AesEmailEncryptor(_validKey);
        var email = "deterministic@example.com";

        // Act
        var encrypted1 = encryptor.Encrypt(email);
        var encrypted2 = encryptor.Encrypt(email);

        // Assert - deterministic encryption means same input = same output
        Assert.Equal(encrypted1, encrypted2);
    }

    [Fact]
    public void Encrypt_DifferentEmails_ProduceDifferentCiphertexts()
    {
        // Arrange
        var encryptor = new AesEmailEncryptor(_validKey);
        var email1 = "user1@example.com";
        var email2 = "user2@example.com";

        // Act
        var encrypted1 = encryptor.Encrypt(email1);
        var encrypted2 = encryptor.Encrypt(email2);

        // Assert
        Assert.NotEqual(encrypted1, encrypted2);
    }

    [Fact]
    public void Encrypt_ProducesBase64Output()
    {
        // Arrange
        var encryptor = new AesEmailEncryptor(_validKey);
        var email = "base64test@example.com";

        // Act
        var encrypted = encryptor.Encrypt(email);

        // Assert - should be valid base64
        var bytes = Convert.FromBase64String(encrypted); // Throws if not valid base64
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void Decrypt_WithWrongKey_ThrowsException()
    {
        // Arrange
        var encryptor1 = new AesEmailEncryptor(_validKey);
        var differentKey = AesEmailEncryptor.GenerateKey();
        var encryptor2 = new AesEmailEncryptor(differentKey);
        var email = "wrongkey@example.com";

        // Act
        var encrypted = encryptor1.Encrypt(email);

        // Assert - decrypting with wrong key should fail
        Assert.ThrowsAny<Exception>(() => encryptor2.Decrypt(encrypted));
    }

    [Fact]
    public void Decrypt_InvalidCiphertext_ThrowsException()
    {
        // Arrange
        var encryptor = new AesEmailEncryptor(_validKey);
        var invalidCiphertext = "not-valid-base64-or-ciphertext!!!";

        // Assert
        Assert.ThrowsAny<Exception>(() => encryptor.Decrypt(invalidCiphertext));
    }

    [Fact]
    public void Decrypt_TamperedCiphertext_ReturnsGarbageOrThrows()
    {
        // Arrange
        var encryptor = new AesEmailEncryptor(_validKey);
        var email = "tampered@example.com";
        var encrypted = encryptor.Encrypt(email);

        // Tamper with the ciphertext (flip a character)
        var chars = encrypted.ToCharArray();
        chars[10] = chars[10] == 'A' ? 'B' : 'A';
        var tampered = new string(chars);

        // Act & Assert
        // Tampering may either throw an exception OR return garbage data
        // (AES-CBC without authentication doesn't guarantee tamper detection)
        try
        {
            var decrypted = encryptor.Decrypt(tampered);
            // If it doesn't throw, the result should NOT be the original email
            Assert.NotEqual(email, decrypted);
        }
        catch (Exception)
        {
            // Exception is also acceptable - tampering detected
            Assert.True(true);
        }
    }

    [Fact]
    public void GenerateKey_ReturnsValidBase64Key()
    {
        // Act
        var key = AesEmailEncryptor.GenerateKey();

        // Assert
        var bytes = Convert.FromBase64String(key);
        Assert.Equal(32, bytes.Length); // 256 bits for AES-256
    }

    [Fact]
    public void GenerateKey_ProducesUniqueKeys()
    {
        // Act
        var key1 = AesEmailEncryptor.GenerateKey();
        var key2 = AesEmailEncryptor.GenerateKey();

        // Assert
        Assert.NotEqual(key1, key2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidKey_ThrowsArgumentException(string invalidKey)
    {
        // Assert
        Assert.Throws<ArgumentException>(() => new AesEmailEncryptor(invalidKey));
    }

    [Fact]
    public void Encrypt_SpecialCharactersInEmail_RoundTripsCorrectly()
    {
        // Arrange
        var encryptor = new AesEmailEncryptor(_validKey);
        var email = "user+tag@sub.example.com";

        // Act
        var encrypted = encryptor.Encrypt(email);
        var decrypted = encryptor.Decrypt(encrypted);

        // Assert
        Assert.Equal(email, decrypted);
    }

    [Fact]
    public void Encrypt_UnicodeEmail_RoundTripsCorrectly()
    {
        // Arrange
        var encryptor = new AesEmailEncryptor(_validKey);
        var email = "用户@例子.公司";

        // Act
        var encrypted = encryptor.Encrypt(email);
        var decrypted = encryptor.Decrypt(encrypted);

        // Assert
        Assert.Equal(email, decrypted);
    }
}

