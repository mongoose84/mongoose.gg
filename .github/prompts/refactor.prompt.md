---
agent: agent
model: Claude Sonnet 4.6
tools: ['file-search', 'semantic-search', 'codebase', 'editFiles', 'runTests']
description: 'Code refactoring workflow with safety checks'
---
# Code Refactoring Workflow

## Context Loading Phase
1. Review the target code provided by the user
2. Search the codebase for all usages of the target code
3. Find and review related test files
4. Review [architecture spec](../specs/architecture.spec.md) for relevant conventions

## Refactoring Planning
### Analysis
- [ ] Identify code smells and issues
- [ ] Map dependencies and impacts
- [ ] Verify test coverage exists
- [ ] List breaking changes
- [ ] Identify DDD impacts (bounded contexts, aggregate boundaries, ubiquitous language)

### Refactoring Strategy
- [ ] Define refactoring objectives
- [ ] Plan incremental steps
- [ ] Identify safe transformation patterns
- [ ] Plan for backward compatibility if needed
- [ ] Preserve or improve domain model clarity (avoid moving domain rules out of Core)

## Safe Refactoring Principles
1. **Small Steps**: Make small, incremental changes
2. **Test First**: Ensure tests pass before and after
3. **One Change**: One refactoring technique at a time
4. **Verify Often**: Run tests after each change

## Common Refactoring Patterns
### Simplification
- Extract method/function
- Inline method/function
- Consolidate duplicate code
- Simplify conditional expressions

### Organization
- Move method/function
- Rename for clarity
- Extract class/module
- Organize imports

## Execution Steps
1. Run existing tests to establish baseline
2. Apply refactoring incrementally
3. Run tests after each change
4. Update documentation
5. Final test run

## Validation Checklist
- [ ] All tests pass
- [ ] No functionality has changed
- [ ] Code is more readable
- [ ] Complexity has reduced
- [ ] Performance is maintained or improved
- [ ] Documentation is updated

## Human Validation Gate
🚨 **STOP**: Review refactoring plan before execution.
Confirm: Test coverage is adequate, changes are incremental, rollback plan exists.
