---
mode: agent
model: gpt-4
tools: ['file-search', 'semantic-search', 'codebase', 'problems', 'testFailure', 'editFiles', 'runTests']
description: 'Systematic bug investigation and fix workflow'
---
# Bug Fix Workflow

## Context Loading Phase
1. Review [bug report](${issueLink})
2. Check [related code](semantic-search "${component}")
3. Review [test failures](testFailure)
4. Check [recent changes](changes) to affected code

## Investigation Phase
### Reproduce the Bug
- [ ] Understand expected behavior
- [ ] Identify actual behavior
- [ ] Create minimal reproduction case
- [ ] Document reproduction steps

### Root Cause Analysis
- [ ] Trace code execution path
- [ ] Identify point of failure
- [ ] Understand why the bug occurs
- [ ] Check for similar bugs elsewhere

## Fix Strategy
### Planning
- [ ] Determine fix approach
- [ ] Identify affected components
- [ ] Consider edge cases
- [ ] Plan for regression prevention

### Implementation
1. Write a failing test that reproduces the bug
2. Implement the fix
3. Verify the test now passes
4. Run all tests to check for regressions
5. Add additional tests for edge cases

## Fix Validation Checklist
- [ ] Bug is reproducible before fix
- [ ] Bug is fixed after changes
- [ ] New tests prevent regression
- [ ] All existing tests pass
- [ ] No new issues introduced
- [ ] Edge cases are handled
- [ ] Documentation updated if needed

## Deterministic Execution
Use semantic search to find related code:
`semantic-search "similar functionality"`

Use file search to find test files:
`file-search "**/*test*"`

## Structured Output Requirements
Provide fix with:
1. Clear description of root cause
2. Explanation of fix approach
3. Code changes
4. Test that reproduces and validates fix
5. Any documentation updates

## Human Validation Gate
🚨 **STOP**: Review fix strategy before implementation.
Confirm: Root cause is understood, fix is minimal, tests are comprehensive.
