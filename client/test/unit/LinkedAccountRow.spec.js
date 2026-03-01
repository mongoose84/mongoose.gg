import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import LinkedAccountRow from '@/components/settings/LinkedAccountRow.vue'

describe('LinkedAccountRow.vue', () => {
  const baseAccount = {
    puuid: 'puuid-1',
    gameName: 'MainPlayer',
    tagLine: 'EUW',
    region: 'euw1',
    isPrimary: false,
    soloTier: 'DIAMOND',
    soloRank: 'II',
    soloLp: 67,
    flexTier: null,
    flexRank: null,
    flexLp: null,
    lastSyncAt: new Date(Date.now() - 60 * 60 * 1000).toISOString()
  }

  it('renders account data correctly', () => {
    const wrapper = mount(LinkedAccountRow, {
      props: { account: baseAccount }
    })

    expect(wrapper.find('[data-testid="account-name"]').text()).toContain('MainPlayer#EUW')
    expect(wrapper.text()).toContain('EUW1')
    expect(wrapper.text()).not.toContain('DIAMOND')
    expect(wrapper.text()).not.toContain('67LP')
  })

  it('shows primary badge and hides set primary button for primary account', () => {
    const wrapper = mount(LinkedAccountRow, {
      props: {
        account: {
          ...baseAccount,
          isPrimary: true
        }
      }
    })

    expect(wrapper.find('[data-testid="primary-badge"]').exists()).toBe(true)
    expect(wrapper.find('[data-testid="set-primary-button"]').exists()).toBe(false)
    expect(wrapper.text()).not.toContain('DIAMOND')
    expect(wrapper.text()).not.toContain('67LP')
  })
})
