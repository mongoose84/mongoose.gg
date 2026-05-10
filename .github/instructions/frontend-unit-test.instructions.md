---
applyTo: "client/test/unit/**/*.{js,ts}"
description: "Frontend unit test guidance for Vitest and Vue Test Utils. Use when writing or editing tests under client/test/unit/, including component, composable, store, service, router, and utility tests."
---
# Frontend Unit Test Guidelines

## Context Loading
Review these BEFORE writing frontend unit tests:
- [Test Strategy Spec](../specs/test-strategy.spec.md) — frontend coverage priorities and test pyramid expectations
- [Architecture Spec](../specs/architecture.spec.md) — API contracts used by services and views
- [UI/UX Spec](../specs/ui-ux.spec.md) — expected component behavior, states, and accessibility expectations
- The source file under test — understand props, emits, computed state, async flows, and interactions
- Existing tests in `client/test/unit/` — match local style and helper usage

## Frontend Unit Test Stack

- **Frameworks**: Vitest + Vue Test Utils
- **Scope**: components, layouts, views, composables, stores, services, router, bootstrap, and utilities
- **Helpers**: `client/test/helpers/testUtils.js` and nearby test utilities

## Component And View Coverage

For components, layouts, and views with logic, cover these states when applicable:

1. Renders with valid data
2. Empty state
3. Loading state
4. Error state
5. Key user interactions and emitted events

### Standard Pattern
```javascript
import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import MyComponent from '@/components/MyComponent.vue'

describe('MyComponent', () => {
  const mountComponent = (props = {}) =>
    mount(MyComponent, { props: { data: defaultData, ...props } })

  it('renders with data', () => {
    const wrapper = mountComponent()
    expect(wrapper.find('[data-testid="my-component"]').exists()).toBe(true)
  })

  it('shows empty state when no data', () => {
    const wrapper = mountComponent({ data: [] })
    expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
  })
})
```

## Mocking And Helpers

- Use `vi.mock()` for external dependencies such as API modules, Chart.js, or third-party UI libraries
- Prefer `data-testid` selectors over CSS classes or tag names
- Use `setupPinia()` from `test/helpers/testUtils.js` for store-dependent tests
- Use local mount helpers to keep defaults and stubs consistent
- Await Vue updates after async state changes or prop updates

## Module Coverage Rules

- **Composables**: cover returned state, derived values, and side effects
- **Stores**: cover actions, loading/error transitions, and derived state
- **Services**: cover request/response transformation and error handling
- **Router and bootstrap**: cover guards, redirects, and startup wiring when logic exists
- **Utilities**: cover edge cases and formatting behavior directly

## Quality Checklist

- [ ] Tests assert user-visible behavior or public outputs
- [ ] External dependencies are mocked only as far as needed
- [ ] Assertions use stable selectors and explicit expectations
- [ ] New or changed frontend logic has corresponding unit coverage
- [ ] No snapshot tests when explicit assertions are clearer