using Mongoose.Api.Infrastructure.Security;

namespace Mongoose.Api.Tests;

internal sealed partial class TestWebApplicationFactory
{
    /// <summary>
    /// Fake encryptor for testing that doesn't actually encrypt.
    /// Just passes through the value as-is (or with a simple marker).
    /// </summary>
    private sealed class FakeEncryptor : IEncryptor
    {
        public string Encrypt(string input) => $"encrypted:{input.ToLowerInvariant().Trim()}";
        public string EncryptPreserveCase(string input) => $"encrypted:{input.Trim()}";
        public string Decrypt(string encryptedInput) =>
            encryptedInput.StartsWith("encrypted:")
                ? encryptedInput.Substring("encrypted:".Length)
                : encryptedInput;
    }
}
