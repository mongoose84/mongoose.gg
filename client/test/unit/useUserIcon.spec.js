import { describe, it, expect, beforeEach, vi } from 'vitest'

describe('useUserIcon', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.resetModules()
  })

  async function loadComposable() {
    const mod = await import('@/composables/useUserIcon')
    return mod.useUserIcon()
  }

  it('defaults to null when localStorage is empty', async () => {
    const { selectedIconId, userIconUrl } = await loadComposable()
    expect(selectedIconId.value).toBeNull()
    expect(userIconUrl.value).toBeNull()
  })

  it('reads saved icon ID from localStorage', async () => {
    localStorage.setItem('mongoose_user_icon', '29')
    const { selectedIconId } = await loadComposable()
    expect(selectedIconId.value).toBe(29)
  })

  it('returns a profile icon URL when icon is set', async () => {
    localStorage.setItem('mongoose_user_icon', '29')
    const { userIconUrl } = await loadComposable()
    expect(userIconUrl.value).toContain('/profileicon/29.png')
  })

  it('setUserIcon updates ref and localStorage', async () => {
    const { selectedIconId, setUserIcon } = await loadComposable()

    setUserIcon(503)

    expect(selectedIconId.value).toBe(503)
    expect(localStorage.getItem('mongoose_user_icon')).toBe('503')
  })

  it('setUserIcon(null) clears the selection', async () => {
    localStorage.setItem('mongoose_user_icon', '29')
    const { selectedIconId, userIconUrl, setUserIcon } = await loadComposable()

    setUserIcon(null)

    expect(selectedIconId.value).toBeNull()
    expect(userIconUrl.value).toBeNull()
    expect(localStorage.getItem('mongoose_user_icon')).toBeNull()
  })

  it('ignores non-numeric localStorage values', async () => {
    localStorage.setItem('mongoose_user_icon', 'invalid')
    const { selectedIconId } = await loadComposable()
    expect(selectedIconId.value).toBeNull()
  })
})
