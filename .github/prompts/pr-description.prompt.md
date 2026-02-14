---
mode: agent
model: gpt-4
tools: ['changes', 'codebase', 'semantic-search']
description: 'Generate comprehensive pull request descriptions'
---
# Pull Request Description Generator

## Context Loading Phase
1. Review [changed files](changes)
2. Analyze [commit messages](git log)
3. Check [related issues](${issueLinks})
4. Understand [project context](../../README.md)

## PR Description Structure

### Title
Create a clear, concise title following the format:
`[Type] Brief description of changes`

Types: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`

### Description Template
```markdown
## Overview
[Brief summary of what this PR does]

## Problem Statement
[What problem does this solve? Link to issue if applicable]

## Solution
[How does this PR solve the problem?]

## Changes
### Added
- [New features or functionality]

### Modified
- [Changed functionality]

### Removed
- [Deleted functionality]

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing performed

### Test Plan
[How to test these changes]

## Breaking Changes
[List any breaking changes, or "None"]

## Documentation
- [ ] Documentation updated
- [ ] API docs updated (if applicable)
- [ ] README updated (if applicable)

## Checklist
- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] Tests pass locally
- [ ] No new warnings
- [ ] Documentation is clear

## Related Issues
Closes #[issue_number]
Related to #[issue_number]
```

## Content Guidelines
- Be specific and factual
- Explain the "why" not just the "what"
- Include screenshots for UI changes
- Link to related documentation
- Mention performance implications
- Note any deployment considerations

## Human Validation Gate
🚨 **STOP**: Review generated description.
Confirm: Description is accurate, complete, and helpful for reviewers.
