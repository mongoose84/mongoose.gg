using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using RiotProxy.Core.Entities;
using RiotProxy.Infrastructure.Database.Repositories;
using RiotProxy.Infrastructure.Email;
using static RiotProxy.Application.DTOs.RegisterDto;

namespace RiotProxy.Application.Endpoints.Auth;

/// <summary>
/// Register Endpoint
/// Creates a new user account with username, email, and password.
/// Sets emailVerified=false and logs the user in with a session cookie.
/// </summary>
public sealed class RegisterEndpoint : IEndpoint
{
    public string Route { get; }
    private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

    public RegisterEndpoint(string basePath)
    {
        Route = basePath + "/auth/register";
    }

    public void Configure(WebApplication app)
    {
        app.MapPost(Route, async (
            [FromBody] RegisterRequest request,
            HttpContext httpContext,
            [FromServices] UsersRepository usersRepo,
            [FromServices] VerificationTokensRepository tokensRepo,
            [FromServices] IEmailService emailService,
            [FromServices] ILogger<RegisterEndpoint> logger,
            [FromServices] IConfiguration config
        ) =>
        {
            try
            {
                // Feature flag gate
                var enableMvpLogin = config.GetValue<bool>("Auth:EnableMvpLogin");
                if (!enableMvpLogin)
                {
                    logger.LogWarning("Registration attempt blocked: MVP login disabled by configuration");
                    return Results.Json(new { error = "Registration is currently disabled" }, statusCode: 503);
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(request.Username))
                    return Results.BadRequest(new { error = "Username is required", code = "USERNAME_REQUIRED" });

                if (string.IsNullOrWhiteSpace(request.Email))
                    return Results.BadRequest(new { error = "Email is required", code = "EMAIL_REQUIRED" });

                if (string.IsNullOrWhiteSpace(request.Password))
                    return Results.BadRequest(new { error = "Password is required", code = "PASSWORD_REQUIRED" });

                // Validate username length (3-50 chars)
                if (request.Username.Length < 3)
                    return Results.BadRequest(new { error = "Username must be at least 3 characters", code = "USERNAME_TOO_SHORT" });

                if (request.Username.Length > 50)
                    return Results.BadRequest(new { error = "Username must be 50 characters or less", code = "USERNAME_TOO_LONG" });

                // Validate username format (alphanumeric, underscore, hyphen only)
                if (!UsernameRegex.IsMatch(request.Username))
                    return Results.BadRequest(new { error = "Username can only contain letters, numbers, underscores, and hyphens", code = "USERNAME_INVALID" });

                // Normalize username to lowercase to prevent case-variant duplicates
                var normalizedUsername = request.Username.ToLowerInvariant().Trim();

                // Validate password length
                if (request.Password.Length < 8)
                    return Results.BadRequest(new { error = "Password must be at least 8 characters", code = "PASSWORD_TOO_SHORT" });

                // Check if username already exists (case-insensitive)
                if (await usersRepo.UsernameExistsAsync(normalizedUsername))
                {
                    logger.LogWarning("Registration attempt with existing username: {Username}", request.Username);
                    return Results.Conflict(new { error = "This username is already taken", code = "USERNAME_TAKEN" });
                }

                // Check if email already exists
                if (await usersRepo.EmailExistsAsync(request.Email))
                {
                    logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
                    return Results.Conflict(new { error = "This email is already registered", code = "EMAIL_TAKEN" });
                }

                // Hash password (using BCrypt)
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // Create user with normalized username and email
                var newUser = new User
                {
                    Email = request.Email.ToLowerInvariant().Trim(),
                    Username = normalizedUsername,
                    PasswordHash = passwordHash,
                    EmailVerified = false,
                    IsActive = true,
                    Tier = "free",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var userId = await usersRepo.UpsertAsync(newUser);
                newUser.UserId = userId;

                // Invalidate any existing verification tokens for this user
                // This handles edge cases like race conditions or if UpsertAsync updated an existing user
                await tokensRepo.InvalidateActiveTokensAsync(userId, TokenTypes.EmailVerification);

                // Generate verification code and create token
                var verificationCode = VerificationCodeGenerator.Generate();
                var verificationExpiresAt = DateTime.UtcNow.AddMinutes(15);
                await tokensRepo.CreateTokenAsync(userId, TokenTypes.EmailVerification, verificationCode, verificationExpiresAt);

                // Send verification email (fire-and-forget to not block registration)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await emailService.SendVerificationEmailAsync(newUser.Email, newUser.Username, verificationCode);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to send verification email to {Email}", newUser.Email);
                    }
                });

                // Create claims identity for cookie auth
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, newUser.Username),
                    new Claim(ClaimTypes.Email, newUser.Email),
                    new Claim("email_verified", "false"),
                    new Claim("tier", newUser.Tier)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var sessionTimeoutMinutes = config.GetValue<int>("Auth:SessionTimeout", 30);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(sessionTimeoutMinutes)
                };

                // Sign in user with cookie
                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                logger.LogInformation("User {Username} (ID: {UserId}) registered successfully", newUser.Username, userId);

                return Results.Ok(new RegisterResponse(
                    userId,
                    newUser.Username,
                    newUser.Email,
                    false,
                    "Registration successful. Please verify your email."
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in RegisterEndpoint");
                return Results.Json(new { error = "Internal server error" }, statusCode: 500);
            }
        });
    }
}

