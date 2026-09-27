using System.Net;

namespace Mongoose.Api.Infrastructure.Email;

/// <summary>
/// HTML and plain-text bodies of an outgoing email.
/// </summary>
public sealed record EmailContent(string Subject, string Html, string Text);

/// <summary>
/// Builds the transactional emails in the Mongoose.gg design system:
/// dark ground (bg), one surface card, the code in the single highlight box,
/// Clash Display for the title and code, Satoshi for text, and plain "you" copy.
/// Email clients cannot load the web fonts reliably, so each family falls back to system fonts.
/// </summary>
public static class EmailTemplates
{
    /// <summary>Minutes a verification or reset code stays valid (matches the token expiry in the auth endpoints).</summary>
    public const int CodeLifetimeMinutes = 15;

    // Design-system tokens (.claude/skills/mongoose-design/reference/tokens.json)
    private const string Bg = "#0a0810";
    private const string Surface = "#120f19";
    private const string SurfaceHighlight = "#1a1328";
    private const string BorderHighlight = "#3b2a5c";
    private const string Divider = "#1f1929";
    private const string Ink = "#f1eef7";
    private const string InkSoft = "#cfc9da";
    private const string InkMuted = "#a39cb3";
    private const string InkFaint = "#8a839a";

    private const string DisplayFont = "'Clash Display', 'Satoshi', -apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif";
    private const string BodyFont = "'Satoshi', -apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif";

    /// <summary>
    /// Email sent after sign-up (and on resend) with the code that verifies the address.
    /// </summary>
    public static EmailContent Verification(string username, string code, bool hasLogo, int? year = null) =>
        Build(new Template(
            Subject: "Verify your email for Mongoose.gg",
            Preheader: $"Your verification code expires in {CodeLifetimeMinutes} minutes.",
            Title: "Verify your email",
            Intro: "enter this code on Mongoose.gg to finish creating your account.",
            CodeLabel: "VERIFICATION CODE",
            NextStep: "Next, you link your Riot ID and we sync your recent matches.",
            Ignore: "Didn't sign up for Mongoose.gg? You can ignore this email."),
            username, code, hasLogo, year ?? DateTime.UtcNow.Year);

    /// <summary>
    /// Email sent from "Forgot password?" with the code that lets the player choose a new password.
    /// </summary>
    public static EmailContent PasswordReset(string username, string code, bool hasLogo, int? year = null) =>
        Build(new Template(
            Subject: "Reset your Mongoose.gg password",
            Preheader: $"Your reset code expires in {CodeLifetimeMinutes} minutes.",
            Title: "Reset your password",
            Intro: "enter this code on Mongoose.gg to choose a new password.",
            CodeLabel: "RESET CODE",
            NextStep: null,
            Ignore: "Didn't ask for a new password? You can ignore this email. Your password stays the same."),
            username, code, hasLogo, year ?? DateTime.UtcNow.Year);

    private sealed record Template(
        string Subject,
        string Preheader,
        string Title,
        string Intro,
        string CodeLabel,
        string? NextStep,
        string Ignore);

    private static EmailContent Build(Template t, string username, string code, bool hasLogo, int year)
    {
        // HTML-encode user-controlled data to prevent HTML injection
        var encodedUsername = WebUtility.HtmlEncode(username);
        var encodedCode = WebUtility.HtmlEncode(code);

        var logoHtml = hasLogo
            ? $@"<img src=""cid:logo"" alt="""" width=""99"" height=""40"" style=""display: block; width: 99px; height: 40px; border: 0;"" />"
            : "";

        var nextStepHtml = t.NextStep is null
            ? ""
            : $@"<p style=""margin: 0 0 12px 0; font-family: {BodyFont}; font-size: 15px; line-height: 23px; color: {InkSoft};"">{t.NextStep}</p>";

        var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <meta name=""color-scheme"" content=""dark"">
    <meta name=""supported-color-schemes"" content=""dark"">
    <title>{t.Title}</title>
    <style>
        @media only screen and (max-width: 480px) {{
            .mg-card {{ padding: 28px 20px !important; }}
        }}
    </style>
</head>
<body style=""margin: 0; padding: 0; background-color: {Bg}; color: {Ink}; font-family: {BodyFont};"">
    <div style=""display: none; max-height: 0; overflow: hidden; opacity: 0; color: {Bg};"">{t.Preheader}</div>
    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""width: 100%; border-collapse: collapse; background-color: {Bg};"">
        <tr>
            <td align=""center"" style=""padding: 40px 16px;"">
                <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""width: 100%; max-width: 560px; border-collapse: separate;"">
                    <!-- Brand -->
                    <tr>
                        <td style=""padding: 0 0 24px 0;"">
                            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse: collapse;"">
                                <tr>
                                    <td style=""padding: 0 10px 0 0; vertical-align: middle;"">{logoHtml}</td>
                                    <td style=""vertical-align: middle; font-family: {DisplayFont}; font-size: 20px; font-weight: 700; color: {Ink};"">Mongoose.gg</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <!-- Card -->
                    <tr>
                        <td class=""mg-card"" style=""padding: 40px 36px; background-color: {Surface}; border: 1px solid {Divider}; border-radius: 24px;"">
                            <h1 style=""margin: 0 0 16px 0; font-family: {DisplayFont}; font-size: 30px; line-height: 36px; font-weight: 600; color: {Ink};"">{t.Title}</h1>
                            <p style=""margin: 0 0 28px 0; font-family: {BodyFont}; font-size: 16px; line-height: 25px; color: {InkSoft};"">
                                Hi <strong style=""color: {Ink};"">{encodedUsername}</strong>, {t.Intro}
                            </p>
                            <!-- Code: the one highlight box -->
                            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""width: 100%; border-collapse: separate;"">
                                <tr>
                                    <td align=""center"" style=""padding: 24px 16px; background-color: {SurfaceHighlight}; border: 1px solid {BorderHighlight}; border-radius: 20px;"">
                                        <div style=""margin: 0 0 10px 0; font-family: {BodyFont}; font-size: 13px; line-height: 17px; font-weight: 700; letter-spacing: 0.06em; color: {InkMuted};"">{t.CodeLabel}</div>
                                        <div style=""font-family: {DisplayFont}; font-size: 40px; line-height: 44px; font-weight: 700; letter-spacing: 10px; color: {Ink};"">{encodedCode}</div>
                                    </td>
                                </tr>
                            </table>
                            <p style=""margin: 28px 0 12px 0; font-family: {BodyFont}; font-size: 15px; line-height: 23px; color: {InkSoft};"">The code expires in {CodeLifetimeMinutes} minutes.</p>
                            {nextStepHtml}
                            <p style=""margin: 0; font-family: {BodyFont}; font-size: 14px; line-height: 21px; color: {InkMuted};"">{t.Ignore}</p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style=""padding: 24px 4px 0 4px; font-family: {BodyFont}; font-size: 13px; line-height: 19px; color: {InkFaint};"">
                            Mongoose.gg · Coaching from your own match history<br>
                            Not affiliated with Riot Games. © {year} Mongoose.gg
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

        var codeLabel = t.CodeLabel[..1] + t.CodeLabel[1..].ToLowerInvariant();
        var lines = new List<string>
        {
            t.Title,
            "",
            $"Hi {username}, {t.Intro}",
            "",
            $"{codeLabel}: {code}",
            "",
            $"The code expires in {CodeLifetimeMinutes} minutes.",
        };
        if (t.NextStep is not null)
        {
            lines.Add(t.NextStep);
        }
        lines.Add(t.Ignore);
        lines.Add("");
        lines.Add("Mongoose.gg · Coaching from your own match history");
        lines.Add($"Not affiliated with Riot Games. © {year} Mongoose.gg");
        var text = string.Join("\n", lines);

        return new EmailContent(t.Subject, html, text);
    }
}
