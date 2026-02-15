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
```vue
<template>
  <div class="component-name" data-testid="component-name">
    <!-- Always include data-testid for testing -->
    
    <!-- Loading State -->
    <div v-if="isLoading" class="loading-state">
      <span>Loading...</span>
    </div>
    
    <!-- Error State -->
    <div v-else-if="error" class="error-state">
      <p>{{ error }}</p>
    </div>
    
    <!-- Content -->
    <div v-else-if="hasData" class="content">
      <!-- Component content -->
    </div>
    
    <!-- Empty State -->
    <div v-else class="empty-state" data-testid="empty-state">
      <p>No data available</p>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'

// Props with validation
const props = defineProps({
  /** Array of data items to display */
  data: {
    type: Array,
    default: () => []
  },
  /** Loading state from parent */
  loading: {
    type: Boolean,
    default: false
  },
  /** Variant for styling */
  variant: {
    type: String,
    default: 'primary',
    validator: (v) => ['primary', 'secondary', 'ghost'].includes(v)
  }
})

// Emits declaration
const emit = defineEmits(['update', 'close', 'error'])

// Local reactive state
const localData = ref(null)
const isLoading = ref(false)
const error = ref(null)

// Computed properties
const hasData = computed(() => props.data && props.data.length > 0)

// Methods
async function fetchData() {
  isLoading.value = true
  error.value = null
  try {
    localData.value = await api.getData()
    emit('update', localData.value)
  } catch (err) {
    console.error('Failed to fetch data:', err)
    error.value = err.message
    emit('error', err)
  } finally {
    isLoading.value = false
  }
}

// Lifecycle
onMounted(() => {
  // Initialization
})

// Watchers
watch(() => props.data, (newData) => {
  // React to prop changes
})
</script>

<style scoped>
/* Component-specific styles */
.component-name {
  /* Use CSS custom properties for theming */
  background-color: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--border-radius-md);
  padding: var(--spacing-md);
}

.loading-state,
.error-state,
.empty-state {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 200px;
  color: var(--text-secondary);
}
</style>
```

## Naming Conventions (STRICT)

### File Naming
- **Components**: `PascalCase.vue` (e.g., `BaseButton.vue`, `WinrateChart.vue`)
- **Pages/Views**: `PascalCasePage.vue` (e.g., `SoloPage.vue`, `AuthPage.vue`)
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
```javascript
import { defineStore } from 'pinia'

export const useMyStore = defineStore('myStore', {
  state: () => ({
    data: null,
    isLoading: false,
    error: null
  }),
  
  getters: {
    hasData: (state) => state.data !== null,
    dataCount: (state) => state.data?.length ?? 0
  },
  
  actions: {
    async fetchData() {
      this.isLoading = true
      this.error = null
      try {
        this.data = await api.getData()
      } catch (err) {
        console.error('Failed to fetch data:', err)
        this.error = err.message
        throw err
      } finally {
        this.isLoading = false
      }
    },
    
    clearData() {
      this.data = null
      this.error = null
    }
  }
})
```

**Existing Stores**:
- `authStore` — User session, authentication state, user profile
- `uiStore` — Sidebar collapse state, UI preferences

## API Integration

### Service Pattern
```javascript
// services/myApi.js
import { apiRequest, parseResponse } from './apiClient'

/**
 * Get dashboard data for a user
 * @param {number} userId - User ID
 * @param {string} [queueType] - Optional queue filter
 * @param {string} [timeRange] - Optional time range
 * @returns {Promise<Object>} Dashboard data
 */
export async function getDashboardData(userId, queueType = 'all', timeRange) {
  const params = new URLSearchParams()
  if (queueType && queueType !== 'all') {
    params.append('queueType', queueType)
  }
  if (timeRange) {
    params.append('timeRange', timeRange)
  }
  
  const endpoint = `/my-resource/${userId}${
    params.toString() ? '?' + params.toString() : ''
  }`
  const response = await apiRequest(endpoint, { method: 'GET' })
  
  if (response.status === 404) {
    return null // No data found
  }
  
  return parseResponse(response, 'Failed to get dashboard data')
}

/**
 * Update user settings
 * @param {number} userId - User ID
 * @param {Object} settings - Settings to update
 * @returns {Promise<Object>} Updated settings
 */
export async function updateSettings(userId, settings) {
  const response = await apiRequest(`/my-resource/${userId}`, {
    method: 'PUT',
    body: JSON.stringify(settings)
  })
  
  return parseResponse(response, 'Failed to update settings')
}
```

**Rules**:
- All API calls go through service modules in `services/`
- Use `apiRequest` from `apiClient.js` for standardized error handling
- Always handle 404 responses (return null)
- Use JSDoc comments for parameters and return types

## Error Handling (MANDATORY)

### Always Include Loading/Error States
```javascript
const data = ref(null)
const isLoading = ref(false)
const error = ref(null)

async function fetchData() {
  isLoading.value = true
  error.value = null
  try {
    data.value = await api.getData()
  } catch (err) {
    console.error('Failed to fetch data:', err)
    error.value = err.message
  } finally {
    isLoading.value = false
  }
}
```

### Template Error Display
```vue
<template>
  <div v-if="isLoading">Loading...</div>
  <div v-else-if="error" class="error-message">{{ error }}</div>
  <div v-else-if="hasData">
    <!-- Content -->
  </div>
  <div v-else class="empty-state">No data available</div>
</template>
```

## Styling Guidelines

### Use CSS Custom Properties (Design Tokens)
```vue
<style scoped>
.my-component {
  /* Colors */
  background-color: var(--color-surface);
  color: var(--text-primary);
  border-color: var(--color-border);
  
  /* Spacing */
  padding: var(--spacing-md);
  gap: var(--spacing-sm);
  
  /* Typography */
  font-size: var(--font-size-base);
  
  /* Border radius */
  border-radius: var(--border-radius-md);
}
</style>
```

### Tailwind + Scoped Styles
```vue
<template>
  <!-- Use Tailwind for layout/sizing -->
  <div class="flex items-center gap-4 p-4">
    <!-- Use scoped styles for themed colors -->
    <div class="card">
      Content
    </div>
  </div>
</template>

<style scoped>
.card {
  background-color: var(--color-surface);
  border: 1px solid var(--color-border);
}
</style>
```

**Available Design Tokens** (see `src/style.css`):
- Colors: `--color-surface`, `--color-border`, `--text-primary`, `--text-secondary`
- Spacing: `--spacing-xs`, `--spacing-sm`, `--spacing-md`, `--spacing-lg`
- Border radius: `--border-radius-sm`, `--border-radius-md`, `--border-radius-lg`

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

### Chart.js Integration Pattern
```vue
<template>
  <div class="chart-wrapper" data-testid="my-chart">
    <div v-if="hasData" class="chart-container">
      <Line :data="chartData" :options="chartOptions" />
    </div>
    <div v-else class="empty-state" data-testid="empty-state">
      <p>No data available</p>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { Line } from 'vue-chartjs'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler
} from 'chart.js'
import annotationPlugin from 'chartjs-plugin-annotation'

// Register Chart.js components
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler,
  annotationPlugin
)

const props = defineProps({
  data: { type: Array, default: () => [] }
})

const hasData = computed(() => props.data && props.data.length > 0)

const chartData = computed(() => {
  if (!hasData.value) return { labels: [], datasets: [] }
  
  return {
    labels: props.data.map(d => d.label),
    datasets: [{
      label: 'My Data',
      data: props.data.map(d => d.value),
      borderColor: '#6d28d9',
      backgroundColor: '#6d28d920'
    }]
  }
})

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false },
    tooltip: {
      backgroundColor: 'rgba(0, 0, 0, 0.9)',
      callbacks: {
        title: (items) => `Point ${items[0].dataIndex}`,
        label: (context) => `Value: ${context.parsed.y}`
      }
    }
  },
  scales: {
    x: {
      display: true,
      grid: { color: 'rgba(255, 255, 255, 0.05)' },
      ticks: { color: '#888888' }
    },
    y: {
      display: true,
      grid: { color: 'rgba(255, 255, 255, 0.05)' },
      ticks: { color: '#888888' }
    }
  }
}))
</script>

<style scoped>
.chart-wrapper {
  height: 100%;
}

.chart-container {
  min-height: 200px;
  position: relative;
}
</style>
```

## Testing Requirements

### Unit Test Pattern (MANDATORY)
```javascript
import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import MyComponent from '@/components/MyComponent.vue'

// Mock external dependencies
vi.mock('vue-chartjs', () => ({
  Line: {
    name: 'Line',
    props: ['data', 'options'],
    template: '<div data-testid="mock-chart"></div>'
  }
}))

describe('MyComponent', () => {
  const mountComponent = (props = {}) => {
    return mount(MyComponent, {
      props: {
        data: [],
        ...props
      }
    })
  }
  
  describe('Rendering', () => {
    it('renders with data', () => {
      const wrapper = mountComponent({ data: [1, 2, 3] })
      expect(wrapper.find('[data-testid="my-component"]').exists()).toBe(true)
    })
    
    it('shows empty state when no data', () => {
      const wrapper = mountComponent({ data: [] })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
    })
  })
  
  describe('User Interactions', () => {
    it('emits update event when button clicked', async () => {
      const wrapper = mountComponent()
      await wrapper.find('button').trigger('click')
      expect(wrapper.emitted('update')).toBeTruthy()
    })
  })
})
```

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
