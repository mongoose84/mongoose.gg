using System.Text.Json.Serialization;

namespace Mongoose.Api.Application.DTOs.Auth;

public record ForgotPasswordRequest(
    [property: JsonPropertyName("email")] string Email);

public record ForgotPasswordResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string Message);

public record ResetPasswordRequest(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("newPassword")] string NewPassword);

public record ResetPasswordResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string Message);

public record ChangePasswordRequest(
    [property: JsonPropertyName("currentPassword")] string CurrentPassword,
    [property: JsonPropertyName("newPassword")] string NewPassword);

public record ChangePasswordResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string Message);

