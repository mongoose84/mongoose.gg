using FluentAssertions;
using RiotProxy.Infrastructure.Email;
using Xunit;

namespace RiotProxy.Tests;

public class VerificationCodeGeneratorTests
{
    [Fact]
    public void Generate_returns_six_digit_string()
    {
        var code = VerificationCodeGenerator.Generate();
        
        code.Should().HaveLength(6);
        code.Should().MatchRegex(@"^\d{6}$");
    }

    [Fact]
    public void Generate_pads_with_leading_zeros()
    {
        // Generate many codes to test that small numbers are properly padded
        var codes = new HashSet<string>();
        for (int i = 0; i < 1000; i++)
        {
            codes.Add(VerificationCodeGenerator.Generate());
        }

        // All codes should be exactly 6 digits
        codes.Should().OnlyContain(code => code.Length == 6);
    }

    [Fact]
    public void Generate_produces_different_codes()
    {
        // Generate multiple codes and verify they're not all the same
        var codes = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            codes.Add(VerificationCodeGenerator.Generate());
        }

        // Should have generated at least some unique codes
        // (statistically, getting 100 identical codes is virtually impossible)
        codes.Count.Should().BeGreaterThan(50);
    }

    [Fact]
    public void Generate_returns_only_numeric_characters()
    {
        for (int i = 0; i < 100; i++)
        {
            var code = VerificationCodeGenerator.Generate();
            code.Should().MatchRegex(@"^[0-9]+$", "code should contain only digits");
        }
    }
}

