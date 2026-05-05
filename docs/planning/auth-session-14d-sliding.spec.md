# Spec: 14-Day Sliding Auth Session (Always Persistent)

**Audience:** Implementer (Claude Haiku). Follow each step verbatim.
**Goal:** Replace the dual-mode (short session vs. 30-day remember-me) auth with a single 14-day sliding persistent session for every login. Remove the "Remember me" checkbox.

---

## Background (read once, then implement)

Current behavior:
- If user does NOT tick "Keep me logged in for 30 days" → cookie expires after `Auth:SessionTimeout` minutes (default **30 minutes**) AND does not slide. Users get logged out fast.
- If user ticks the box → 30-day persistent cookie with sliding refresh.
- The checkbox is **unchecked by default**, so most users get the 30-minute experience even though the UI implies 30 days.

Target behavior:
- Every successful login produces a **persistent cookie that lives 14 days** and **slides** (ASP.NET default sliding rules: cookie is reissued when more than half the lifetime has elapsed on a request).
- No "Remember me" checkbox. No `rememberMe` field anywhere.
- No custom sliding logic. Use the framework defaults.

---

## File-by-file changes

### 1. `server/Mongoose.Api/Program.cs`

Locate the cookie auth registration block (around lines 129–145). Update:

- `options.ExpireTimeSpan = TimeSpan.FromDays(30);` → `options.ExpireTimeSpan = TimeSpan.FromDays(14);`
- Keep `options.SlidingExpiration = true;` as-is.
- Replace the comment block above `builder.Services.AddAuthentication(...)` (the multi-line comment starting with `// Add authentication (cookie-based)` and ending before the `AddAuthentication` call) with:

```csharp
// Add authentication (cookie-based)
// All logins produce a 14-day persistent cookie with sliding expiration.
// ASP.NET reissues the cookie automatically when more than half the lifetime has elapsed.
```

Do not touch any other option (`HttpOnly`, `SecurePolicy`, `SameSite`, `Cookie.Name`, `OnRedirectToLogin`, `OnRedirectToAccessDenied`, `OnValidatePrincipal`).

---

### 2. `server/Mongoose.Api/Application/Endpoints/Auth/LoginEndpoint.cs`

Locate the block that builds `AuthenticationProperties` (currently around lines 154–177, the `if (request.RememberMe) { ... } else { ... }` branch).

Replace the entire `if/else` block — including the comment immediately above it — with:

```csharp
// Single session policy: every login produces a 14-day persistent cookie.
// SlidingExpiration on the cookie handler refreshes it automatically on activity.
var authProperties = new AuthenticationProperties
{
    IsPersistent = true,
    AllowRefresh = true,
    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
};
```

Update the XML doc summary on the class (currently mentions "configurable session timeout by default and 30-day remember-me sessions"). Replace it with:

```csharp
/// <summary>
/// Login Endpoint
/// Validates username/password and sets an httpOnly auth cookie for subsequent requests.
/// All sessions are 14-day sliding persistent cookies.
/// Rate limited to 10 requests per 15 minutes per IP to prevent brute force attacks.
/// </summary>
```

Do not change anything else in the file (rate limiting, BCrypt, security_stamp, login sync, etc.). The `request.RememberMe` value is now ignored by the server — that is intentional and is removed from the DTO in step 3.

---

### 3. `server/Mongoose.Api/Application/DTOs/Auth/LoginDto.cs`

Remove the `RememberMe` parameter from the `LoginRequest` record. The record should become:

```csharp
public record LoginRequest(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("consentLevel")] string? ConsentLevel = null
);
```

Do not touch `LoginResponse`.

---

### 4. `server/Mongoose.Api/appsettings.Development.json`

Remove the line `"SessionTimeout": 30,` from the `Auth` section. The `Auth` block becomes:

```json
"Auth": {
  "CookieName": "mongoose-auth",
  "EnableMvpLogin": true,
  "VerificationMaxAttempts": 5
}
```

Do not modify any other section.

---

### 5. `server/Mongoose.Api/appsettings.json`

No changes required (the production file does not contain `Auth:SessionTimeout`).

---

### 6. `client/src/services/authApi.js`

In the `login` function (around line 50), remove `rememberMe` from the destructured params and from the request body. Also remove the `@param {boolean} params.rememberMe` JSDoc line.

Result:

```js
/**
 * Login user
 * @param {Object} params - Login params
 * @param {string} params.username - Username or email
 * @param {string} params.password - User password
 * @param {string} params.consentLevel - Cookie consent level ('accepted' or 'rejected')
 * @returns {Promise<Object>} User data on success
 */
export async function login({ username, password, consentLevel = 'accepted' }) {
  const response = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ username, password, consentLevel })
  })
  // ... leave the rest of the function unchanged
```

Do not change error handling or other functions in this file.

---

### 7. `client/src/stores/authStore.js`

In the `login` function (around line 262), remove `rememberMe` from the destructured signature and from the `authApi.login` call.

Before:
```js
async function login({ username: uname, password, rememberMe = false }) {
  ...
  await authApi.login({ username: uname, password, rememberMe, consentLevel })
```

After:
```js
async function login({ username: uname, password }) {
  ...
  await authApi.login({ username: uname, password, consentLevel })
```

Do not change anything else in the function or file.

---

### 8. `client/src/views/AuthPage.vue`

Three edits in this file:

**8a.** Remove the entire "Remember me + Forgot password row" `<div v-if="isLogin" ...>` block (around lines 113–131) and replace it with a row that only contains the "Forgot password?" button, right-aligned:

```vue
<!-- Forgot password row (login only) -->
<div v-if="isLogin" class="flex items-center justify-end">
  <button
    type="button"
    class="text-sm text-primary hover:opacity-80 transition-opacity bg-transparent border-none cursor-pointer"
    @click="showForgotPassword"
  >
    Forgot password?
  </button>
</div>
```

**8b.** In the `formData` `ref` initializer (around line 186–192), remove the `rememberMe: false` line:

```js
const formData = ref({
  username: '',
  email: '',
  password: ''
});
```

In the `toggleMode` function (around line 273), remove `rememberMe: false` from the reset object:

```js
formData.value = { username: '', email: '', password: '' };
```

**8c.** In `handleSubmit` (around lines 310–320), remove `rememberMe` from the `authStore.login` call and from the `trackAuth` payload:

```js
const result = await authStore.login({
  username: formData.value.username,
  password: formData.value.password
});

trackAuth('login', true);
```

Do not change anything else in the file.

---

### 9. Backend tests — `server/Mongoose.Api.Tests/LoginEndpointTests.cs`

**9a.** **Delete** the test method `Login_sets_allow_refresh_false_for_non_remember_me_sessions` in its entirety (it asserts the old short-session behavior, which no longer exists).

**9b.** **Rename** `Login_sets_allow_refresh_true_for_remember_me_sessions` to `Login_sets_persistent_sliding_cookie_with_14_day_expiry` and update its body so that:
- The POST body no longer contains `rememberMe = true` (just `username` and `password`).
- The expiry assertion checks 14 days instead of 30:
  ```csharp
  remaining.Should().BeGreaterThan(TimeSpan.FromDays(13));
  remaining.Should().BeLessThan(TimeSpan.FromDays(15));
  ```
- `ticket.Properties.IsPersistent.Should().BeTrue();` is added alongside the existing `AllowRefresh` assertion.

**9c.** Update `Login_sets_secure_http_only_cookie_on_success`:
- Remove the `["Auth:SessionTimeout"] = "45"` config entry from the `TestWebApplicationFactory` constructor (the key is no longer used).
- Update the `remaining` assertion at the bottom:
  ```csharp
  remaining.Should().BeGreaterThan(TimeSpan.FromDays(13));
  remaining.Should().BeLessThan(TimeSpan.FromDays(15));
  ```

**9d.** Search the rest of the file for any other reference to `rememberMe`, `RememberMe`, or `Auth:SessionTimeout`. Remove the field from request bodies (just drop the line) and remove the config key from factory setup. Do not delete other tests.

---

### 10. Frontend tests

Run a workspace-wide search for `rememberMe` under `client/test/` and `client/e2e/`. For every match:
- If it appears in a request body or `authStore.login(...)` call argument, remove just that property.
- If a test exists specifically to verify the "Remember me" checkbox UI, delete that test.
- Do not delete tests for unrelated login behavior.

---

### 11. Architecture spec — `.github/specs/architecture.spec.md`

Two known references to update:

- Line ~318: `**Request body**: `LoginRequest(username, password, rememberMe?)`` → `**Request body**: `LoginRequest(username, password, consentLevel?)``
- Line ~500: `public record LoginRequest(string Username, string Password, bool RememberMe = false);` → `public record LoginRequest(string Username, string Password, string? ConsentLevel = null);`

Also search the file for any narrative text about "30-day remember-me" or "session timeout" sessions and replace with: "All sessions are 14-day sliding persistent cookies."

---

## Verification

After all edits, run:

```bash
# Backend
cd server && dotnet build && dotnet test

# Frontend
cd client && npm run test:unit
```

All tests must pass. Manually verify by logging in and inspecting the `mongoose-auth` cookie in DevTools → Application → Cookies:
- `Expires / Max-Age` should be approximately **14 days** in the future.
- After making any authenticated request more than 7 days after login, the cookie's `Expires` should be reissued further into the future (ASP.NET default sliding refresh kicks in past the 50% mark).

---

## Out of scope

- Do NOT add a custom `OnValidatePrincipal` refresh mechanism.
- Do NOT change `SameSite`, `HttpOnly`, `SecurePolicy`, or the security_stamp validation.
- Do NOT touch the rate limiter, BCrypt verification, or login sync background task.
- Do NOT change the logout endpoint.
- Do NOT migrate existing cookies — current sessions will simply expire on their original schedule and users will get the new 14-day cookie on their next login.
