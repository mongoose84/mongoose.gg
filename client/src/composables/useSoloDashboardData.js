import { computed, onScopeDispose, ref, watch } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { useAsyncData } from './useAsyncData'
import { useSyncWebSocket } from './useSyncWebSocket'
import { trackFilterChange } from '../services/analyticsApi'
import { getSoloClimb, getSoloStatTrends, getSoloWinFactors, getSoloDeathZones } from '../services/soloApi'

// FR 39: without a socket, the death-zones card refetches this often while the backfill runs
const BACKFILL_POLL_MS = 30_000

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
  const { detailBackfill, isConnected } = useSyncWebSocket()

  const climb = useAsyncData(
    () => getSoloClimb(authStore.userId, queue.value, range.value),
    { errorMessage: 'Failed to load climb' }
  )
  const statTrends = useAsyncData(
    () => getSoloStatTrends(authStore.userId, queue.value, range.value),
    { errorMessage: 'Failed to load stat trends' }
  )
  const winFactors = useAsyncData(
    () => getSoloWinFactors(authStore.userId, queue.value, range.value),
    { errorMessage: 'Failed to load win factors' }
  )
  const deathZones = useAsyncData(
    () => getSoloDeathZones(authStore.userId, queue.value, range.value),
    { errorMessage: 'Failed to load death zones' }
  )

  const hasNoLinkedAccount = computed(() => authStore.isInitialized && authStore.hasLinkedAccount === false)

  /** The queue the page shows: the player's choice, else the server's default */
  const selectedQueue = computed(() =>
    queue.value ?? statTrends.data.value?.queueType ?? winFactors.data.value?.queueType ?? climb.data.value?.queueType ?? null
  )

  // Skeletons only before a card has anything to show
  const statTrendsLoading = computed(() => statTrends.isLoading.value && !statTrends.data.value)
  const winFactorsLoading = computed(() => winFactors.isLoading.value && !winFactors.data.value)
  const climbLoading = computed(() => climb.isLoading.value && !climb.data.value)
  const deathZonesLoading = computed(() => deathZones.isLoading.value && !deathZones.data.value)

  function run(resource) {
    // useAsyncData keeps the error for the card; nothing to do here
    return resource.execute().catch(() => {})
  }

  const fetchStatTrends = () => run(statTrends)
  const fetchWinFactors = () => run(winFactors)
  const fetchClimb = () => run(climb)
  const fetchDeathZones = () => run(deathZones)

  function fetchAll() {
    if (!authStore.userId || hasNoLinkedAccount.value) return Promise.resolve()
    return Promise.all([fetchClimb(), fetchStatTrends(), fetchWinFactors(), fetchDeathZones()])
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

  watch([queue, range], fetchAll)

  // FR 39: backfill progress over the socket; a finished account refetches the zones once
  watch(detailBackfill, (message) => {
    const current = deathZones.data.value
    if (!message || !current?.backfill) return
    if (message.status === 'done') {
      fetchDeathZones()
      return
    }
    deathZones.data.value = { ...current, backfill: { ...message } }
  })

  // Without a socket, refetch every 30 seconds while the backfill runs
  let pollTimer = null
  function stopPolling() {
    if (pollTimer !== null) clearInterval(pollTimer)
    pollTimer = null
  }
  watch(
    () => Boolean(deathZones.data.value?.backfill) && !isConnected.value,
    (poll) => {
      stopPolling()
      if (poll) pollTimer = setInterval(fetchDeathZones, BACKFILL_POLL_MS)
    }
  )
  onScopeDispose(stopPolling)

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
    climb: climb.data,
    climbError: climb.hasError,
    climbLoading,
    statTrends: statTrends.data,
    statTrendsError: statTrends.hasError,
    statTrendsLoading,
    winFactors: winFactors.data,
    winFactorsError: winFactors.hasError,
    winFactorsLoading,
    deathZones: deathZones.data,
    deathZonesError: deathZones.hasError,
    deathZonesLoading,
    setQueue,
    setRange,
    fetchAll,
    fetchClimb,
    fetchDeathZones,
    fetchStatTrends,
    fetchWinFactors
  }
}
