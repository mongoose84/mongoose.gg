import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import ChampionHero from '@/components/base/ChampionHero.vue'

function mountHero(props = {}, slots = {}) {
  return mount(ChampionHero, {
    props: {
      headline: "You've won 7 of 12 matches this week",
      text: 'You win 64% of matches with 4 or fewer deaths.',
      playerLine: 'Faker#KR1 · Ahri main · Gold II · 45 LP',
      championName: 'Ahri',
      chips: ['12 matches this week', '58% win rate'],
      ...props
    },
    slots
  })
}

describe('ChampionHero', () => {
  it('renders the headline as the page h1 with the supporting text and player line', () => {
    const wrapper = mountHero()
    expect(wrapper.get('h1').text()).toBe("You've won 7 of 12 matches this week")
    expect(wrapper.get('[data-testid="champion-hero-text"]').text()).toContain('64%')
    expect(wrapper.get('[data-testid="champion-hero-player"]').text()).toBe('Faker#KR1 · Ahri main · Gold II · 45 LP')
  })

  it('shows the champion splash with alt text', () => {
    const art = mountHero().get('[data-testid="champion-hero-art"]')
    expect(art.attributes('src')).toBe('https://ddragon.leagueoflegends.com/cdn/img/champion/splash/Ahri_0.jpg')
    expect(art.attributes('alt')).toBe('Ahri splash art')
  })

  it('falls back to a plain card without a champion or when the art fails', async () => {
    const plain = mountHero({ championName: null })
    expect(plain.find('[data-testid="champion-hero-art"]').exists()).toBe(false)
    expect(plain.classes()).toContain('champion-hero--plain')

    const broken = mountHero()
    await broken.get('[data-testid="champion-hero-art"]').trigger('error')
    expect(broken.find('[data-testid="champion-hero-art"]').exists()).toBe(false)
  })

  it('shows at most two glass chips', () => {
    const chips = mountHero({ chips: ['a', 'b', 'c'] }).findAll('[data-testid="champion-hero-chip"]')
    expect(chips.map((chip) => chip.text())).toEqual(['a', 'b'])
    expect(mountHero({ chips: [] }).find('[data-testid="champion-hero-chip"]').exists()).toBe(false)
  })

  it('renders the action slot', () => {
    const wrapper = mountHero({}, { action: '<a data-testid="action" href="/app/matches">See your matches</a>' })
    expect(wrapper.find('[data-testid="action"]').exists()).toBe(true)
  })
})
