---
mode: agent
model: gpt-4
tools: ['file-search', 'semantic-search', 'codebase']
description: 'Feature implementation workflow with specification-first approach'
---
# Feature Implementation from Specification

## Context Loading Phase
1. Review [project specification](${specFile})
2. Analyze [existing codebase patterns](../../src/)
3. Check [API documentation](../../docs/api/)
4. Review [architecture guidelines](../../docs/architecture/)

## Planning Phase
### Requirements Analysis
- [ ] Understand problem statement
- [ ] Identify affected components
- [ ] List dependencies and integrations
- [ ] Identify breaking changes

### Technical Design
- [ ] Define data models and types
- [ ] Design API contracts
- [ ] Plan database changes
- [ ] Consider error handling
- [ ] Plan for testing

## Deterministic Execution
Use semantic search to find similar implementations:
`semantic-search "similar feature implementation"`

Use file search to locate test patterns:
`file-search "**/*.test.{js,ts,go,py}"`

## Implementation Checklist
### Backend Implementation
- [ ] Create/update data models
- [ ] Implement business logic
- [ ] Add API endpoints
- [ ] Handle errors properly
- [ ] Add validation

### Frontend Implementation
- [ ] Create/update components
- [ ] Implement state management
- [ ] Add API integration
- [ ] Handle loading and error states
- [ ] Ensure accessibility

### Testing
- [ ] Write unit tests (>90% coverage target)
- [ ] Add integration tests
- [ ] Test error scenarios
- [ ] Verify edge cases

### Documentation
- [ ] Update API documentation
- [ ] Add inline code comments
- [ ] Update README if needed
- [ ] Add usage examples

## Structured Output Requirements
Generate implementation with:
1. Feature code in appropriate module
2. Comprehensive unit tests
3. Integration tests for API endpoints
4. Documentation updates

## Human Validation Gate
🚨 **STOP**: Review implementation plan before proceeding to code generation.
Confirm: Architecture alignment, test strategy, and breaking change impact.
