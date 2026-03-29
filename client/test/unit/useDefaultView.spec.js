import { describe, it, expect, beforeEach, vi } from 'vitest'

// Reset module between tests to get fresh module-level ref
beforeEach(() => {
  localStorage.clear()
  vi.resetModules()
})

describe('useDefaultView', () => {
  it('returns "overall" when no localStorage value exists', async () => {
    const { useDefaultView } = await import('@/composables/useDefaultView')
    const { defaultView } = useDefaultView()

    expect(defaultView.value).toBe('overall')
  })

  it('reads initial value from localStorage', async () => {
    localStorage.setItem('mongoose_default_view', 'some-puuid')
    const { useDefaultView } = await import('@/composables/useDefaultView')
    const { defaultView } = useDefaultView()

    expect(defaultView.value).toBe('some-puuid')
  })

  it('setDefaultView updates the ref and localStorage', async () => {
    const { useDefaultView } = await import('@/composables/useDefaultView')
    const { defaultView, setDefaultView } = useDefaultView()

    setDefaultView('new-puuid')

    expect(defaultView.value).toBe('new-puuid')
    expect(localStorage.getItem('mongoose_default_view')).toBe('new-puuid')
  })

  it('shares state across multiple calls', async () => {
    const { useDefaultView } = await import('@/composables/useDefaultView')
    const first = useDefaultView()
    const second = useDefaultView()

    first.setDefaultView('shared-value')

    expect(second.defaultView.value).toBe('shared-value')
  })
})