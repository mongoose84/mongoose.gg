import { describe, it, expect, vi } from 'vitest'
import { nextTick } from 'vue'
import { useAsyncData } from '@/composables/useAsyncData'

describe('useAsyncData', () => {
  it('stores data and state on successful execute', async () => {
    const fetcher = vi.fn().mockResolvedValue({ value: 42 })
    const { data, error, isLoading, isFetched, execute } = useAsyncData(fetcher)

    const result = await execute()

    expect(result).toEqual({ value: 42 })
    expect(data.value).toEqual({ value: 42 })
    expect(error.value).toBe(null)
    expect(isLoading.value).toBe(false)
    expect(isFetched.value).toBe(true)
  })

  it('sets error message and rethrows on failure', async () => {
    const fetcher = vi.fn().mockRejectedValue(new Error('Network down'))
    const { error, execute } = useAsyncData(fetcher, { errorMessage: 'Fallback error' })

    await expect(execute()).rejects.toThrow('Network down')
    expect(error.value).toBe('Network down')
  })

  it('resets all reactive state', async () => {
    const fetcher = vi.fn().mockResolvedValue(['ok'])
    const { data, error, isFetched, execute, reset } = useAsyncData(fetcher)

    await execute()
    reset()

    expect(data.value).toBe(null)
    expect(error.value).toBe(null)
    expect(isFetched.value).toBe(false)
  })

  it('keeps the newest response when an older one resolves last', async () => {
    let resolveOld
    const fetcher = vi.fn()
      .mockReturnValueOnce(new Promise((resolve) => { resolveOld = resolve }))
      .mockResolvedValueOnce('new')
    const { data, isLoading, execute } = useAsyncData(fetcher)

    const oldCall = execute()
    await execute()
    resolveOld('old')

    await expect(oldCall).resolves.toBe('old')
    expect(data.value).toBe('new')
    expect(isLoading.value).toBe(false)
  })

  it('ignores the error of a superseded call', async () => {
    let rejectOld
    const fetcher = vi.fn()
      .mockReturnValueOnce(new Promise((_, reject) => { rejectOld = reject }))
      .mockResolvedValueOnce('new')
    const { data, error, execute } = useAsyncData(fetcher)

    const oldCall = execute()
    await execute()
    rejectOld(new Error('stale failure'))

    await expect(oldCall).rejects.toThrow('stale failure')
    expect(error.value).toBe(null)
    expect(data.value).toBe('new')
  })

  it('can execute immediately when configured', async () => {
    const fetcher = vi.fn().mockResolvedValue('ready')
    const { data, isFetched } = useAsyncData(fetcher, { immediate: true })

    await nextTick()
    await Promise.resolve()

    expect(fetcher).toHaveBeenCalledTimes(1)
    expect(data.value).toBe('ready')
    expect(isFetched.value).toBe(true)
  })
})
