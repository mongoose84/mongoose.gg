import { computed, ref, watch } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { useAsyncData } from './useAsyncData'
import { trackFilterChange } from '../services/analyticsApi'
import { getSoloStatTrends, getSoloWinFactors, getDeathPositions } from '../services/soloApi'

// The death map (DangerZonesMap) still reads days until the death-zones card replaces it (5f)
const DEATH_MAP_TIME_RANGE = 'current_season'

/**
 * Data for the Solo page (features/solo-trends.spec.md): each card loads on its own, in
 * parallel, and reloads on a queue, range or account change. Old content stays until the new
 * data arrives, so a filter change never flashes a skeleton.
 *
 * The queue starts unset: the server picks the default (Solo/Duo when played this season,
 * else Flex, else all queues) and the control shows the queue it answered with.
 */
export function useSoloDashboardData() {
  const authStore = useAuthStore()

  const queue = ref(null)
  const range = ref('last20')
  const sideFilter = ref('all')

  const statTrends = useAsyncData(
    () => getSoloStatTrends(authStore.userId, queue.value, range.value),
    { errorMessage: 'Failed to load stat trends' }
  )
  const winFactors = useAsyncData(
    () => getSoloWinFactors(authStore.userId, queue.value, range.value),
    { errorMessage: 'Failed to load win factors' }
  )
  const deathPositions = useAsyncData(
    () => getDeathPositions(authStore.userId, selectedQueue.value ?? 'all', DEATH_MAP_TIME_RANGE, sideFilter.value),
    { errorMessage: 'Failed to load death positions' }
  )

  const hasNoLinkedAccount = computed(() => authStore.isInitialized && authStore.hasLinkedAccount === false)

  /** The queue the page shows: the player's choice, else the server's default */
  const selectedQueue = computed(() =>
    queue.value ?? statTrends.data.value?.queueType ?? winFactors.data.value?.queueType ?? null
  )

  // Skeletons only before a card has anything to show
  const statTrendsLoading = computed(() => statTrends.isLoading.value && !statTrends.data.value)
  const winFactorsLoading = computed(() => winFactors.isLoading.value && !winFactors.data.value)

  function run(resource) {
    // useAsyncData keeps the error for the card; nothing to do here
    return resource.execute().catch(() => {})
  }

  const fetchStatTrends = () => run(statTrends)
  const fetchWinFactors = () => run(winFactors)
  const fetchDeathPositions = () => run(deathPositions)

  function fetchAll() {
    if (!authStore.userId || hasNoLinkedAccount.value) return Promise.resolve()
    // The death map waits for stat trends, which settles the default queue
    return Promise.all([fetchStatTrends().then(fetchDeathPositions), fetchWinFactors()])
  }

  function setQueue(value) {
    if (value === selectedQueue.value) return
    queue.value = value
    trackFilterChange('queue', value)
  }

  function setRange(value) {
    if (value === range.value) return
    range.value = value
    trackFilterChange('range', value)
  }

  function setSide(value) {
    sideFilter.value = value
    fetchDeathPositions()
  }

  watch([queue, range], fetchAll)

  watch(() => authStore.activeAccountPuuid, () => {
    if (authStore.isInitialized) fetchAll()
  })

  // Wait for auth so the active account is settled before the first request
  watch(() => authStore.isInitialized, (initialized) => {
    if (initialized) fetchAll()
  }, { immediate: true })

  return {
    selectedQueue,
    range,
    hasNoLinkedAccount,
    statTrends: statTrends.data,
    statTrendsError: statTrends.hasError,
    statTrendsLoading,
    winFactors: winFactors.data,
    winFactorsError: winFactors.hasError,
    winFactorsLoading,
    deathPositions: deathPositions.data,
    deathPositionsError: deathPositions.error,
    deathPositionsLoading: deathPositions.isLoading,
    setQueue,
    setRange,
    setSide,
    fetchAll,
    fetchStatTrends,
    fetchWinFactors
  }
}
