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
    // Header should show the missing details message
    const heading = wrapper.find('.user-container h2')
    expect(heading.exists()).toBe(true)
    expect(heading.text()).toBe('Missing user details')
  })

  it('renders the user name header when props are valid and updates on change', async () => {
    const wrapper = mount(UserView, {
      props: { userName: 'Alice', userId: 1 }
    })

    // Header should be just the name (no ID)
    expect(wrapper.find('.user-container h2').text()).toBe('Alice')
    expect(wrapper.text()).not.toContain('(ID:')
    expect(wrapper.text()).not.toMatch(/^User:\s/)

    // Update props and verify
    await wrapper.setProps({ userName: 'Bob', userId: 2 })
    expect(wrapper.find('.user-container h2').text()).toBe('Bob')
  })
})
