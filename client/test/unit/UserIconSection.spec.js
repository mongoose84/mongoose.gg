import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { ref } from 'vue'
import UserIconSection from '@/components/settings/UserIconSection.vue'

const mockSelectedIconId = ref(null)
const mockUserIconUrl = ref(null)
const mockSetUserIcon = vi.fn()

vi.mock('@/composables/useUserIcon', () => ({
  useUserIcon: () => ({
    selectedIconId: mockSelectedIconId,
    userIconUrl: mockUserIconUrl,
    setUserIcon: mockSetUserIcon
  }),
  ICON_OPTIONS: [29, 1, 2, 3]
}))

describe('UserIconSection.vue', () => {
  const createWrapper = () => mount(UserIconSection)

  beforeEach(() => {
    mockSelectedIconId.value = null
    mockUserIconUrl.value = null
    mockSetUserIcon.mockReset()
  })

  it('renders the section', () => {
    const wrapper = createWrapper()
    expect(wrapper.find('[data-testid="user-icon-section"]').exists()).toBe(true)
  })

  it('shows fallback icon when no icon is selected', () => {
    const wrapper = createWrapper()
    const preview = wrapper.find('[data-testid="user-icon-preview"]')
    expect(preview.find('svg').exists()).toBe(true)
    expect(preview.find('img').exists()).toBe(false)
  })

  it('shows selected icon image in preview', () => {
    mockSelectedIconId.value = 29
    mockUserIconUrl.value = 'https://ddragon.leagueoflegends.com/cdn/16.1.1/img/profileicon/29.png'

    const wrapper = createWrapper()
    const preview = wrapper.find('[data-testid="user-icon-preview"]')
    expect(preview.find('img').exists()).toBe(true)
    expect(preview.find('img').attributes('src')).toContain('/profileicon/29.png')
  })

  it('renders icon options in the grid', () => {
    const wrapper = createWrapper()
    const grid = wrapper.find('[data-testid="user-icon-grid"]')
    expect(grid.findAll('button').length).toBe(4)
  })

  it('clicking an icon calls setUserIcon with the icon ID', async () => {
    const wrapper = createWrapper()

    await wrapper.find('[data-testid="user-icon-option-29"]').trigger('click')

    expect(mockSetUserIcon).toHaveBeenCalledWith(29)
  })

  it('shows remove button when an icon is selected', () => {
    mockSelectedIconId.value = 29
    mockUserIconUrl.value = 'https://example.com/icon.png'

    const wrapper = createWrapper()
    expect(wrapper.find('[data-testid="user-icon-clear"]').exists()).toBe(true)
  })

  it('hides remove button when no icon is selected', () => {
    const wrapper = createWrapper()
    expect(wrapper.find('[data-testid="user-icon-clear"]').exists()).toBe(false)
  })

  it('clicking remove calls setUserIcon(null)', async () => {
    mockSelectedIconId.value = 29
    mockUserIconUrl.value = 'https://example.com/icon.png'

    const wrapper = createWrapper()
    await wrapper.find('[data-testid="user-icon-clear"]').trigger('click')

    expect(mockSetUserIcon).toHaveBeenCalledWith(null)
  })

  it('highlights the currently selected icon in the grid', () => {
    mockSelectedIconId.value = 29

    const wrapper = createWrapper()
    const selectedButton = wrapper.find('[data-testid="user-icon-option-29"]')
    expect(selectedButton.classes()).toContain('border-primary')
  })

  it('hides icon button when image fails to load', async () => {
    const wrapper = createWrapper()
    const button = wrapper.find('[data-testid="user-icon-option-29"]')
    expect(button.element.style.display).not.toBe('none')

    await button.find('img').trigger('error')
    await wrapper.vm.$nextTick()

    expect(button.element.style.display).toBe('none')
  })

  it('clears selected icon when preview image fails to load', async () => {
    mockSelectedIconId.value = 29
    mockUserIconUrl.value = 'https://ddragon.leagueoflegends.com/cdn/16.1.1/img/profileicon/29.png'

    const wrapper = createWrapper()
    const preview = wrapper.find('[data-testid="user-icon-preview"]')
    expect(preview.find('img').exists()).toBe(true)

    await preview.find('img').trigger('error')

    expect(mockSetUserIcon).toHaveBeenCalledWith(null)
  })
})
