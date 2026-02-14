---
applyTo: "**/test/**"
description: "Testing guidelines with context engineering"
---
# Testing Guidelines

## Context Loading
Review [project conventions](../../README.md) and
[existing unittests, Playwright tests](../../) before writing tests.

## Deterministic Requirements
- Follow the AAA pattern: Arrange, Act, Assert
- Write descriptive test names that explain the scenario
- Mock external dependencies — keep tests isolated
- Ensure tests are deterministic and repeatable
- Cover both happy paths and error conditions
- Follow unittests, Playwright conventions and patterns
- Unit tests, E2E
- Follow standard formatting

## Structured Output
Generate tests with:
- [ ] Setup and teardown for shared state
- [ ] Edge case and error condition coverage
- [ ] Mock implementations for external dependencies
- [ ] Clear test documentation
