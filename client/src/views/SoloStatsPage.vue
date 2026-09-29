<template>
  <div class="solo-page" data-testid="solo-dashboard">
    <SyncProgress
      v-if="syncState"
      :state="syncState"
      :current="progressCurrent"
      :total="progressTotal"
      :synced-count="syncedCount"
      @retry="startSync"
    />

    <BaseEmptyState
      v-if="hasNoLinkedAccount"
      :heading-level="1"
      title="Link your Riot account to see your trends"
      description="We sync your recent matches and your trends show up here within a few minutes."
      data-testid="solo-no-account"
    >
      <template #action>
        <BaseButton size="lg" data-testid="solo-link-account" @click="showLinkModal = true">
          <template #icon-left><BaseIcon name="link" :size="20" /></template>
          Link Riot account
        </BaseButton>
      </template>
    </BaseEmptyState>

    <template v-else>
      <header class="solo-header" data-testid="solo-header">
        <div class="solo-header__text">
          <p v-if="rankLine" class="solo-header__rank" data-testid="solo-rank-line">
            <span v-if="riotId">{{ riotId }} · </span>
            <span
              class="solo-header__tier-dot"
              :style="{ background: `var(--color-rank-${rankLine.tierKey}, var(--color-text-secondary))` }"
              aria-hidden="true"
            />{{ rankLine.text }}
          </p>
          <h1 class="solo-header__title" data-testid="solo-headline">{{ headline }}</h1>
          <p v-if="secondLine" class="solo-header__subline" data-testid="solo-subline">{{ secondLine }}</p>
        </div>
        <div class="solo-header__controls">
          <BaseSegmentedControl
            :model-value="selectedQueue"
            :options="QUEUE_OPTIONS"
            aria-label="Queue"
            test-id-prefix="queue"
            @update:model-value="setQueue"
          />
          <BaseSegmentedControl
            :model-value="range"
            :options="RANGE_OPTIONS"
            aria-label="Range"
            test-id-prefix="range"
            @update:model-value="setRange"
          />
        </div>
      </header>

      <div v-reveal-on-view class="solo-top" :class="{ 'solo-top--with-focus': showFocus }">
        <SoloClimbCard
          :data="climb"
          :loading="climbLoading"
          :error="climbError"
          :queue="selectedQueue"
          :syncing="isSyncing"
          :single-account="singleAccount"
          @retry="fetchClimb"
          @show-all-queues="setQueue('all')"
          @sync="startSync"
        />
        <SoloFocusCard
          v-if="showFocus"
          :focus="statTrends?.focus ?? null"
          :verdict="focusVerdict"
          :loading="statTrendsLoading"
        />
      </div>

      <div v-reveal-on-view>
        <SoloStatTrends
          :data="statTrends"
          :loading="statTrendsLoading"
          :error="statTrendsError"
          :queue="selectedQueue"
          :syncing="isSyncing"
          @retry="fetchStatTrends"
          @show-all-queues="setQueue('all')"
          @sync="startSync"
        />
      </div>

      <div v-reveal-on-view>
        <SoloDeathZones
          :data="deathZones"
          :loading="deathZonesLoading"
          :error="deathZonesError"
          :queue="selectedQueue"
          :syncing="isSyncing"
          @retry="fetchDeathZones"
          @show-all-queues="setQueue('all')"
          @sync="startSync"
        />
      </div>

      <div v-reveal-on-view class="solo-pair">
        <SoloWinFactors
          :data="winFactors"
          :loading="winFactorsLoading"
          :error="winFactorsError"
          :queue="selectedQueue"
          :syncing="isSyncing"
          @retry="fetchWinFactors"
          @show-all-queues="setQueue('all')"
          @sync="startSync"
        />
        <SoloChampionLp
          :data="climb"
          :loading="climbLoading"
          :error="climbError"
          :queue="selectedQueue"
          :syncing="isSyncing"
          @retry="fetchClimb"
          @sync="startSync"
        />
      </div>

      <div v-reveal-on-view>
        <SoloPatterns
          :data="winFactors"
          :loading="winFactorsLoading"
          :error="winFactorsError"
          @retry="fetchWinFactors"
        />
      </div>
    </template>
  </div>

  <LinkRiotAccountModal
    :is-open="showLinkModal"
    @close="showLinkModal = false"
    @success="handleLinkSuccess"
  />
</template>

<script setup>
/**
 * Solo: "am I improving?" (features/solo-trends.spec.md). Headline and the queue and range
 * controls, then stat trends, the death map, win factors and patterns. Each card loads on its own.
 */
import { computed, ref, watch } from 'vue'
import { useAuthStore } from '../stores/authStore'
import { useSoloDashboardData } from '../composables/useSoloDashboardData'
import { useSyncMatches } from '../composables/useSyncMatches'
import { vRevealOnView } from '../composables/useRevealOnView'
import {
  QUEUE_OPTIONS,
  RANGE_OPTIONS,
  buildClimbHeadline,
  buildRankLine,
  buildSecondLine,
  buildSoloHeadline
} from '../utils/soloSummary'
import BaseButton from '../components/base/BaseButton.vue'
import BaseIcon from '../components/base/BaseIcon.vue'
import BaseEmptyState from '../components/base/BaseEmptyState.vue'
import BaseSegmentedControl from '../components/base/BaseSegmentedControl.vue'
import SyncProgress from '../components/base/SyncProgress.vue'
import SoloClimbCard from '../components/solo/SoloClimbCard.vue'
import SoloChampionLp from '../components/solo/SoloChampionLp.vue'
import SoloFocusCard from '../components/solo/SoloFocusCard.vue'
import SoloStatTrends from '../components/solo/SoloStatTrends.vue'
import SoloWinFactors from '../components/solo/SoloWinFactors.vue'
import SoloPatterns from '../components/solo/SoloPatterns.vue'
import SoloDeathZones from '../components/solo/SoloDeathZones.vue'
import LinkRiotAccountModal from '../components/LinkRiotAccountModal.vue'

const authStore = useAuthStore()
const showLinkModal = ref(false)

const {
  selectedQueue,
  range,
  hasNoLinkedAccount,
  climb,
  climbError,
  climbLoading,
  statTrends,
  statTrendsError,
  statTrendsLoading,
  winFactors,
  winFactorsError,
  winFactorsLoading,
  deathZones,
  deathZonesError,
  deathZonesLoading,
  setQueue,
  setRange,
  fetchAll,
  fetchClimb,
  fetchDeathZones,
  fetchStatTrends,
  fetchWinFactors
} = useSoloDashboardData()

const {
  syncState,
  isSyncing,
  progressCurrent,
  progressTotal,
  syncedCount,
  startSync
} = useSyncMatches()

// FR 5–6 from the climb; until it answers, the match count from stat trends
const headline = computed(() =>
  buildClimbHeadline(climb.value)
    ?? buildSoloHeadline(statTrends.value?.range, statTrends.value?.matches)
    ?? 'Your trends'
)
const rankLine = computed(() => buildRankLine(climb.value))

// FR 18: the focus card is left out when the server picks no focus (stat-trends errors show on its own card)
const showFocus = computed(() => statTrendsLoading.value || Boolean(statTrends.value?.focus))
const focusVerdict = computed(() => {
  const focus = statTrends.value?.focus
  return focus ? statTrends.value.stats.find((s) => s.key === focus.stat)?.verdict ?? null : null
})

// The rank belongs to one account: the active one, or the only one linked
const rankAccount = computed(() => authStore.activeAccount ?? authStore.primaryRiotAccount)
const riotId = computed(() =>
  rankAccount.value?.gameName ? `${rankAccount.value.gameName}#${rankAccount.value.tagLine}` : null
)
const singleAccount = computed(() => !authStore.isOverallMode || (authStore.riotAccounts?.length ?? 0) <= 1)
const secondLine = computed(() => buildSecondLine(statTrends.value?.stats))

async function handleLinkSuccess() {
  await authStore.refreshUser()
  fetchAll()
}

// A finished sync that brought in matches refreshes every card
watch(syncState, (state, previous) => {
  if (state === 'done' && previous === 'running') fetchAll()
})
</script>

<style scoped>
.solo-page {
  display: flex;
  flex-direction: column;
  gap: 1.75rem;
  padding: 1.75rem 0 3.5rem;
}

.solo-header {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  justify-content: space-between;
  gap: 1rem 1.5rem;
}

.solo-header__text {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  min-width: 0;
}

.solo-header__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 2.25rem;
  font-weight: 600;
  line-height: 1.15;
  color: var(--color-text);
}

.solo-header__rank {
  display: inline-flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.375rem;
  margin: 0;
  font-size: 0.875rem;
  color: var(--color-ink-soft);
}

.solo-header__tier-dot {
  width: 0.5rem;
  height: 0.5rem;
  border-radius: 999px;
}

.solo-top {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 1.25rem;
  align-items: stretch;
}

.solo-top--with-focus {
  grid-template-columns: minmax(0, 2fr) minmax(0, 1fr);
}

.solo-pair {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  gap: 1.25rem;
  align-items: start;
}

.solo-header__subline {
  margin: 0;
  max-width: 65ch;
  font-size: 1rem;
  line-height: 1.55;
  color: var(--color-ink-soft);
}

.solo-header__controls {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.5rem;
}

@media (max-width: 899px) {
  .solo-top--with-focus,
  .solo-pair {
    grid-template-columns: minmax(0, 1fr);
  }

  .solo-header__title {
    font-size: 1.75rem;
  }

  .solo-header__controls {
    align-items: flex-start;
  }
}
</style>
