import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import MatchRow from '@/components/matches/MatchRow.vue'

// Mock TrendBadge to avoid dependency concerns
vi.mock('@/components/matches/TrendBadge.vue', () => ({
  default: {
    name: 'TrendBadge',
    props: ['badge'],
    template: '<div data-testid="mock-trend-badge"></div>'
  }
}))

vi.mock('@/utils/formatters', () => ({
  formatRole: (role) => role,
  formatKda: (k, d, a) => `${k}/${d}/${a}`,
  formatDuration: (sec) => `${Math.floor(sec / 60)}m`,
  formatRelativeTime: () => '2h ago'
}))

// Mock authStore with configurable riotAccounts
const mockRiotAccounts = vi.fn(() => [])
vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => ({
    riotAccounts: mockRiotAccounts()
  })
}))

describe('MatchRow.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    mockRiotAccounts.mockReturnValue([])
  })

  const baseMatch = {
    matchId: 'EUW1_123456789',
    win: true,
    championName: 'Jinx',
    championIconUrl: 'https://example.com/jinx.png',
    role: 'ADC',
    queueType: 'Ranked Solo',
    kills: 12,
    deaths: 3,
    assists: 8,
    gameDurationSec: 1800,
    gameStartTime: Date.now() - 7200000,
    trendBadge: null
  }

  const createWrapper = (matchOverrides = {}, props = {}) => {
    return mount(MatchRow, {
      props: {
        match: { ...baseMatch, ...matchOverrides },
        selected: false,
        ...props
      }
    })
  }

  describe('Account Tag', () => {
    it('renders account icon wrapper when accountGameName is provided', () => {
      const wrapper = createWrapper({ accountGameName: 'FakerMain', accountRegion: 'euw' })
      expect(wrapper.find('[data-testid="account-tag"]').exists()).toBe(true)
    })

    it('does not render account icon when accountGameName is absent', () => {
      const wrapper = createWrapper()
      expect(wrapper.find('[data-testid="account-tag"]').exists()).toBe(false)
    })

    it('does not render account icon when accountGameName is null', () => {
      const wrapper = createWrapper({ accountGameName: null })
      expect(wrapper.find('[data-testid="account-tag"]').exists()).toBe(false)
    })

    it('does not render account icon when accountGameName is empty string', () => {
      const wrapper = createWrapper({ accountGameName: '' })
      expect(wrapper.find('[data-testid="account-tag"]').exists()).toBe(false)
    })

    it('sets tooltip with "GameName · REGION" format', () => {
      const wrapper = createWrapper({ accountGameName: 'FakerMain', accountRegion: 'euw' })
      const tag = wrapper.find('[data-testid="account-tag"]')
      expect(tag.attributes('title')).toBe('FakerMain · EUW')
    })

    it('sets tooltip with only game name when region is absent', () => {
      const wrapper = createWrapper({ accountGameName: 'FakerMain' })
      const tag = wrapper.find('[data-testid="account-tag"]')
      expect(tag.attributes('title')).toBe('FakerMain')
    })

    it('shows profile icon when matching account with profileIconId is in store', () => {
      mockRiotAccounts.mockReturnValue([
        { gameName: 'FakerMain', region: 'euw', profileIconId: 1234 }
      ])
      const wrapper = createWrapper({ accountGameName: 'FakerMain', accountRegion: 'euw' })
      const img = wrapper.find('[data-testid="account-tag"] img')
      expect(img.exists()).toBe(true)
      expect(img.attributes('src')).toContain('1234')
    })

    it('shows fallback svg when no matching account in store', () => {
      mockRiotAccounts.mockReturnValue([])
      const wrapper = createWrapper({ accountGameName: 'FakerMain', accountRegion: 'euw' })
      expect(wrapper.find('[data-testid="account-tag"] img').exists()).toBe(false)
      expect(wrapper.find('[data-testid="account-tag"] .account-icon-fallback').exists()).toBe(true)
    })

    it('account icon wrapper is not a button or anchor', () => {
      const wrapper = createWrapper({ accountGameName: 'FakerMain', accountRegion: 'euw' })
      const tag = wrapper.find('[data-testid="account-tag"]')
      expect(tag.element.tagName).not.toBe('A')
      expect(tag.element.tagName).not.toBe('BUTTON')
    })

    it('matches account case-insensitively', () => {
      mockRiotAccounts.mockReturnValue([
        { gameName: 'FakerMain', region: 'EUW', profileIconId: 999 }
      ])
      const wrapper = createWrapper({ accountGameName: 'fakermain', accountRegion: 'euw' })
      const img = wrapper.find('[data-testid="account-tag"] img')
      expect(img.exists()).toBe(true)
    })
  })

  describe('Match Row Basics', () => {
    it('renders champion name', () => {
      const wrapper = createWrapper()
      expect(wrapper.text()).toContain('Jinx')
    })

    it('emits select event with matchId on click', async () => {
      const wrapper = createWrapper()
      await wrapper.trigger('click')
      expect(wrapper.emitted('select')).toBeTruthy()
      expect(wrapper.emitted('select')[0]).toEqual([baseMatch.matchId])
    })

    it('applies win class for winning matches', () => {
      const wrapper = createWrapper({ win: true })
      expect(wrapper.find('.match-row').classes()).toContain('win')
    })

    it('applies loss class for losing matches', () => {
      const wrapper = createWrapper({ win: false })
      expect(wrapper.find('.match-row').classes()).toContain('loss')
    })

    it('applies selected class when selected prop is true', () => {
      const wrapper = createWrapper({}, { selected: true })
      expect(wrapper.find('.match-row').classes()).toContain('selected')
    })
  })
})
