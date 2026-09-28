import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import InsightCard from '@/components/base/InsightCard.vue'

function mountCard(props = {}) {
  return mount(InsightCard, {
    props: {
      kind: 'pattern',
      title: 'You win 64% of matches with 4 or fewer deaths',
      text: 'With 7 or more it drops to 38%.',
      championName: 'Ahri',
      ...props
    }
  })
}

describe('InsightCard', () => {
  it('renders the finding and its evidence', () => {
    const wrapper = mountCard()
    expect(wrapper.get('[data-testid="insight-card-title"]').text()).toBe('You win 64% of matches with 4 or fewer deaths')
    expect(wrapper.get('[data-testid="insight-card-text"]').text()).toBe('With 7 or more it drops to 38%.')
  })

  it.each([
    ['strength', 'Strength', 'trending-up', 'mp-chip--strength'],
    ['pattern', 'Pattern', 'repeat', 'mp-chip--pattern'],
    ['trend', 'Trend', 'activity', 'mp-chip--trend']
  ])('labels a %s chip with its word and icon', (kind, label, icon, chipClass) => {
    const chip = mountCard({ kind }).get('[data-testid="insight-card-chip"]')
    expect(chip.text()).toBe(label)
    expect(chip.classes()).toContain(chipClass)
    expect(chip.get('[data-testid="base-icon"]').attributes('data-icon')).toBe(icon)
  })

  it('shows the champion icon and hides it when missing or broken', async () => {
    const wrapper = mountCard()
    const icon = wrapper.get('[data-testid="insight-card-icon"]')
    expect(icon.attributes('src')).toContain('/img/champion/Ahri.png')

    await icon.trigger('error')
    expect(wrapper.find('[data-testid="insight-card-icon"]').exists()).toBe(false)
    expect(mountCard({ championName: null }).find('[data-testid="insight-card-icon"]').exists()).toBe(false)
  })
})
