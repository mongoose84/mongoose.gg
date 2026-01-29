using FluentAssertions;
using RiotProxy.Core.Entities;
using Xunit;

namespace RiotProxy.Tests;

public class VerificationTokenTests
{
    [Fact]
    public void IsExpired_returns_false_for_future_expiry()
    {
        var token = new VerificationToken
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_returns_true_for_past_expiry()
    {
        var token = new VerificationToken
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };

        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsUsed_returns_false_when_UsedAt_is_null()
    {
        var token = new VerificationToken
        {
            UsedAt = null
        };

        token.IsUsed.Should().BeFalse();
    }

    [Fact]
    public void IsUsed_returns_true_when_UsedAt_has_value()
    {
        var token = new VerificationToken
        {
            UsedAt = DateTime.UtcNow
        };

        token.IsUsed.Should().BeTrue();
    }

    [Fact]
    public void IsValid_returns_true_when_not_expired_and_not_used()
    {
        var token = new VerificationToken
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            UsedAt = null
        };

        token.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_returns_false_when_expired()
    {
        var token = new VerificationToken
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            UsedAt = null
        };

        token.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_returns_false_when_used()
    {
        var token = new VerificationToken
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            UsedAt = DateTime.UtcNow
        };

        token.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_returns_false_when_both_expired_and_used()
    {
        var token = new VerificationToken
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            UsedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        token.IsValid.Should().BeFalse();
    }

    [Fact]
    public void TokenTypes_constants_have_correct_values()
    {
        TokenTypes.EmailVerification.Should().Be("email_verification");
        TokenTypes.PasswordReset.Should().Be("password_reset");
        TokenTypes.EmailChange.Should().Be("email_change");
    }
}

