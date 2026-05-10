# Component Specification: [ComponentName]

> **Purpose**: Implementation-oriented component spec template for documenting a component's role, API, behavior, and test expectations before or during delivery.

> **Role in guidance split**: This spec is the canonical home for full component templates and examples. Keep broad frontend instructions terse and use this file when a task needs the complete component contract or scaffold.

## Overview
**Purpose**: [Brief description of what this component does]

**Framework**: Vue 3 (`<script setup>`)
**Language**: JavaScript (no TypeScript)

## Component Details

### Type
- [ ] Presentational (UI only, no business logic)
- [ ] Container (manages state and business logic)
- [ ] Layout (structural component)
- [ ] Page (route component)

### Location
**File Path**: `client/src/components/[domain]/[ComponentName].vue`

> **Naming conventions:**
> - Components: `PascalCase.vue` (e.g. `MainChampionCard.vue`)
> - Base/shared components: `client/src/components/base/Base*.vue`
> - Feature components: `client/src/components/[solo|overview|shared]/*.vue`
> - Views/pages: `client/src/views/*Page.vue` (e.g. `SoloStatsPage.vue`)
> - Composables: `client/src/composables/use*.js`

### Props
```js
const props = defineProps({
  /** [description] */
  prop1: {
    type: String,
    required: true
  },
  /** [description] */
  prop2: {
    type: Number,
    default: 0
  },
  /** [description] — use validator for constrained values */
  variant: {
    type: String,
    default: 'primary',
    validator: (v) => ['primary', 'secondary', 'ghost'].includes(v)
  },
  /** Array/Object defaults must use factory functions */
  items: {
    type: Array,
    default: () => []
  }
})
```

### Emits
```js
const emit = defineEmits(['update', 'close'])

// Usage:
emit('close')
emit('update', payload)
```

### State Management
**Local State** (Composition API refs/reactives):
- `[stateName]`: `ref([initial])` — [description]
- `[stateName]`: `computed(() => ...)` — [description]

**Pinia Store** (if applicable):
- Store: `use[Name]Store` from `@/stores/[name]`
- State: `[list state properties accessed]`
- Actions: `[list actions called]`

## Visual Design

### Layout
```
[ASCII art or description of component layout]
┌─────────────────────────────┐
│  Header                     │
├─────────────────────────────┤
│  Content Area               │
│                             │
├─────────────────────────────┤
│  Actions                    │
└─────────────────────────────┘
```

### Styling
**Approach**: Tailwind utility classes + `<style scoped>` + CSS custom properties (design tokens)

**Design Tokens** (CSS variables from theme):
- Colors: `var(--color-surface)`, `var(--color-border)`, `var(--color-text-primary)`, etc.
- Spacing: `var(--spacing-xs)`, `var(--spacing-sm)`, `var(--spacing-md)`, etc.
- Typography: [font styles]

> Use Tailwind utilities in templates for layout/sizing. Use `<style scoped>` with CSS custom properties for component-specific styles and themed values. No CSS Modules.

### Responsive Behavior
- **Mobile** (< 768px): [description]
- **Tablet** (768px - 1024px): [description]
- **Desktop** (> 1024px): [description]

## Behavior

### User Interactions
1. **[Action]**: [Description of what happens]
2. **[Action]**: [Description of what happens]

### Side Effects
- [ ] API calls on mount (`onMounted`)
- [ ] Watchers (`watch` / `watchEffect`)
- [ ] Subscriptions/intervals
- [ ] Cleanup via `onUnmounted`

## Data Flow

### Input
- Props from parent component
- Data from API via service: `@/services/[service]`
- Data from Pinia store: `use[Name]Store`
- Data from composable: `use[Name]`

### Output
- Events emitted to parent: `[list events]`
- Store actions triggered: `[list actions]`
- API calls made: `[list endpoints]`

## Accessibility

### ARIA Attributes
- `aria-label`: [description]
- `aria-describedby`: [description]
- `role`: [appropriate role]

### Keyboard Navigation
- `Tab`: [behavior]
- `Enter/Space`: [behavior]
- `Escape`: [behavior]

### Screen Reader Support
- [ ] All interactive elements have labels
- [ ] Dynamic content changes are announced
- [ ] Focus management is handled properly

## Testing

### Unit Tests
**Framework**: Vitest + `@vue/test-utils`
**Test file**: `client/test/unit/[ComponentName].spec.js`

- [ ] Renders without errors
- [ ] Handles all props correctly (use `it.each` for variants)
- [ ] Emits correct events on interaction
- [ ] Handles edge cases (null, undefined, empty arrays)
- [ ] Loading / empty / error states render correctly

```js
// Example test structure
import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import ComponentName from '@/components/[domain]/ComponentName.vue'

function mountComponent(props = {}, options = {}) {
  return mount(ComponentName, {
    props: { /* defaults */ ...props },
    global: {
      stubs: { /* child component stubs */ },
      ...options
    }
  })
}

describe('ComponentName', () => {
  it('renders correctly with default props', () => {
    const wrapper = mountComponent()
    expect(wrapper.exists()).toBe(true)
  })
})
```

### E2E Tests (if applicable)
**Framework**: Playwright (`client/e2e/*.spec.js`)

- [ ] Component works in full page context
- [ ] Interactions produce expected navigation/API calls

## Performance

### Optimization Strategies
- [ ] `computed` for derived values (automatic caching)
- [ ] `shallowRef` for large non-reactive object trees
- [ ] Lazy loading via `defineAsyncComponent` or route-level code splitting
- [ ] `v-once` / `v-memo` for static or rarely-changing subtrees
- [ ] Virtualization for long lists

## Dependencies

### External Libraries
- [ ] [Library name] — [purpose]

### Internal Dependencies
- [ ] [Component] from `@/components/...` — [purpose]
- [ ] [Composable] from `@/composables/...` — [purpose]
- [ ] [Store] from `@/stores/...` — [purpose]

## Implementation Checklist
- [ ] Component `.vue` file created with `<script setup>`
- [ ] Props defined with types, defaults, and validators
- [ ] Emits declared via `defineEmits`
- [ ] Template implemented with Tailwind + scoped styles
- [ ] Logic implemented (composables extracted where reusable)
- [ ] Unit tests written (`client/test/unit/[ComponentName].spec.js`)
- [ ] Accessibility verified
- [ ] Documentation updated
- [ ] Code review completed

## Examples

### Basic Usage
```vue
<ComponentName
  prop1="value"
  :prop2="123"
  @update="handleUpdate"
/>
```

### Usage with slots (if applicable)
```vue
<ComponentName prop1="value">
  <template #default>
    Slot content
  </template>
</ComponentName>
```

## Future Enhancements
- [ ] [Potential improvement]
- [ ] [Potential improvement]
