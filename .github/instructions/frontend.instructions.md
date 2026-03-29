---
applyTo: "**/*.{vue,js,ts,css,scss,sass,less}"
description: "JavaScript, Vue development guidelines with context engineering"
---
# Vue 3 Frontend Development Guidelines

## Context Loading
Review these BEFORE starting:
- [UI/UX Spec](../specs/ui-ux.spec.md) — Design system, component patterns, page layouts
- [Component Spec Template](../specs/component.spec.md) — Component structure template
- [Architecture Spec](../specs/architecture.spec.md) — API endpoints and DTOs
- [Client AGENTS.md](../../client/AGENTS.md) — Build/run instructions and patterns

## Component Structure (MANDATORY)

### Single-File Component Template
Every component must use `<script setup>` and handle all 4 states: loading, error, content, empty.

```vue
<template>
  <div class="component-name" data-testid="component-name">
    <div v-if="isLoading" class="loading-state">Loading...</div>
    <div v-else-if="error" class="error-state">{{ error }}</div>
    <div v-else-if="hasData" class="content"><!-- Content --></div>
    <div v-else class="empty-state" data-testid="empty-state">No data available</div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'

const props = defineProps({
  data: { type: Array, default: () => [] },
  loading: { type: Boolean, default: false }
})

const emit = defineEmits(['update', 'close', 'error'])

const isLoading = ref(false)
const error = ref(null)
const hasData = computed(() => props.data?.length > 0)
</script>

<style scoped>
.component-name {
  background-color: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--border-radius-md);
  padding: var(--spacing-md);
}
</style>
```

## Naming Conventions (STRICT)

### File Naming
- **Components**: `PascalCase.vue` (e.g., `BaseButton.vue`, `WinrateChart.vue`)
- **Pages/Views**: `PascalCasePage.vue` (e.g., `SoloStatsPage.vue`, `AuthPage.vue`)
- **Composables**: `camelCase.js` with `use` prefix (e.g., `useWinRateColor.js`, `useSyncWebSocket.js`)
- **Stores**: `camelCaseStore.js` (e.g., `authStore.js`, `uiStore.js`)
- **Services**: `camelCaseApi.js` (e.g., `authApi.js`, `analyticsApi.js`)
- **Utils**: `camelCase.js` (e.g., `formatters.js`, `leagueAssets.js`)

### Component Organization
```
components/
├── base/           # Reusable primitives (BaseButton, BaseModal, BaseInput)
├── overview/       # Overview dashboard specific
├── solo/           # Solo dashboard specific
├── matches/        # Match history specific
└── shared/         # Shared across multiple pages (AnalysisLayout)
```

## State Management

### Pinia Store Pattern
- Use `defineStore` with options API (state, getters, actions)
- Every store needs `isLoading` and `error` state
- Actions must set `isLoading`/`error` with try/catch/finally
- Use `console.error` (never `console.log`) in catch blocks

**Existing Stores**:
- `authStore` — User session, authentication state, user profile
- `uiStore` — Sidebar collapse state, UI preferences

## API Integration

### Service Pattern
- All API calls go through service modules in `services/`
- Use `apiRequest` and `parseResponse` from `apiClient.js`
- Handle 404 responses (return null)
- Use JSDoc comments for parameters and return types
- File naming: `camelCaseApi.js` (e.g., `authApi.js`, `matchesApi.js`)

## Error Handling
All async operations must use `isLoading`/`error`/`data` pattern with try/catch/finally (see component template above). Use `console.error` for caught errors, never `console.log`.

## Styling Guidelines

Use CSS custom properties (design tokens) for all themed values. See [UI/UX Spec — Design Tokens](../specs/ui-ux.spec.md#2-design-tokens-css-variables) for the complete token reference.

- Use **Tailwind** for layout and sizing (`flex`, `gap-4`, `p-4`)
- Use **scoped `<style>`** with CSS variables for themed colors and borders
- Available token categories: colors (`--color-*`, `--text-*`), spacing (`--spacing-*`), border radius (`--border-radius-*`), typography (`--font-size-*`)

## Accessibility (WCAG AA)

### Required Attributes
```vue
<template>
  <!-- Semantic HTML -->
  <nav aria-label="Main navigation">
    <button
      aria-label="Close modal"
      @click="closeModal"
    >
      <XIcon aria-hidden="true" />
    </button>
  </nav>
  
  <!-- Form labels -->
  <label for="email">Email</label>
  <input
    id="email"
    type="email"
    aria-describedby="email-error"
    aria-invalid="!!error"
  />
  <span id="email-error" v-if="error">{{ error }}</span>
  
  <!-- Testing attributes -->
  <div data-testid="my-component">
    Content
  </div>
</template>
```

**Checklist**:
- [ ] Use semantic HTML (`<button>`, `<nav>`, `<main>`, `<section>`)
- [ ] Include `aria-label` for icon-only buttons
- [ ] Add `data-testid` for all interactive elements
- [ ] Ensure keyboard navigation (Tab, Enter, Escape)
- [ ] Maintain color contrast ratios (4.5:1 for text)
- [ ] Form inputs have associated labels
- [ ] Error messages are programmatically associated

## Chart Components

### Chart.js Rules
- Use `vue-chartjs` wrappers (`Line`, `Bar`, `Doughnut`)
- Register only the Chart.js components you need (`CategoryScale`, `LinearScale`, etc.)
- Always check `hasData` before rendering — show empty state when no data
- Use `responsive: true` and `maintainAspectRatio: false`
- Chart colors: use project palette (`#6d28d9` purple primary) with transparency for fills
- Grid/tick colors: `rgba(255, 255, 255, 0.05)` for grid, `#888888` for ticks
- Wrap chart in a container with `min-height: 200px` and `data-testid`

## Testing Requirements

### Unit Test Rules
- Use Vitest + Vue Test Utils. See [testing.instructions.md](testing.instructions.md) for full patterns.
- Mock external dependencies (`vue-chartjs`, API services) with `vi.mock()`
- Create a `mountComponent` helper with sensible defaults
- Test rendering (all 4 states), user interactions, and emitted events
- Use `data-testid` selectors (not CSS classes) for assertions

## Code Checklist

Before submitting code:
- [ ] Component uses Vue 3 Composition API (`<script setup>`)
- [ ] Props have type validation and defaults
- [ ] Emits are declared
- [ ] Loading/error/empty states handled
- [ ] Data-testid attributes on all interactive elements
- [ ] Accessibility attributes (aria-label, semantic HTML)
- [ ] CSS custom properties for themed values
- [ ] Unit tests cover rendering + user interactions
- [ ] JSDoc comments on exported functions
- [ ] No console.log statements (use console.error for errors)
