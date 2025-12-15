import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import UserView from '@/views/UserView.vue'

describe('UserView', () => {
  it('shows prompt when user details are missing', async () => {
    const wrapper = mount(UserView, {
      props: { userName: '', userId: '' }
    })

    expect(wrapper.text()).toContain('Missing user details')
    expect(wrapper.text()).toContain('Please navigate via the Users list.')
    // Should not show the user header
    expect(wrapper.text()).not.toMatch(/User:\s+/)
  })

  it('renders the user header when props are valid and updates on change', async () => {
    const wrapper = mount(UserView, {
      props: { userName: 'Alice', userId: 1 }
    })

    expect(wrapper.text()).toContain('User: Alice (ID: 1)')
    expect(wrapper.text()).not.toContain('Missing user details')

    await wrapper.setProps({ userName: 'Bob', userId: 2 })
    expect(wrapper.text()).toContain('User: Bob (ID: 2)')
  })
})
