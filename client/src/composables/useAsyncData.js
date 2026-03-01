import { computed, ref } from 'vue'

/**
 * Generic composable for async data loading with standardized state.
 *
 * @template T
 * @param {(...args: any[]) => Promise<T>} fetcher - Async fetch function
 * @param {Object} [options]
 * @param {boolean} [options.immediate=false] - Execute immediately on creation
 * @param {(result: T) => any} [options.transform] - Optional data transform
 * @param {string} [options.errorMessage='Failed to load data'] - Fallback error message
 */
export function useAsyncData(fetcher, options = {}) {
  const {
    immediate = false,
    transform = (result) => result,
    errorMessage = 'Failed to load data'
  } = options

  const data = ref(null)
  const error = ref(null)
  const isLoading = ref(false)
  const isFetched = ref(false)

  const hasData = computed(() => data.value !== null)
  const hasError = computed(() => !!error.value)

  async function execute(...args) {
    isLoading.value = true
    error.value = null

    try {
      const result = await fetcher(...args)
      data.value = transform(result)
      isFetched.value = true
      return data.value
    } catch (err) {
      console.error('useAsyncData request failed:', err)
      error.value = err?.message || errorMessage
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function refresh(...args) {
    return execute(...args)
  }

  function reset() {
    data.value = null
    error.value = null
    isLoading.value = false
    isFetched.value = false
  }

  if (immediate) {
    execute().catch(() => {})
  }

  return {
    data,
    error,
    isLoading,
    isFetched,
    hasData,
    hasError,
    execute,
    refresh,
    reset
  }
}
