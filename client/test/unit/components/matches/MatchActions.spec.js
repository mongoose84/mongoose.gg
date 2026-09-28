import { describe, it, expect } from 'vitest'
import { mount, RouterLinkStub } from '@vue/test-utils'
import MatchActions from '@/components/matches/MatchActions.vue'

function mountActions() {
  return mount(MatchActions, {
    props: { match: { matchId: 'EUW1_1' } },
    global: { stubs: { RouterLink: RouterLinkStub, BaseIcon: true } }
  })
}

describe('MatchActions', () => {
  it('renders the next-step card', () => {
    const wrapper = mountActions()
    expect(wrapper.find('[data-testid="match-actions"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('Is this match a one-off?')
  })

  it('links to the Solo trends with a secondary button', () => {
    const wrapper = mountActions()
    const link = wrapper.getComponent(RouterLinkStub)
    expect(link.props('to')).toEqual({ name: 'app-solo' })
    expect(wrapper.get('[data-testid="match-actions-trends"]').text()).toContain('See your trends')
  })

  it('no longer offers the unfinished goal impact action', () => {
    expect(mountActions().text()).not.toContain('Goal Impact')
  })
})
