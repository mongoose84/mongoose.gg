---
applyTo: "**/test/**"
description: "Testing guidelines with context engineering"
---
# Testing Guidelines

## Context Loading
Review these BEFORE writing tests:
- [Test Strategy Spec](../specs/test-strategy.spec.md) — Testing pyramid, patterns, infrastructure
- [Architecture Spec](../specs/architecture.spec.md) — API endpoints and expected responses
- [UI/UX Spec](../specs/ui-ux.spec.md) — Component behavior and interactions
- Existing tests in `server/Mongoose.Api.Tests/` and `client/test/unit/`

## Testing Pyramid

```
        /\
       /E2E\        (Few) - Critical user journeys only
      /______\
     / Backend \    (More) - API endpoint coverage
    /___API____\
   / Unit Tests \ (Many) - Components, utils, business logic
  /__BE + FE____\
```

## Backend Testing (xUnit + .NET 9)

### Integration Test Pattern (MANDATORY for all endpoints)
```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using System.Net;
using System.Text.Json;

public class MyEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;
    
    public MyEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task GetEndpoint_ReturnsData_WhenAuthenticated()
    {
        // Arrange
        var authenticatedClient = _factory.CreateAuthenticatedClient(userId: 1);
        
        // Act
        var response = await authenticatedClient.GetAsync("/api/v2/resource/1");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<MyResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        data.Should().NotBeNull();
        data!.UserId.Should().Be(1);
        data.GamesPlayed.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public async Task GetEndpoint_Returns401_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/api/v2/resource/1");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task GetEndpoint_Returns403_WhenAccessingOtherUsersData()
    {
        // Arrange
        var authenticatedClient = _factory.CreateAuthenticatedClient(userId: 1);
        
        // Act - trying to access user 2's data
        var response = await authenticatedClient.GetAsync("/api/v2/resource/2");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    
    [Fact]
    public async Task GetEndpoint_Returns404_WhenNoRiotAccountLinked()
    {
        // Arrange
        var authenticatedClient = _factory.CreateAuthenticatedClient(userId: 999);
        
        // Act
        var response = await authenticatedClient.GetAsync("/api/v2/resource/999");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

**TestWebApplicationFactory Methods**:
- `CreateClient()` — Unauthenticated HTTP client
- `CreateAuthenticatedClient(userId)` — Authenticated client with session cookie
- Access fake repositories via `FakeUserRepository`, `FakeRiotAccountsRepository`, etc.

### Unit Test Pattern
```csharp
public class MyServiceTests
{
    [Fact]
    public void CalculateAverage_ReturnsCorrectValue()
    {
        // Arrange
        var values = new[] { 5.0, 10.0, 15.0 };
        var service = new MyService();
        
        // Act
        var result = service.CalculateAverage(values);
        
        // Assert
        result.Should().BeApproximately(10.0, 0.01);
    }
    
    [Theory]
    [InlineData(new double[] { }, 0)]
    [InlineData(new[] { 5.0 }, 5.0)]
    [InlineData(new[] { 5.0, 15.0 }, 10.0)]
    public void CalculateAverage_HandlesEdgeCases(double[] values, double expected)
    {
        // Arrange
        var service = new MyService();
        
        // Act
        var result = service.CalculateAverage(values);
        
        // Assert
        result.Should().BeApproximately(expected, 0.01);
    }
}
```

## Frontend Testing (Vitest + Vue Test Utils)

### Component Unit Test Pattern
```javascript
import { describe, it, expect, vi, beforeEach } from 'vitest'
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

vi.mock('chart.js', () => ({
  Chart: { register: vi.fn() },
  CategoryScale: {},
  LinearScale: {},
  PointElement: {},
  LineElement: {},
  Title: {},
  Tooltip: {},
  Legend: {},
  Filler: {}
}))

describe('MyComponent', () => {
  // Sample data for all tests
  const sampleData = [
    { id: 1, name: 'Item 1', value: 100 },
    { id: 2, name: 'Item 2', value: 200 }
  ]
  
  // Helper function to mount component
  const mountComponent = (props = {}) => {
    return mount(MyComponent, {
      props: {
        data: sampleData,
        ...props
      }
    })
  }
  
  describe('Rendering', () => {
    it('renders component with data', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="my-component"]').exists()).toBe(true)
    })
    
    it('shows empty state when no data', () => {
      const wrapper = mountComponent({ data: [] })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
      expect(wrapper.text()).toContain('No data available')
    })
    
    it('shows loading state when loading', () => {
      const wrapper = mountComponent({ loading: true })
      expect(wrapper.find('[data-testid="loading-state"]').exists()).toBe(true)
    })
    
    it('displays error message when error occurs', () => {
      const errorMessage = 'Failed to load data'
      const wrapper = mountComponent({ error: errorMessage })
      expect(wrapper.text()).toContain(errorMessage)
    })
  })
  
  describe('User Interactions', () => {
    it('emits update event when button clicked', async () => {
      const wrapper = mountComponent()
      await wrapper.find('[data-testid="update-button"]').trigger('click')
      
      expect(wrapper.emitted('update')).toBeTruthy()
      expect(wrapper.emitted('update')![0]).toEqual([{ action: 'update' }])
    })
    
    it('calls handleClick method on button click', async () => {
      const wrapper = mountComponent()
      const handleClickSpy = vi.spyOn(wrapper.vm, 'handleClick')
      
      await wrapper.find('button').trigger('click')
      
      expect(handleClickSpy).toHaveBeenCalledTimes(1)
    })
  })
  
  describe('Computed Properties', () => {
    it('calculates hasData correctly', () => {
      const wrapper = mountComponent()
      expect(wrapper.vm.hasData).toBe(true)
      
      wrapper.setProps({ data: [] })
      expect(wrapper.vm.hasData).toBe(false)
    })
  })
  
  describe('Props Validation', () => {
    it('accepts valid variant prop', () => {
      const wrapper = mountComponent({ variant: 'primary' })
      expect(wrapper.props('variant')).toBe('primary')
    })
  })
})
```

### Chart Component Testing Pattern
```javascript
import { describe, it, expect, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import MyChart from '@/components/solo/MyChart.vue'

// Mock Chart.js
vi.mock('vue-chartjs', () => ({
  Line: {
    name: 'Line',
    props: ['data', 'options'],
    template: '<div data-testid="mock-line-chart" :data-chart-data="JSON.stringify(data)" :data-chart-options="JSON.stringify(options)"></div>'
  }
}))

vi.mock('chart.js', () => ({
  Chart: { register: vi.fn() },
  // ... register all required Chart.js components
}))

vi.mock('chartjs-plugin-annotation', () => ({ default: {} }))

describe('MyChart', () => {
  it('passes correct data to chart', () => {
    const data = [{ x: 1, y: 10 }, { x: 2, y: 20 }]
    const wrapper = mount(MyChart, { props: { data } })
    
    const chart = wrapper.find('[data-testid="mock-line-chart"]')
    const chartData = JSON.parse(chart.attributes('data-chart-data'))
    
    expect(chartData.datasets[0].data).toEqual([10, 20])
  })
  
  it('applies color based on trend', () => {
    const wrapper = mount(MyChart, {
      props: { data: [1, 2, 3], trend: 'improving' }
    })
    
    const chart = wrapper.find('[data-testid="mock-line-chart"]')
    const chartData = JSON.parse(chart.attributes('data-chart-data'))
    
    expect(chartData.datasets[0].borderColor).toBe('#22c55e') // Green
  })
})
```

### Store Testing Pattern
```javascript
import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useMyStore } from '@/stores/myStore'
import * as api from '@/services/myApi'

// Mock API
vi.mock('@/services/myApi')

describe('myStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })
  
  it('initializes with default state', () => {
    const store = useMyStore()
    
    expect(store.data).toBeNull()
    expect(store.isLoading).toBe(false)
    expect(store.error).toBeNull()
  })
  
  it('fetches data successfully', async () => {
    const mockData = { id: 1, name: 'Test' }
    vi.mocked(api.getData).mockResolvedValue(mockData)
    
    const store = useMyStore()
    await store.fetchData()
    
    expect(store.isLoading).toBe(false)
    expect(store.data).toEqual(mockData)
    expect(store.error).toBeNull()
  })
  
  it('handles fetch errors', async () => {
    const errorMessage = 'Network error'
    vi.mocked(api.getData).mockRejectedValue(new Error(errorMessage))
    
    const store = useMyStore()
    await expect(store.fetchData()).rejects.toThrow(errorMessage)
    
    expect(store.isLoading).toBe(false)
    expect(store.data).toBeNull()
    expect(store.error).toBe(errorMessage)
  })
})
```

## E2E Testing (Playwright)

### E2E Test Pattern
```javascript
import { test, expect } from '@playwright/test'

test.describe('Solo Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    // Login before each test
    await page.goto('/auth?mode=login')
    await page.fill('[data-testid="email-input"]', 'test@example.com')
    await page.fill('[data-testid="password-input"]', 'password123')
    await page.click('[data-testid="login-button"]')
    await expect(page).toHaveURL('/overview')
  })
  
  test('displays summary stats', async ({ page }) => {
    // Navigate to solo dashboard
    await page.click('[data-testid="solo-nav-link"]')
    await expect(page).toHaveURL('/solo')
    
    // Verify stats cards are visible
    await expect(page.locator('[data-testid="summary-stats-card"]')).toBeVisible()
    await expect(page.locator('[data-testid="winrate-trend-card"]')).toBeVisible()
  })
  
  test('filters by queue type', async ({ page }) => {
    await page.goto('/solo')
    
    // Change queue filter
    await page.click('[data-testid="queue-toggle-ranked-solo"]')
    
    // Wait for data to reload
    await page.waitForResponse(resp =>
      resp.url().includes('/solo/performance') && resp.status() === 200
    )
    
    // Verify URL updated with query param
    await expect(page).toHaveURL(/\?.*queueType=ranked_solo/)
  })
  
  test('handles error states', async ({ page }) => {
    // Mock error response
    await page.route('**/api/v2/solo/performance/**', route => {
      route.fulfill({ status: 500, body: JSON.stringify({
        error: 'Server error'
      })})
    })
    
    await page.goto('/solo')
    
    // Verify error message displayed
    await expect(page.locator('[data-testid="error-state"]')).toBeVisible()
  })
})
```

**E2E Prerequisites**:
- Backend must run with: `Auth__AutoVerifyEmail=true Email__DevMode=true dotnet run`
- Global setup creates test user (see `e2e/global-setup.js`)
- Global teardown deletes test user (see `e2e/global-teardown.js`)

## Test Organization

### Describe Blocks
Group related tests:
```javascript
describe('MyComponent', () => {
  describe('Rendering', () => {
    // All rendering tests
  })
  
  describe('User Interactions', () => {
    // All interaction tests
  })
  
  describe('Edge Cases', () => {
    // Edge case tests
  })
})
```

### Test Naming
Use descriptive names that explain the scenario:
```javascript
// ✅ GOOD
it('displays empty state when no data provided')
it('emits update event when save button clicked')
it('returns 401 when user not authenticated')

// ❌ BAD
it('works')
it('test component')
it('should render')
```

## Mocking Strategies

### Mock External Dependencies
```javascript
// Mock entire modules
vi.mock('@/services/myApi', () => ({
  getData: vi.fn(),
  updateData: vi.fn()
}))

// Mock Chart.js (always required for chart components)
vi.mock('vue-chartjs', () => ({
  Line: {
    name: 'Line',
    props: ['data', 'options'],
    template: '<div data-testid="mock-chart"></div>'
  }
}))

// Mock composables
vi.mock('@/composables/useMyComposable', () => ({
  useMyComposable: () => ({
    data: ref(mockData),
    isLoading: ref(false)
  })
}))
```

## Coverage Requirements

### Backend
- **Integration tests**: MANDATORY for all endpoints
- **Test scenarios**: Authentication, authorization, happy path, error cases, edge cases
- **Status codes**: 200, 201, 400, 401, 403, 404, 500

### Frontend
- **Component tests**: All components with logic
- **Test scenarios**: Rendering, empty states, loading states, error states, user interactions
- **Store tests**: All actions and complex getters
- **Utility tests**: All utility functions

### E2E
- **Critical paths**: Login, solo dashboard, match list
- **User journeys**: Complete flows from login to data display
- **Error handling**: Network errors, auth failures

## Test Checklist

Before submitting tests:
- [ ] AAA pattern (Arrange, Act, Assert)
- [ ] Descriptive test names
- [ ] All external dependencies mocked
- [ ] Happy path covered
- [ ] Error cases covered
- [ ] Edge cases covered (empty arrays, null values, etc.)
- [ ] No console.log statements
- [ ] Tests are deterministic (no random data)
- [ ] Cleanup via beforeEach/afterEach if needed
- [ ] Backend: Uses TestWebApplicationFactory
- [ ] Frontend: Uses data-testid for element selection
- [ ] E2E: Uses Playwright best practices (wait for responses, not timeouts)
