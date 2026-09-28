import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import { createRouter, createMemoryHistory } from 'vue-router'
import AppTabBar from '@/components/AppTabBar.vue'

async function createWrapper(initialPath = '/app/overview') {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/app/overview', component: { template: '<div>Overview</div>' } },
      { path: '/app/champion-select', component: { template: '<div>Champion Select</div>' } },
      { path: '/app/matches/:matchId?', component: { template: '<div>Matches</div>' } },
      { path: '/app/solo', component: { template: '<div>Solo</div>' } }
    ]
  })
  router.push(initialPath)
  await router.isReady()

  return mount(AppTabBar, {
    global: { plugins: [router] }
  })
}

describe('AppTabBar.vue', () => {
  it('renders a nav labelled Main with the four tabs', async () => {
    const wrapper = await createWrapper()
    const nav = wrapper.get('[data-testid="app-tabbar"]')
    expect(nav.attributes('aria-label')).toBe('Main')

    expect(wrapper.find('[data-testid="tab-overview"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="tab-champion-select"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="tab-matches"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="tab-solo"]').exists()).toBe(true)
  })

  it('shortens Champion Select to Champ Select', async () => {
    const wrapper = await createWrapper()
    expect(wrapper.get('[data-testid="tab-champion-select"]').text()).toContain('Champ Select')
  })

  it('marks the active tab with aria-current="page"', async () => {
    const wrapper = await createWrapper('/app/solo')
    expect(wrapper.get('[data-testid="tab-solo"]').attributes('aria-current')).toBe('page')
    expect(wrapper.get('[data-testid="tab-overview"]').attributes('aria-current')).toBeUndefined()
  })

  it('keeps the Matches tab current on an open match', async () => {
    const wrapper = await createWrapper('/app/matches/EUW1_1')
    expect(wrapper.get('[data-testid="tab-matches"]').attributes('aria-current')).toBe('page')
  })
})
