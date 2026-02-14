---
mode: agent
model: gpt-4
tools: ['file-search', 'semantic-search', 'changes', 'problems']
description: 'Structured code review workflow with validation gates'
---
# Code Review Workflow

## Context Loading Phase
1. Review [project guidelines](../../docs/standards/)
2. Check [changed files](changes) in current PR
3. Analyze [existing issues](problems) and warnings
4. Verify adherence to: Follow standard formatting
5. Check security requirements: Authentication

## Review Checklist
### Code Quality
- [ ] Code follows project style guidelines
- [ ] Functions/methods have clear, single responsibilities
- [ ] Variable and function names are descriptive
- [ ] No unnecessary complexity or over-engineering
- [ ] Code is DRY (Don't Repeat Yourself)

### Security
- [ ] No hard-coded credentials or secrets
- [ ] Input validation is present
- [ ] No SQL injection vulnerabilities
- [ ] Authentication/authorization checks in place
- [ ] Sensitive data is properly handled

### Testing
- [ ] Unit tests cover new/modified code
- [ ] Edge cases are tested
- [ ] Tests are meaningful and not just for coverage
- [ ] Integration tests updated if needed

### Documentation
- [ ] Public APIs are documented
- [ ] Complex logic has explanatory comments
- [ ] README updated if needed
- [ ] CHANGELOG updated for user-facing changes

### Performance
- [ ] No obvious performance bottlenecks
- [ ] Database queries are optimized
- [ ] No N+1 query problems
- [ ] Resource cleanup (connections, files) is handled

## Deterministic Execution
Use semantic search to find similar patterns: `semantic-search "<pattern>"`
Use file search to locate related files: `file-search "**/*.test.*"`

## Structured Output Requirements
Provide review feedback in the following format:

### Summary
[High-level assessment of the changes]

### Critical Issues
[Issues that must be fixed before merging]

### Suggestions
[Recommended improvements]

### Positive Observations
[Good patterns or improvements worth noting]

## Human Validation Gate
🚨 **STOP**: Review feedback before posting.
Confirm: Feedback is constructive, specific, and actionable.
