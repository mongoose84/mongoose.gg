import { describe, it, expect, beforeEach, vi } from 'vitest'

describe('useDefaultView', () => {
  beforeEach(() => {
    localStorage.clear()
    // Re-import to reset module-level state
    vi.resetModules()
  })

  async function loadComposable() {
    const mod = await import('@/composables/useDefaultView')
    return mod.useDefaultView()
  }

  it('defaults to "overall" when localStorage is empty', async () => {
    const { defaultView } = await loadComposable()
    expect(defaultView.value).toBe('overall')
  })

  it('reads saved value from localStorage', async () => {
    localStorage.setItem('mongoose_default_view', 'acc-1')   
    const { defaultView } = await loadComposable()
    expect(defaultView.value).toBe('acc-1')
  })

  it('setDefaultView updates ref and localStorage', async () => {
    const { defaultView, setDefaultView } = await loadComposable()

    setDefaultView('acc-2')

    expect(defaultView.value).toBe('acc-2')
    expect(localStorage.getItem('mongoose_default_view')).toBe('acc-2')
  })

  it('setDefaultView can reset to overall', async () => {
    localStorage.setItem('mongoose_default_view', 'acc-1')
    const { defaultView, setDefaultView } = await loadComposable()

    setDefaultView('overall')

    expect(defaultView.value).toBe('overall')
    expect(localStorage.getItem('mongoose_default_view')).toBe('overall')
  })
})
