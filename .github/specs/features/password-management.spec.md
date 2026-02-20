# Feature: Password Management — Forgot, Reset & Change Password

## Problem Statement
Users can register and log in, but there is no way to recover access if they forget their password, nor can they change their password from within the app. This is a critical gap — any user who loses their password is permanently locked out.

## Proposed Solution
Implement three password management scenarios using the existing `verification_tokens` infrastructure (which already supports the `password_reset` token type) and the `IEmailService` / `SmtpEmailService` email pipeline:

1. **Forgot Password** — Public form on the auth page; user enters their email to receive a 6-digit reset code
2. **Reset Password** — Public page where the user enters the code + a new password to regain access
3. **Change Password** — Authenticated action on the User Settings page; user enters current password + new password

## User Stories

### Primary User Story
As a registered user who forgot my password, I want to request a password reset via email so that I can regain access to my account.

### Additional User Stories
- As a user on the reset page, I want to enter the 6-digit code I received and set a new password so that I can log back in
- As a logged-in user, I want to change my password from my settings page so that I can keep my account secure

## Requirements

### Functional Requirements

#### Scenario 1 — Forgot Password (request reset code)
1. Accessible from the login form via a "Forgot password?" link
2. User enters their email address
3. If the email exists: generate a 6-digit code (token type `password_reset`), send it via email, redirect to the reset page
4. If the email does not exist: return the **same success message** (no email enumeration)
5. Rate limit: 5 requests / hour / IP
6. Token expiry: 15 minutes
7. Invalidate any previous active `password_reset` tokens for the user before creating a new one

#### Scenario 2 — Reset Password (consume code + set new password)
8. User enters their email, the 6-digit code, and a new password (≥ 8 chars)
9. Validate code against `verification_tokens` (type `password_reset`, not expired, not used, max 5 attempts)
10. On success: hash new password with BCrypt, update `users.password_hash`, mark token as used, redirect to login
11. On failure: increment attempts, return error. After 5 failed attempts the token is burned
12. The user is **not** automatically signed in — they must log in with the new password

#### Scenario 3 — Change Password (authenticated)
13. User enters current password and new password (≥ 8 chars)
14. Verify current password via BCrypt
15. Hash and store the new password, update `users.updated_at`
16. Invalidate all active sessions (sign the user out, force re-login with new password)
17. No email notification required in v1

### Non-Functional Requirements
- **Performance**: All endpoints respond in < 200ms
- **Security**: No email enumeration on forgot-password; BCrypt hashing; rate limiting; brute-force protection via attempt counter; HTTPS only
- **Accessibility**: All forms keyboard-navigable, proper labels, focus states, error announcements

## Technical Approach

### Database Changes
**No schema changes required.** The `verification_tokens` table already supports `token_type = 'password_reset'` and `TokenTypes.PasswordReset` is already defined in `server/Core/Entities/VerificationToken.cs`.

The `IUsersRepository` needs a new method to update the password hash:
```csharp
Task UpdatePasswordHashAsync(long userId, string passwordHash);
```

### Backend Changes
**Language**: C#

**Components**:
- [ ] New endpoint: `server/Application/Endpoints/Auth/ForgotPasswordEndpoint.cs`
- [ ] New endpoint: `server/Application/Endpoints/Auth/ResetPasswordEndpoint.cs`
- [ ] New endpoint: `server/Application/Endpoints/Auth/ChangePasswordEndpoint.cs`
- [ ] New DTOs: `server/Application/DTOs/Auth/PasswordDto.cs`
- [ ] Extend: `server/Core/Interfaces/IUsersRepository.cs` — add `UpdatePasswordHashAsync`
- [ ] Extend: `server/Infrastructure/Database/Repositories/UsersRepository.cs` — implement `UpdatePasswordHashAsync`
- [ ] Extend: `server/Infrastructure/Email/IEmailService.cs` — add `SendPasswordResetEmailAsync`
- [ ] Extend: `server/Infrastructure/Email/SmtpEmailService.cs` — implement `SendPasswordResetEmailAsync`
- [ ] Register endpoints: `server/Application/MongooseApiApplication.cs`

### Frontend Changes
**Framework**: Vue

**Components**:
- [ ] Extend: `client/src/views/AuthPage.vue` — add "Forgot password?" link, forgot-password form state
- [ ] New view: `client/src/views/ResetPasswordPage.vue` — code + new password form
- [ ] Extend: `client/src/views/UserSettingsPage.vue` — add change-password section
- [ ] Extend: `client/src/router/index.js` — add `/auth/reset-password` route
- [ ] Extend: `client/src/services/authApi.js` — add `forgotPassword()`, `resetPassword()`, `changePassword()`
- [ ] Extend: `client/src/stores/authStore.js` — add `changePassword` action


### API Contracts

#### Endpoint 1 — Forgot Password
```
POST /api/v2/auth/forgot-password
```
**Auth**: None (public)
**Rate limit**: 5 / hour / IP

**Request**:
```json
{ "email": "user@example.com" }
```
**Response (200 — always, to prevent email enumeration)**:
```json
{ "success": true, "message": "If an account with that email exists, a reset code has been sent." }
```
**Side effects**: If email exists → invalidate old `password_reset` tokens → generate 6-digit code (15 min expiry) → send email

**DTO**:
```csharp
public record ForgotPasswordRequest(
    [property: JsonPropertyName("email")] string Email);
public record ForgotPasswordResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string Message);
```

#### Endpoint 2 — Reset Password
```
POST /api/v2/auth/reset-password
```
**Auth**: None (public)
**Rate limit**: 10 / 15 min / IP

**Request**:
```json
{ "email": "user@example.com", "code": "482917", "newPassword": "newSecure123" }
```
**Response (200)**:
```json
{ "success": true, "message": "Password has been reset. Please log in with your new password." }
```
**Error responses**:
| Status | Code | Condition |
|--------|------|-----------|
| 400 | `INVALID_CODE` | Code wrong or expired or max attempts exceeded |
| 400 | `PASSWORD_TOO_SHORT` | New password < 8 chars |
| 400 | `INVALID_EMAIL` | Email missing or malformed |

**DTO**:
```csharp
public record ResetPasswordRequest(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("newPassword")] string NewPassword);
public record ResetPasswordResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string Message);
```

#### Endpoint 3 — Change Password (authenticated)
```
POST /api/v2/auth/change-password
```
**Auth**: Yes (session cookie)

**Request**:
```json
{ "currentPassword": "oldPass123", "newPassword": "newSecure456" }
```
**Response (200)**:
```json
{ "success": true, "message": "Password changed. Please log in again." }
```
**Error responses**:
| Status | Code | Condition |
|--------|------|-----------|
| 401 | `INVALID_PASSWORD` | Current password incorrect |
| 400 | `PASSWORD_TOO_SHORT` | New password < 8 chars |
| 400 | `SAME_PASSWORD` | New password same as current |

**Side effects**: Update password hash → sign out user (clear session cookie)

**DTO**:
```csharp
public record ChangePasswordRequest(
    [property: JsonPropertyName("currentPassword")] string CurrentPassword,
    [property: JsonPropertyName("newPassword")] string NewPassword);
public record ChangePasswordResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string Message);
```

### Route Map Update

| Method | Route | Auth | Rate Limited | Handler File |
|--------|-------|------|--------------|--------------|
| `POST` | `/api/v2/auth/forgot-password` | No | 5/hr/IP | `Auth/ForgotPasswordEndpoint.cs` |
| `POST` | `/api/v2/auth/reset-password` | No | 10/15min/IP | `Auth/ResetPasswordEndpoint.cs` |
| `POST` | `/api/v2/auth/change-password` | Yes | No | `Auth/ChangePasswordEndpoint.cs` |

### Frontend Route

| Route | View | Notes |
|-------|------|-------|
| `/auth/reset-password` | `ResetPasswordPage.vue` | Public, `?email=` pre-fills email field |

### UX Flow

**Forgot → Reset flow**:
1. User clicks "Forgot password?" on login form
2. Auth page switches to forgot-password state (email input + submit)
3. On submit → `POST /auth/forgot-password` → redirect to `/auth/reset-password?email={email}`
4. Reset page: email (pre-filled, editable), 6-digit code input, new password input
5. On submit → `POST /auth/reset-password` → on success, redirect to `/auth?mode=login` with success toast

**Change password flow**:
1. User navigates to User Settings (`/app/user`)
2. Change Password section: current password + new password inputs + submit button
3. On submit → `POST /auth/change-password` → user is signed out → redirect to `/auth?mode=login`

### UI/UX Requirements

All views follow the existing design system defined in [UI/UX Spec](../ui-ux.spec.md). Use design tokens — never hardcode colors, spacing, or shadows.

#### Forgot Password State (on `AuthPage.vue`)

**Layout**: Reuse the existing auth card container (`max-w-[440px]`, `bg-background-surface`, `border border-border`, `rounded-lg`, `backdrop-blur-[10px]`). This is a third form state alongside login and register — toggled via internal state, not a separate route.

**Structure**:
```
┌─────────────────────────────────────┐
│         [Mongoose logo]             │
│       Forgot Your Password?         │
│  Enter your email to receive a      │
│         reset code                  │
│                                     │
│  ┌─ Error alert (conditional) ────┐ │
│  │ {errorMessage}                 │ │
│  └────────────────────────────────┘ │
│                                     │
│  Email                              │
│  ┌────────────────────────────────┐ │
│  │ you@example.com                │ │
│  └────────────────────────────────┘ │
│                                     │
│  ┌────────────────────────────────┐ │
│  │        Send Reset Code         │ │
│  └────────────────────────────────┘ │
│                                     │
│  ─────────────────────────────────  │
│      ← Back to Sign In             │
└─────────────────────────────────────┘
```

**Components**:
- Logo + heading: Same pattern as login/register — `img` (mongoose.png, `w-32 h-16`), `h1` (`text-2xl font-bold tracking-tight`), `p` (`text-base text-text-secondary`)
- Error alert: `p-md bg-error-soft border border-error-border rounded-md text-error text-sm` (same pattern as `LinkRiotAccountModal`)
- Email input: `BaseInput` — `type="email"`, `label="Email"`, `placeholder="you@example.com"`, `autocomplete="email"`, `required`
- Submit button: `BaseButton` — `variant="primary"`, `size="lg"`, `:loading="isSubmitting"`. Label: `"Send Reset Code"` / `"Sending..."` when loading
- Footer: `BaseButton variant="ghost" size="sm"` — `"← Back to Sign In"`, calls `toggleMode` back to login state

**Behavior**:
- On submit success: redirect to `/auth/reset-password?email={email}` (URL-encoded)
- On submit: button enters loading state (spinner + disabled). All form fields disabled during submission
- The success response is always 200 (no email enumeration) — no client-side success message needed, just redirect
- If network/server error: show error alert above form

**Accessibility**:
- `h1` heading updates to "Forgot Your Password?" for screen readers
- Email input has associated `label` via `BaseInput`
- Submit button has visible loading state text change
- Error alert has `role="alert"` for screen reader announcement
- Entire form is keyboard-navigable (Tab order: email → submit → back link)

---

#### Reset Password Page (`ResetPasswordPage.vue`)

**Layout**: Same centered card pattern as `AuthPage` and `VerifyPage`. Full-page view with `NavBar` at top. Card centered vertically and horizontally (`min-h-[calc(100vh-64px)] flex items-center justify-center p-xl`).

**Structure**:
```
┌─────────────────────────────────────┐
│         [Mongoose logo]             │
│        Reset Your Password          │
│   Enter the code sent to your       │
│     email and set a new password    │
│                                     │
│  ┌─ Success alert (conditional) ──┐ │
│  │ {successMessage}               │ │
│  └────────────────────────────────┘ │
│                                     │
│  ┌─ Error alert (conditional) ────┐ │
│  │ {errorMessage}                 │ │
│  └────────────────────────────────┘ │
│                                     │
│  Email                              │
│  ┌────────────────────────────────┐ │
│  │ user@example.com  (pre-filled) │ │
│  └────────────────────────────────┘ │
│                                     │
│  Reset Code                         │
│  ┌────────────────────────────────┐ │
│  │       0  0  0  0  0  0        │ │
│  └────────────────────────────────┘ │
│  Enter the 6-digit code from email  │
│                                     │
│  New Password                       │
│  ┌────────────────────────────────┐ │
│  │ ••••••••                       │ │
│  └────────────────────────────────┘ │
│  Must be at least 8 characters      │
│                                     │
│  ┌────────────────────────────────┐ │
│  │       Reset Password           │ │
│  └────────────────────────────────┘ │
│                                     │
│  ─────────────────────────────────  │
│  Remember your password? Sign in    │
└─────────────────────────────────────┘
```

**Components**:
- Header: Same auth card header pattern — logo, `h1` (`text-2xl font-bold tracking-tight`), `p` description (`text-base text-text-secondary`)
- Success alert: `p-md bg-success-soft border border-success-border rounded-md text-success text-sm text-center` (same as VerifyPage)
- Error alert: `p-md bg-error-soft border border-error-border rounded-md text-error text-sm` with `role="alert"`
- Email input: `BaseInput` — `type="email"`, `label="Email"`, pre-filled from `?email=` query param, editable, `autocomplete="email"`
- Code input: Styled monospace input — same pattern as VerifyPage code input (`text-2xl font-bold text-center tracking-[0.5em]`, `inputmode="numeric"`, `maxlength="6"`, `pattern="[0-9]{6}"`, `autocomplete="one-time-code"`). Hint text below: `"Enter the 6-digit code from your email"` (`text-xs text-text-secondary text-center`)
- New password input: `BaseInput` — `type="password"`, `label="New Password"`, `placeholder="••••••••"`, `minlength="8"`, `autocomplete="new-password"`. Hint text: `"Must be at least 8 characters"` (`text-xs text-text-secondary`)
- Submit button: `BaseButton` — `variant="primary"`, `size="lg"`, `:loading="isSubmitting"`, disabled when code length ≠ 6 or password < 8 chars. Label: `"Reset Password"` / `"Resetting..."` when loading
- Footer: `BaseButton variant="ghost" size="sm"` — `"Remember your password? Sign in"`, links to `/auth?mode=login`

**Behavior**:
- Email field pre-filled from `route.query.email` (URL-decoded) but remains editable
- Code input: filter non-numeric characters on input (same `handleCodeInput` pattern as VerifyPage — `e.target.value.replace(/\D/g, '').slice(0, 6)`)
- Submit disabled until code is exactly 6 digits AND password is ≥ 8 characters
- All fields disabled during submission
- On success: redirect to `/auth?mode=login` (user must log in with new password). Optionally pass a success indicator via query param or route state
- Error mapping:
  - `INVALID_CODE` → `"Invalid or expired code. Please request a new one."`
  - `PASSWORD_TOO_SHORT` → `"Password must be at least 8 characters."`
  - `INVALID_EMAIL` → `"Please enter a valid email address."`
  - Network error → `"Something went wrong. Please try again."`

**Accessibility**:
- All inputs have associated labels via `BaseInput` `label` prop or explicit `<label>` elements
- Code input has `inputmode="numeric"` for mobile numeric keyboard
- Submit button disabled state communicated via `aria-disabled` (inherited from `BaseButton`)
- Error and success alerts use `role="alert"` for live region announcements
- Tab order: email → code → password → submit → footer link

---

#### Change Password Section (on `UserSettingsPage.vue`)

**Layout**: New section inserted between the existing "Account" section and the "Session" section. Follows the exact same section pattern used throughout the page: `h2` heading + surface card.

**Structure**:
```
── Account section (existing) ──────────

── Security ────────────────────────────
┌─────────────────────────────────────┐
│  Change Password                    │
│  Update your account password.      │
│  You will be signed out afterward.  │
│                                     │
│  ┌─ Error alert (conditional) ────┐ │
│  │ {errorMessage}                 │ │
│  └────────────────────────────────┘ │
│                                     │
│  Current Password                   │
│  ┌────────────────────────────────┐ │
│  │ ••••••••                       │ │
│  └────────────────────────────────┘ │
│                                     │
│  New Password                       │
│  ┌────────────────────────────────┐ │
│  │ ••••••••                       │ │
│  └────────────────────────────────┘ │
│  Must be at least 8 characters      │
│                                     │
│  ┌──────────────────┐              │
│  │ Change Password  │              │
│  └──────────────────┘              │
└─────────────────────────────────────┘

── Session section (existing) ──────────
```

**Components**:
- Section heading: `h2` — `text-lg font-semibold text-text tracking-tight` with text `"Security"` (matches existing section heading pattern)
- Card: `bg-background-surface border border-border rounded-lg p-xl` (matches existing card pattern)
- Section title: `h3` — `text-sm font-semibold text-text` with text `"Change Password"`
- Description: `p` — `text-xs text-text-secondary mt-xs` with text `"Update your account password. You will be signed out afterward."`
- Error alert: `p-md bg-error-soft border border-error-border rounded-md text-error text-sm` with `role="alert"`
- Current password: `BaseInput` — `type="password"`, `label="Current Password"`, `placeholder="Your current password"`, `autocomplete="current-password"`, `required`
- New password: `BaseInput` — `type="password"`, `label="New Password"`, `placeholder="••••••••"`, `minlength="8"`, `autocomplete="new-password"`, `required`. Hint text below: `"Must be at least 8 characters"` (`text-xs text-text-secondary`)
- Submit button: `BaseButton` — `variant="primary"`, `size="md"`, `:loading="isChangingPassword"`, disabled when either field is empty or new password < 8 chars. Label: `"Change Password"` / `"Changing..."` when loading. Left-aligned (not full-width — matches the Danger Zone button alignment style)

**Behavior**:
- Both fields required — submit button disabled until current password is non-empty AND new password is ≥ 8 characters
- All fields disabled during submission
- On success: `authStore.user` is set to `null` (session is invalidated server-side), redirect to `/auth?mode=login`
- Error mapping:
  - `INVALID_PASSWORD` → `"Current password is incorrect."`
  - `PASSWORD_TOO_SHORT` → `"New password must be at least 8 characters."`
  - `SAME_PASSWORD` → `"New password must be different from your current password."`
  - Network error → `"Something went wrong. Please try again."`
- Form resets on error (clear new password field only, keep current password for retry)

**Accessibility**:
- All inputs have associated labels via `BaseInput` `label` prop
- Error alert uses `role="alert"` for screen reader announcement
- Submit button disabled state communicated via native `disabled` attribute
- Tab order: current password → new password → submit button
- `autocomplete="current-password"` and `autocomplete="new-password"` for password manager support

## Testing Strategy

### Integration Tests
**File**: `server/Mongoose.Api.Tests/PasswordManagementEndpointTests.cs`

- [ ] `ForgotPassword_Returns200_WhenEmailExists` — verify token created, email sent
- [ ] `ForgotPassword_Returns200_WhenEmailDoesNotExist` — verify no token created, no email sent (same response)
- [ ] `ForgotPassword_Returns429_WhenRateLimited`
- [ ] `ResetPassword_ReturnsSuccess_WithValidCode` — verify password updated, token marked used
- [ ] `ResetPassword_Returns400_WithInvalidCode`
- [ ] `ResetPassword_Returns400_WithExpiredCode`
- [ ] `ResetPassword_Returns400_AfterMaxAttempts`
- [ ] `ResetPassword_Returns400_WithShortPassword`
- [ ] `ChangePassword_ReturnsSuccess_WhenAuthenticated` — verify password updated, session cleared
- [ ] `ChangePassword_Returns401_WithWrongCurrentPassword`
- [ ] `ChangePassword_Returns400_WithShortNewPassword`
- [ ] `ChangePassword_Returns400_WhenSamePassword`
- [ ] `ChangePassword_Returns401_WhenNotAuthenticated`

## Validation Criteria
Feature is considered complete when:
- [ ] All three endpoints implemented and returning correct responses
- [ ] Password reset email is sent and received with correct 6-digit code
- [ ] Token brute-force protection works (5 max attempts)
- [ ] No email enumeration possible on forgot-password endpoint
- [ ] Change password signs the user out after success
- [ ] Frontend flows work end-to-end for all three scenarios
- [ ] All integration tests pass
- [ ] Rate limiting enforced on public endpoints

## Dependencies
### Internal Dependencies
- [x] `verification_tokens` table — already supports `password_reset` token type
- [x] `TokenTypes.PasswordReset` — already defined in `VerificationToken.cs`
- [x] `IVerificationTokensRepository` — reuse all existing methods (create, validate, increment, invalidate)
- [x] `SmtpEmailService` — extend with password reset email template
- [x] `VerificationCodeGenerator` — reuse for 6-digit code generation
- [x] `IRateLimiter` / `EndpointRateLimiter` — reuse for rate limiting

### External Dependencies
- None

## Risks and Mitigations
| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Email delivery delays frustrate users | Medium | Low | Show message "Code may take a few minutes" + resend option with 60s cooldown |
| Brute-force on reset code (6 digits = 1M combos) | High | Low | 5-attempt limit per token + 15 min expiry + IP rate limiting |
| User enters wrong email on reset page | Low | Medium | Pre-fill email from forgot step via query param; allow editing |

## References
- [Architecture Spec](../architecture.spec.md) — Endpoint patterns, auth flow, VerificationToken entity
- [UI/UX Spec](../ui-ux.spec.md) — AuthPage, UserSettingsPage, design tokens, component patterns
- [Database Schema](../../../server/schema.sql) — `verification_tokens` table with `password_reset` enum
