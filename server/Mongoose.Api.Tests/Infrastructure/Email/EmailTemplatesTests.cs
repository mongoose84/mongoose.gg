using Mongoose.Api.Infrastructure.Email;
using Xunit;

namespace Mongoose.Api.Tests;

public class EmailTemplatesTests
{
    [Fact]
    public void Verification_ContainsSubjectTitleAndCode()
    {
        var email = EmailTemplates.Verification("Faker", "123456", hasLogo: true, year: 2026);

        Assert.Equal("Verify your email for Mongoose.gg", email.Subject);
        Assert.Contains("Verify your email</h1>", email.Html);
        Assert.Contains(">123456</div>", email.Html);
        Assert.Contains("VERIFICATION CODE", email.Html);
        Assert.Contains("link your Riot ID", email.Html);
        Assert.Contains("Verification code: 123456", email.Text);
        Assert.Contains("Hi Faker, enter this code", email.Text);
    }

    [Fact]
    public void PasswordReset_ContainsSubjectTitleAndCode()
    {
        var email = EmailTemplates.PasswordReset("Faker", "654321", hasLogo: true, year: 2026);

        Assert.Equal("Reset your Mongoose.gg password", email.Subject);
        Assert.Contains("Reset your password</h1>", email.Html);
        Assert.Contains(">654321</div>", email.Html);
        Assert.Contains("Your password stays the same.", email.Html);
        Assert.Contains("Reset code: 654321", email.Text);
        Assert.DoesNotContain("Riot ID", email.Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Both_StateTheCodeLifetime(bool reset)
    {
        var email = reset
            ? EmailTemplates.PasswordReset("user", "111111", hasLogo: false)
            : EmailTemplates.Verification("user", "111111", hasLogo: false);

        var expiry = $"The code expires in {EmailTemplates.CodeLifetimeMinutes} minutes.";
        Assert.Contains(expiry, email.Html);
        Assert.Contains(expiry, email.Text);
    }

    [Fact]
    public void Html_EncodesUserControlledValues()
    {
        var email = EmailTemplates.Verification("<script>alert(1)</script>", "<b>1</b>", hasLogo: false);

        Assert.DoesNotContain("<script>", email.Html);
        Assert.Contains("&lt;script&gt;", email.Html);
        Assert.DoesNotContain("<b>1</b>", email.Html);
    }

    [Fact]
    public void Logo_IsEmbeddedOnlyWhenAvailable()
    {
        Assert.Contains("cid:logo", EmailTemplates.Verification("user", "1", hasLogo: true).Html);
        Assert.DoesNotContain("cid:logo", EmailTemplates.Verification("user", "1", hasLogo: false).Html);
    }

    [Fact]
    public void Html_UsesDesignSystemColoursAndNoLegacyBranding()
    {
        var html = EmailTemplates.PasswordReset("user", "1", hasLogo: true, year: 2026).Html;

        Assert.Contains("#0a0810", html); // bg
        Assert.Contains("#120f19", html); // surface card
        Assert.Contains("#1a1328", html); // highlight box around the code
        Assert.DoesNotContain("Beta", html);
        Assert.DoesNotContain("#1a1a1a", html);
        Assert.Contains("Not affiliated with Riot Games. © 2026 Mongoose.gg", html);
    }

    [Fact]
    public void Html_LoadsBrandFontsFromTheSite()
    {
        var html = EmailTemplates.Verification("user", "1", hasLogo: false).Html;

        Assert.Equal("https://beta.mongoose.gg/fonts", EmailTemplates.FontBaseUrl);
        Assert.Contains("url('https://beta.mongoose.gg/fonts/ClashDisplay-Bold.woff2')", html);
        Assert.Contains("url('https://beta.mongoose.gg/fonts/Satoshi-Regular.woff2')", html);
    }
}
