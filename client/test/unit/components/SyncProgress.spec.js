import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import SyncProgress from '@/components/base/SyncProgress.vue'

describe('SyncProgress', () => {
  it('shows determinate progress when the total is known', () => {
    const wrapper = mount(SyncProgress, { props: { state: 'running', current: 12, total: 40 } })
    expect(wrapper.get('[data-testid="sync-progress-title"]').text()).toBe('Syncing 12 of 40 matches')

    const bar = wrapper.get('[data-testid="sync-progress-bar"]')
    expect(bar.attributes('role')).toBe('progressbar')
    expect(bar.attributes('aria-valuenow')).toBe('12')
    expect(bar.attributes('aria-valuemax')).toBe('40')
    expect(bar.get('span').attributes('style')).toContain('width: 30%')
  })

  it('shows an indeterminate bar while the total is unknown', () => {
    const wrapper = mount(SyncProgress, { props: { state: 'running', current: 0, total: 0 } })
    expect(wrapper.get('[data-testid="sync-progress-title"]').text()).toBe('Looking for new matches…')

    const bar = wrapper.get('[data-testid="sync-progress-bar"]')
    expect(bar.classes()).toContain('sync-progress__bar--indeterminate')
    expect(bar.attributes('aria-valuenow')).toBeUndefined()
  })

  it('announces progress politely, at most every five matches', () => {
    const live = mount(SyncProgress, { props: { state: 'running', current: 13, total: 40 } })
      .get('[data-testid="sync-progress-live"]')
    expect(live.attributes('aria-live')).toBe('polite')
    expect(live.text()).toBe('Syncing 10 of 40 matches')

    expect(mount(SyncProgress, { props: { state: 'running', current: 40, total: 40 } })
      .get('[data-testid="sync-progress-live"]').text()).toBe('Syncing 40 of 40 matches')
  })

  it('explains a rate-limit wait', () => {
    const wrapper = mount(SyncProgress, { props: { state: 'waiting' } })
    expect(wrapper.get('[data-testid="sync-progress-title"]').text()).toBe('Waiting on Riot’s servers…')
    expect(wrapper.get('[data-testid="sync-progress-detail"]').text()).toBe('We’ll continue automatically.')
  })

  it('shows the finished count without a bar', () => {
    const wrapper = mount(SyncProgress, { props: { state: 'done', syncedCount: 1 } })
    expect(wrapper.get('[data-testid="sync-progress-title"]').text()).toBe('Synced 1 match · just now')
    expect(wrapper.find('[data-testid="sync-progress-bar"]').exists()).toBe(false)
  })

  it('shows a failure as an error message with retry', async () => {
    const wrapper = mount(SyncProgress, { props: { state: 'failed' } })
    expect(wrapper.find('[data-testid="sync-progress"]').exists()).toBe(false)
    expect(wrapper.get('[data-testid="sync-progress-error"]').attributes('role')).toBe('alert')

    await wrapper.get('[data-testid="sync-progress-retry"]').trigger('click')
    expect(wrapper.emitted('retry')).toHaveLength(1)
  })
})
