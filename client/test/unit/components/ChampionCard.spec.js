import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ChampionCard from '@/components/base/ChampionCard.vue'

function mountCard(props = {}) {
  return mount(ChampionCard, {
    props: {
      championName: 'Ahri',
      winRate: 63.6,
      matches: 22,
      avgKda: 4.1,
      strengthTag: 'Best laning',
      ...props
    }
  })
}

describe('ChampionCard', () => {
  it('shows name, rounded win rate, matches, KDA and strength tag', () => {
    const wrapper = mountCard()
    expect(wrapper.get('[data-testid="champion-card-name"]').text()).toBe('Ahri')
    expect(wrapper.get('[data-testid="champion-card-winrate"]').text()).toBe('64% win rate')
    expect(wrapper.get('[data-testid="champion-card-meta"]').text()).toBe('22 matches · KDA 4.1')
    expect(wrapper.get('[data-testid="champion-card-tag"]').text()).toBe('Best laning')
  })

  it('is a static article, not a button', () => {
    const wrapper = mountCard()
    expect(wrapper.element.tagName).toBe('ARTICLE')
    expect(wrapper.attributes('aria-pressed')).toBeUndefined()
    expect(wrapper.find('button').exists()).toBe(false)
  })

  it('hides the tag when the champion has none', () => {
    const wrapper = mountCard({ strengthTag: null })
    expect(wrapper.find('[data-testid="champion-card-tag"]').exists()).toBe(false)
  })

  it('uses singular "match" for one match', () => {
    const wrapper = mountCard({ matches: 1, avgKda: 3 })
    expect(wrapper.get('[data-testid="champion-card-meta"]').text()).toBe('1 match · KDA 3.0')
  })

  it('fills the win-rate bar to the win rate, clamped to 0–100', () => {
    expect(mountCard({ winRate: 57.5 }).get('[data-testid="champion-card-bar"]').attributes('style')).toContain('width: 57.5%')
    expect(mountCard({ winRate: 140 }).get('[data-testid="champion-card-bar"]').attributes('style')).toContain('width: 100%')
  })

  it('shows centred art and drops it when it fails to load', async () => {
    const wrapper = mountCard()
    const art = wrapper.get('[data-testid="champion-card-art"]')
    expect(art.attributes('src')).toContain('/img/champion/centered/Ahri_0.jpg')
    expect(art.attributes('alt')).toBe('')

    await art.trigger('error')
    expect(wrapper.find('[data-testid="champion-card-art"]').exists()).toBe(false)
  })

  describe('selectable', () => {
    it('is a pick button with aria-pressed and the spotlight', () => {
      const wrapper = mountCard({ selectable: true, selected: true })
      expect(wrapper.element.tagName).toBe('BUTTON')
      expect(wrapper.attributes('type')).toBe('button')
      expect(wrapper.attributes('aria-pressed')).toBe('true')
      expect(wrapper.classes()).toContain('mp-spotlight')
      expect(wrapper.get('[data-testid="champion-card-name"]').element.tagName).toBe('SPAN')
    })

    it('is not pressed when another card is selected', () => {
      expect(mountCard({ selectable: true }).attributes('aria-pressed')).toBe('false')
    })

    it('emits select on click', async () => {
      const wrapper = mountCard({ selectable: true })
      await wrapper.trigger('click')
      expect(wrapper.emitted('select')).toHaveLength(1)
    })

    it('moves the spotlight with the pointer', async () => {
      const wrapper = mountCard({ selectable: true })
      wrapper.element.getBoundingClientRect = () => ({ left: 10, top: 20 })
      wrapper.element.dispatchEvent(new MouseEvent('pointermove', { clientX: 60, clientY: 50 }))
      expect(wrapper.element.style.getPropertyValue('--mx')).toBe('50px')
      expect(wrapper.element.style.getPropertyValue('--my')).toBe('30px')
    })

    it('never emits select when static', async () => {
      const wrapper = mountCard()
      await wrapper.trigger('click')
      expect(wrapper.emitted('select')).toBeUndefined()
    })
  })
})
