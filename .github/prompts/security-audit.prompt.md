---
agent: agent
model: Claude Sonnet 4.6
description: 'Security audit for files or branches against project security rules and OWASP Top 10'
---
# Security Audit

Audit the provided files or branch changes for security issues using repo rules plus OWASP Top 10 categories.

## Use When

- The user asks for a security review of files, a feature, or a branch.
- A sensitive auth, logging, data, or SQL change needs focused audit coverage.

## Scope

- If a file or component is specified, audit that scope.
- Otherwise, audit current branch changes against `origin/main`.

## Review Standard

- Use [copilot-instructions.md](../copilot-instructions.md) and [architecture.spec.md](../specs/architecture.spec.md) for project-specific security rules.
- Focus on access control, cryptographic handling, injection risks, auth failures, secrets, and logging failures.
- Check repo-specific hotspots such as log sanitization, PUUID exposure, parameterized SQL, ownership checks, and secret handling.

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
