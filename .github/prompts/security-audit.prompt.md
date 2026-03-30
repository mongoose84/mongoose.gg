---
agent: agent
model: Claude Sonnet 4.6
description: 'Security audit for files or branches against project security rules and OWASP Top 10'
---
# Security Audit

Audit the provided file(s) or current branch changes for security violations. Check against both project-specific rules and OWASP Top 10.

## Scope

If a file or component is specified, audit that. Otherwise, audit all changes on the current branch:
```
git diff origin/main...HEAD
```

## Project Security Rules (Non-Negotiable)

### 1. Log Injection Prevention
Every `logger.Log*` call with dynamic values MUST use `LogSanitizer.Sanitize()`:
```csharp
// ✅ Correct
logger.LogWarning("Invalid userId {UserId}", LogSanitizer.Sanitize(userId));

// ❌ Violation
logger.LogWarning("Invalid userId {UserId}", userId);
```

Scan all `.cs` files in scope for `logger.Log` calls where any argument is NOT wrapped in `LogSanitizer.Sanitize()`. Constant strings and numeric literals are exempt.

### 2. PUUID Exposure
PUUIDs are server-internal. They must NEVER appear in:
- API response DTOs returned to clients
- Frontend code (`client/src/`)
- URL paths exposed to browsers

Verify all data endpoints resolve PUUID from User ID via `IUserRiotAccountsRepository`.

### 3. SQL Injection
All SQL must use parameterized queries (`@paramName`). Scan for:
- String concatenation in SQL (`$"SELECT` or `"SELECT" +`)
- `string.Format` used in query building
- Any `CommandText` assignment with interpolation

### 4. Authentication & Authorization
Every data endpoint must:
- Check `httpContext.User?.Identity?.IsAuthenticated`
- Verify `ClaimTypes.NameIdentifier` matches route `userId`
- Use `AuthResults.NotAuthenticated()` / `Results.Forbid()`

### 5. Secrets & PII
- No hardcoded API keys, connection strings, or passwords
- Email and username access goes through `IEncryptor`
- Riot API keys only from environment variables

## OWASP Top 10 Checks

- **A01 Broken Access Control** — horizontal privilege escalation (user A accessing user B's data)
- **A02 Cryptographic Failures** — PII not encrypted, weak hashing, secrets in code
- **A03 Injection** — SQL injection, log injection, XSS via unsanitized output
- **A04 Insecure Design** — missing rate limiting on auth endpoints, no brute-force protection
- **A07 Auth Failures** — session fixation, missing cookie flags (HttpOnly, Secure, SameSite)
- **A09 Logging Failures** — auth events not logged, sensitive data in logs

## Output Format

```markdown
## Security Audit Results

**Scope**: [files or branch reviewed]
**Findings**: [count]

### Critical
- [ ] [File:Line] Description of violation

### Warning
- [ ] [File:Line] Description of concern

### Passed
- [x] Log sanitization — all calls use LogSanitizer.Sanitize()
- [x] No PUUID exposure in responses
- [x] Parameterized SQL only
- [x] Auth checks on all endpoints
- [x] No hardcoded secrets
```

Report findings grouped by severity. If no violations found, confirm each check passed.
