import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ImpactStats from '@/components/matches/ImpactStats.vue'

describe('ImpactStats.vue', () => {
  const defaultProps = {
    role: 'MIDDLE',
    killParticipation: 55,
    goldAt15: 6500,
    damageGoldEfficiency: 1.3,
    visionPerMin: null,
    loading: false
  }

  const mountComponent = (overrides = {}) =>
    mount(ImpactStats, { props: { ...defaultProps, ...overrides } })

  // ── Loading state ──────────────────────────────────────────────────────────

  describe('loading state', () => {
    it('renders the loading skeleton when loading=true', () => {
      const wrapper = mountComponent({ loading: true })
      expect(wrapper.find('[data-testid="loading-state"]').exists()).toBe(true)
    })

    it('renders 3 skeleton elements', () => {
      const wrapper = mountComponent({ loading: true })
      expect(wrapper.findAll('.skeleton-stat')).toHaveLength(3)
    })

    it('does not render the stats grid while loading', () => {
      const wrapper = mountComponent({ loading: true })
      expect(wrapper.find('.stats-grid').exists()).toBe(false)
    })

    it('does not render the empty state while loading', () => {
      const wrapper = mountComponent({ loading: true })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(false)
    })
  })

  // ── Empty state ────────────────────────────────────────────────────────────

  describe('empty state', () => {
    it('renders the empty state when all data props are null', () => {
      const wrapper = mountComponent({
        killParticipation: null,
        goldAt15: null,
        damageGoldEfficiency: null,
        visionPerMin: null
      })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(true)
    })

    it('does not render the stats grid in empty state', () => {
      const wrapper = mountComponent({
        killParticipation: null,
        goldAt15: null,
        damageGoldEfficiency: null,
        visionPerMin: null
      })
      expect(wrapper.find('.stats-grid').exists()).toBe(false)
    })

    it('does not render the empty state when at least one data prop is provided', () => {
      const wrapper = mountComponent({ killParticipation: 40, goldAt15: null, damageGoldEfficiency: null })
      expect(wrapper.find('[data-testid="empty-state"]').exists()).toBe(false)
    })
  })

  // ── Non-support stat set ───────────────────────────────────────────────────

  describe('non-support role', () => {
    it('renders 3 stat cells', () => {
      const wrapper = mountComponent()
      expect(wrapper.findAll('.stat-cell')).toHaveLength(3)
    })

    it('renders Kill Participation stat', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="impact-stat-kp"]').exists()).toBe(true)
    })

    it('renders Gold @15 stat', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="impact-stat-gold15"]').exists()).toBe(true)
    })

    it('renders Dmg/Gold stat instead of Vision/min', () => {
      const wrapper = mountComponent()
      expect(wrapper.find('[data-testid="impact-stat-dmg-gold"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="impact-stat-vision"]').exists()).toBe(false)
    })

    it('formats kill participation as rounded percentage', () => {
      const wrapper = mountComponent({ killParticipation: 47.6 })
      expect(wrapper.find('[data-testid="impact-stat-kp"] .stat-value').text()).toBe('48%')
    })

    it('formats gold at 15 above 1000 with k suffix', () => {
      const wrapper = mountComponent({ goldAt15: 6500 })
      expect(wrapper.find('[data-testid="impact-stat-gold15"] .stat-value').text()).toBe('6.5k')
    })

    it('formats gold at 15 below 1000 as plain number', () => {
      const wrapper = mountComponent({ goldAt15: 850 })
      expect(wrapper.find('[data-testid="impact-stat-gold15"] .stat-value').text()).toBe('850')
    })

    it('formats Dmg/Gold to 2 decimal places', () => {
      const wrapper = mountComponent({ damageGoldEfficiency: 1.256 })
      expect(wrapper.find('[data-testid="impact-stat-dmg-gold"] .stat-value').text()).toBe('1.26')
    })

    it('shows dash when kill participation is null', () => {
      const wrapper = mountComponent({ killParticipation: null })
      expect(wrapper.find('[data-testid="impact-stat-kp"] .stat-value').text()).toBe('—')
    })

    it('shows dash when gold at 15 is null', () => {
      const wrapper = mountComponent({ goldAt15: null })
      expect(wrapper.find('[data-testid="impact-stat-gold15"] .stat-value').text()).toBe('—')
    })

    it('shows dash when Dmg/Gold is null', () => {
      const wrapper = mountComponent({ damageGoldEfficiency: null })
      expect(wrapper.find('[data-testid="impact-stat-dmg-gold"] .stat-value').text()).toBe('—')
    })
  })

  // ── Support role stat set ──────────────────────────────────────────────────

  describe('support role', () => {
    const supportProps = {
      role: 'support',
      killParticipation: 60,
      goldAt15: 5000,
      visionPerMin: 1.8,
      damageGoldEfficiency: null
    }

    it('renders Vision/min stat instead of Dmg/Gold', () => {
      const wrapper = mountComponent(supportProps)
      expect(wrapper.find('[data-testid="impact-stat-vision"]').exists()).toBe(true)
      expect(wrapper.find('[data-testid="impact-stat-dmg-gold"]').exists()).toBe(false)
    })

    it('formats vision per minute to 1 decimal place', () => {
      const wrapper = mountComponent({ ...supportProps, visionPerMin: 1.75 })
      expect(wrapper.find('[data-testid="impact-stat-vision"] .stat-value').text()).toBe('1.8')
    })

    it('shows dash when vision per minute is null', () => {
      const wrapper = mountComponent({ ...supportProps, visionPerMin: null })
      expect(wrapper.find('[data-testid="impact-stat-vision"] .stat-value').text()).toBe('—')
    })

    it('is role-insensitive (UTILITY maps to support)', () => {
      const wrapper = mountComponent({ ...supportProps, role: 'SUPPORT' })
      expect(wrapper.find('[data-testid="impact-stat-vision"]').exists()).toBe(true)
    })
  })

  // ── Sentiment class application ────────────────────────────────────────────

  describe('sentiment — Kill Participation (thresholds: positive ≥ 50, negative ≤ 35)', () => {
    it('applies positive class at exactly the good threshold (50)', () => {
      const wrapper = mountComponent({ killParticipation: 50 })
      expect(wrapper.find('[data-testid="impact-stat-kp"]').classes()).toContain('stat-cell--positive')
    })

    it('applies positive class above the good threshold (65)', () => {
      const wrapper = mountComponent({ killParticipation: 65 })
      expect(wrapper.find('[data-testid="impact-stat-kp"]').classes()).toContain('stat-cell--positive')
    })

    it('applies negative class at exactly the bad threshold (35)', () => {
      const wrapper = mountComponent({ killParticipation: 35 })
      expect(wrapper.find('[data-testid="impact-stat-kp"]').classes()).toContain('stat-cell--negative')
    })

    it('applies negative class below the bad threshold (20)', () => {
      const wrapper = mountComponent({ killParticipation: 20 })
      expect(wrapper.find('[data-testid="impact-stat-kp"]').classes()).toContain('stat-cell--negative')
    })

    it('applies no sentiment class in the neutral zone (42)', () => {
      const wrapper = mountComponent({ killParticipation: 42 })
      const cell = wrapper.find('[data-testid="impact-stat-kp"]')
      expect(cell.classes()).not.toContain('stat-cell--positive')
      expect(cell.classes()).not.toContain('stat-cell--negative')
    })

    it('applies no sentiment class when value is null', () => {
      const wrapper = mountComponent({ killParticipation: null })
      const cell = wrapper.find('[data-testid="impact-stat-kp"]')
      expect(cell.classes()).not.toContain('stat-cell--positive')
      expect(cell.classes()).not.toContain('stat-cell--negative')
    })
  })

  describe('sentiment — Gold @15 (thresholds: positive ≥ 6000, negative ≤ 4500)', () => {
    it('applies positive class at exactly the good threshold (6000)', () => {
      const wrapper = mountComponent({ goldAt15: 6000 })
      expect(wrapper.find('[data-testid="impact-stat-gold15"]').classes()).toContain('stat-cell--positive')
    })

    it('applies negative class at exactly the bad threshold (4500)', () => {
      const wrapper = mountComponent({ goldAt15: 4500 })
      expect(wrapper.find('[data-testid="impact-stat-gold15"]').classes()).toContain('stat-cell--negative')
    })

    it('applies no sentiment class in the neutral zone (5200)', () => {
      const wrapper = mountComponent({ goldAt15: 5200 })
      const cell = wrapper.find('[data-testid="impact-stat-gold15"]')
      expect(cell.classes()).not.toContain('stat-cell--positive')
      expect(cell.classes()).not.toContain('stat-cell--negative')
    })
  })

  describe('sentiment — Dmg/Gold (non-support, thresholds: positive ≥ 1.2, negative ≤ 0.7)', () => {
    it('applies positive class at exactly the good threshold (1.2)', () => {
      const wrapper = mountComponent({ damageGoldEfficiency: 1.2 })
      expect(wrapper.find('[data-testid="impact-stat-dmg-gold"]').classes()).toContain('stat-cell--positive')
    })

    it('applies negative class at exactly the bad threshold (0.7)', () => {
      const wrapper = mountComponent({ damageGoldEfficiency: 0.7 })
      expect(wrapper.find('[data-testid="impact-stat-dmg-gold"]').classes()).toContain('stat-cell--negative')
    })

    it('applies no sentiment class in the neutral zone (0.95)', () => {
      const wrapper = mountComponent({ damageGoldEfficiency: 0.95 })
      const cell = wrapper.find('[data-testid="impact-stat-dmg-gold"]')
      expect(cell.classes()).not.toContain('stat-cell--positive')
      expect(cell.classes()).not.toContain('stat-cell--negative')
    })
  })

  describe('sentiment — Vision/min (support, thresholds: positive ≥ 1.5, negative ≤ 0.8)', () => {
    const supportBase = { role: 'support', killParticipation: 50, goldAt15: 6000, damageGoldEfficiency: null }

    it('applies positive class at exactly the good threshold (1.5)', () => {
      const wrapper = mountComponent({ ...supportBase, visionPerMin: 1.5 })
      expect(wrapper.find('[data-testid="impact-stat-vision"]').classes()).toContain('stat-cell--positive')
    })

    it('applies negative class at exactly the bad threshold (0.8)', () => {
      const wrapper = mountComponent({ ...supportBase, visionPerMin: 0.8 })
      expect(wrapper.find('[data-testid="impact-stat-vision"]').classes()).toContain('stat-cell--negative')
    })

    it('applies no sentiment class in the neutral zone (1.1)', () => {
      const wrapper = mountComponent({ ...supportBase, visionPerMin: 1.1 })
      const cell = wrapper.find('[data-testid="impact-stat-vision"]')
      expect(cell.classes()).not.toContain('stat-cell--positive')
      expect(cell.classes()).not.toContain('stat-cell--negative')
    })
  })

  // ── Section title ──────────────────────────────────────────────────────────

  it('renders the section title "Impact Stats"', () => {
    const wrapper = mountComponent()
    expect(wrapper.find('.section-title').text()).toBe('Impact Stats')
  })

  it('renders root element with correct data-testid', () => {
    const wrapper = mountComponent()
    expect(wrapper.find('[data-testid="impact-stats"]').exists()).toBe(true)
  })
})
