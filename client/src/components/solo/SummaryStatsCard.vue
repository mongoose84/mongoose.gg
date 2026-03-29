<template>
  <BaseCard class="summary-stats-card" data-testid="summary-stats-card">
    <div class="card-content">
      <!-- Loading state -->
      <div v-if="loading" class="stats-grid" data-testid="loading-state">
        <div v-for="i in 3" :key="i" class="stat-item">
          <div class="skeleton skeleton-label" />
          <div class="skeleton skeleton-value" />
        </div>
      </div>

      <!-- Empty state -->
      <div v-else-if="isEmpty" class="empty-state" data-testid="empty-state">
        <p class="empty-text">No games found for this filter</p>
      </div>

      <!-- Stats display -->
      <div v-else class="stats-grid" data-testid="stats-display">
        <!-- Overall Mode: Stacked Per-Account Ranks -->
        <div v-if="ranks && ranks.length" class="stat-item stat-ranks" data-testid="stat-ranks">
          <span class="stat-label">Ranks</span>
          <div class="ranks-stacked">
            <span
              v-for="(rankEntry, idx) in formattedRanks"
              :key="rankEntry.gameName"
              class="rank-pill"
              :class="{ 'rank-pill--highlight': idx === 0 }"
              :title="rankEntry.fullText"
              :data-testid="'rank-pill-' + idx"
            >{{ rankEntry.shortText }}</span>
          </div>
        </div>

        <!-- Solo/Duo Rank (All Queues or Solo filter, single-account mode) -->
        <div v-if="showSoloDuoRank" class="stat-item" data-testid="solo-duo-rank-wrapper">
          <span class="stat-label">Solo/Duo</span>
          <div class="stat-value-rank">
            <BaseRankBadge
              :tier="soloDuoRank?.tier"
              :division="soloDuoRank?.division"
              :lp="soloDuoRank?.lp"
              :has-rank="soloDuoRank?.hasRank ?? false"
              size="sm"
            />
          </div>
        </div>

        <!-- Flex Rank (All Queues or Flex filter, single-account mode) -->
        <div v-if="showFlexRank" class="stat-item" data-testid="flex-rank-wrapper">
          <span class="stat-label">Flex</span>
          <div class="stat-value-rank">
            <BaseRankBadge
              :tier="flexRank?.tier"
              :division="flexRank?.division"
              :lp="flexRank?.lp"
              :has-rank="flexRank?.hasRank ?? false"
              size="sm"
            />
          </div>
        </div>

        <!-- Games Played -->
        <div class="stat-item" data-testid="stat-games">
          <span class="stat-label">Games</span>
          <span class="stat-value" data-testid="stat-games-value">{{ gamesPlayed }}</span>
          <span v-if="accountCount > 1" class="stat-sublabel" data-testid="stat-games-sublabel">Across {{ accountCount }} accounts</span>
        </div>

        <!-- Winrate -->
        <div class="stat-item" data-testid="stat-winrate">
          <span class="stat-label">Winrate</span>
          <span
            class="stat-value"
            :class="[winrateColorClass, winrateTrendClass]"
            :title="winrateTooltip"
            data-testid="stat-winrate-value"
          >
            {{ formattedWinrate }}
          </span>
        </div>

        <!-- K / D / A Breakdown -->
        <div
          class="stat-item stat-kda-group"
          data-testid="stat-kda"
        >
          <span class="stat-label">Average K / D / A</span>
          <div class="kda-values">
            <span
              class="kda-value"
              :class="killsTrendClass"
              :title="killsTooltip"
              data-testid="stat-kills-value"
            >{{ formattedKills }}</span>
            <span class="kda-separator">/</span>
            <span
              class="kda-value"
              :class="deathsTrendClass"
              :title="deathsTooltip"
              data-testid="stat-deaths-value"
            >{{ formattedDeaths }}</span>
            <span class="kda-separator">/</span>
            <span
              class="kda-value"
              :class="assistsTrendClass"
              :title="assistsTooltip"
              data-testid="stat-assists-value"
            >{{ formattedAssists }}</span>
          </div>
          <span class="kda-ratio" :title="kdaTooltip" data-testid="stat-kda-ratio">{{ formattedKdaRatio }} KDA</span>
        </div>
      </div>
    </div>
  </BaseCard>
</template>

<script setup>
import { computed } from 'vue'
import BaseCard from '../base/BaseCard.vue'
import BaseRankBadge from '../base/BaseRankBadge.vue'
import { getWinRateColorClass } from '../../composables/useWinRateColor'

const props = defineProps({
  /** Number of games played */
  gamesPlayed: {
    type: Number,
    default: 0
  },
  /** Win rate percentage (0-100) for selected time range */
  winRate: {
    type: Number,
    default: null
  },
  /** Overall (all-time) win rate percentage (for tooltip comparison) */
  overallWinRate: {
    type: Number,
    default: null
  },
  /** Average KDA ratio (deprecated, kept for backwards compatibility) */
  avgKda: {
    type: Number,
    default: null
  },
  /** Average kills per game */
  avgKills: {
    type: Number,
    default: null
  },
  /** Average deaths per game */
  avgDeaths: {
    type: Number,
    default: null
  },
  /** Average assists per game */
  avgAssists: {
    type: Number,
    default: null
  },
  /** Overall (all-time) average kills (for tooltip comparison) */
  overallAvgKills: {
    type: Number,
    default: null
  },
  /** Overall (all-time) average deaths (for tooltip comparison) */
  overallAvgDeaths: {
    type: Number,
    default: null
  },
  /** Overall (all-time) average assists (for tooltip comparison) */
  overallAvgAssists: {
    type: Number,
    default: null
  },
  /** Overall (all-time) average KDA (for tooltip comparison) */
  overallAvgKda: {
    type: Number,
    default: null
  },
  /** Loading state */
  loading: {
    type: Boolean,
    default: false
  },
  /** Number of accounts contributing to the stats (> 1 shows 'Across N accounts' label) */
  accountCount: {
    type: Number,
    default: 1
  },
  /** Per-account ranks for Overall mode: [{ gameName, soloDuoRank, flexRank }] */
  ranks: {
    type: Array,
    default: null
  },
  /** Solo/Duo rank info object { tier, division, lp, hasRank } */
  soloDuoRank: {
    type: Object,
    default: null
  },
  /** Flex rank info object { tier, division, lp, hasRank } */
  flexRank: {
    type: Object,
    default: null
  },
  /** Current queue filter (all, ranked_solo, ranked_flex, normal, aram) */
  queueFilter: {
    type: String,
    default: 'all'
  }
})

// Computed: Show Solo/Duo rank (All Queues or Solo filter)
const showSoloDuoRank = computed(() => {
  if (props.ranks && props.ranks.length) return false
  return props.queueFilter === 'all' || props.queueFilter === 'ranked_solo'
})

// Computed: Show Flex rank (All Queues or Flex filter)
const showFlexRank = computed(() => {
  if (props.ranks && props.ranks.length) return false
  return props.queueFilter === 'all' || props.queueFilter === 'ranked_flex'
})

// Computed: Format per-account ranks for stacked display
const formattedRanks = computed(() => {
  if (!props.ranks || !props.ranks.length) return []
  return props.ranks.map(account => {
    const solo = account.soloDuoRank
    const flex = account.flexRank
    const soloText = solo?.hasRank
      ? `${solo.tier.charAt(0).toUpperCase() + solo.tier.slice(1).toLowerCase()} ${solo.division}`
      : 'Unranked'
    const flexText = flex?.hasRank
      ? `${flex.tier.charAt(0).toUpperCase() + flex.tier.slice(1).toLowerCase()} ${flex.division}`
      : null
    const shortText = soloText
    const fullText = flexText ? `${soloText} (Flex: ${flexText}) — ${account.gameName}` : `${soloText} — ${account.gameName}`
    return { gameName: account.gameName, shortText: `${shortText} (${account.gameName})`, fullText }
  })
})

// Computed: Check if data is empty
const isEmpty = computed(() => {
  return !props.loading && props.gamesPlayed === 0
})

// Computed: Formatted winrate
const formattedWinrate = computed(() => {
  if (props.winRate === null || props.winRate === undefined) return '--'
  return `${props.winRate.toFixed(1)}%`
})

// Computed: Winrate color class (based on absolute value)
const winrateColorClass = computed(() => {
  return getWinRateColorClass(props.winRate)
})

// Computed: Check if we have winrate trend data
const hasWinrateTrendData = computed(() => {
  return props.winRate !== null && props.overallWinRate !== null
})

// Computed: Winrate tooltip showing comparison to overall
const winrateTooltip = computed(() => {
  if (!hasWinrateTrendData.value) return ''
  return `Selected period: ${props.winRate.toFixed(1)}%, overall: ${props.overallWinRate.toFixed(1)}%`
})

// Computed: Winrate trend class (green if selected period > overall, red if less)
const winrateTrendClass = computed(() => {
  if (!hasWinrateTrendData.value) return ''
  if (props.winRate > props.overallWinRate) return 'trend-positive'
  if (props.winRate < props.overallWinRate) return 'trend-negative'
  return ''
})

// Computed: Formatted K/D/A values
const formattedKills = computed(() => {
  if (props.avgKills === null || props.avgKills === undefined) return '--'
  return props.avgKills.toFixed(1)
})

const formattedDeaths = computed(() => {
  if (props.avgDeaths === null || props.avgDeaths === undefined) return '--'
  return props.avgDeaths.toFixed(1)
})

const formattedAssists = computed(() => {
  if (props.avgAssists === null || props.avgAssists === undefined) return '--'
  return props.avgAssists.toFixed(1)
})

// Computed: KDA ratio (kills + assists) / deaths
const formattedKdaRatio = computed(() => {
  // If avgKda is provided directly, use it
  if (props.avgKda !== null && props.avgKda !== undefined) {
    return props.avgKda.toFixed(2)
  }
  // Otherwise calculate from K/D/A
  if (props.avgKills === null || props.avgDeaths === null || props.avgAssists === null) return '--'
  const kda = props.avgDeaths > 0
    ? (props.avgKills + props.avgAssists) / props.avgDeaths
    : props.avgKills + props.avgAssists
  return kda.toFixed(2)
})

// Trend comparison helpers - check if we have both current period and overall data
const hasTrendData = computed(() => {
  return props.overallAvgKills !== null &&
         props.overallAvgDeaths !== null &&
         props.overallAvgAssists !== null &&
         props.avgKills !== null &&
         props.avgDeaths !== null &&
         props.avgAssists !== null
})

// Computed: Individual tooltips for each K/D/A stat
// Shows comparison of selected time range vs overall (all-time) average
const killsTooltip = computed(() => {
  if (!hasTrendData.value) return ''
  return `Selected period: ${props.avgKills.toFixed(1)}, overall: ${props.overallAvgKills.toFixed(1)}`
})

const deathsTooltip = computed(() => {
  if (!hasTrendData.value) return ''
  return `Selected period: ${props.avgDeaths.toFixed(1)}, overall: ${props.overallAvgDeaths.toFixed(1)}`
})

const assistsTooltip = computed(() => {
  if (!hasTrendData.value) return ''
  return `Selected period: ${props.avgAssists.toFixed(1)}, overall: ${props.overallAvgAssists.toFixed(1)}`
})

const kdaTooltip = computed(() => {
  if (!hasTrendData.value) return ''
  const overallKda = props.overallAvgKda !== null
    ? props.overallAvgKda.toFixed(2)
    : (props.overallAvgDeaths > 0
        ? ((props.overallAvgKills + props.overallAvgAssists) / props.overallAvgDeaths).toFixed(2)
        : (props.overallAvgKills + props.overallAvgAssists).toFixed(2))
  return `Selected period: ${formattedKdaRatio.value}, overall: ${overallKda}`
})

// Trend class for Kills: green if selected period > overall (improving)
const killsTrendClass = computed(() => {
  if (!hasTrendData.value) return ''
  if (props.avgKills > props.overallAvgKills) return 'trend-positive'
  if (props.avgKills < props.overallAvgKills) return 'trend-negative'
  return ''
})

// Trend class for Deaths: green if selected period < overall (fewer deaths = improving), red if more
const deathsTrendClass = computed(() => {
  if (!hasTrendData.value) return ''
  if (props.avgDeaths < props.overallAvgDeaths) return 'trend-positive'
  if (props.avgDeaths > props.overallAvgDeaths) return 'trend-negative'
  return ''
})

// Trend class for Assists: green if selected period > overall (improving)
const assistsTrendClass = computed(() => {
  if (!hasTrendData.value) return ''
  if (props.avgAssists > props.overallAvgAssists) return 'trend-positive'
  if (props.avgAssists < props.overallAvgAssists) return 'trend-negative'
  return ''
})
</script>

<style scoped>
.summary-stats-card {
  width: 100%;
  min-height: 80px;
}

.card-content {
  padding: 0px var(--spacing-lg); 
  margin-top: -18px; /* Adjust for card padding to align with other elements */
}

.stats-grid {
  display: flex;
  justify-content: space-around;
  align-items: center;
  gap: var(--spacing-lg);
}

.stat-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-xs);
}

.stat-label {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  font-weight: 500;
  
}

.stat-value {
  font-size: var(--font-size-lg);
  font-weight: 700;
  color: var(--color-text);
  margin-top: -10px;
}

.stat-value-rank {
  margin-top: -5px;
}

/* K/D/A Group */
.stat-kda-group {
  gap: var(--spacing-xxs);
}

.kda-values {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
}

.kda-value {
  font-size: var(--font-size-lg);
  font-weight: 700;
  color: var(--color-text);
  transition: color 0.2s ease;
}

.kda-separator {
  font-size: var(--font-size-lg);
  font-weight: 400;
  color: var(--color-text-secondary);
}

.kda-ratio {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  font-weight: 500;
}

/* Trend coloring - subtle tints for improvement tracking */
.trend-positive {
  color: var(--color-success);
}

.trend-negative {
  color: var(--color-error);
}

/* Empty state */
.empty-state {
  text-align: center;
  padding: var(--spacing-md) 0;
}

.empty-text {
  margin: 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

/* Skeleton loading */
.skeleton {
  background: linear-gradient(90deg, var(--color-surface) 25%, var(--color-elevated) 50%, var(--color-surface) 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
  border-radius: var(--radius-sm);
}

.skeleton-label {
  width: 48px;
  height: 12px;
}

.skeleton-value {
  width: 64px;
  height: 28px;
  margin-top: var(--spacing-xs);
}

@keyframes shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

/* Across N accounts sub-label */
.stat-sublabel {
  font-size: var(--font-size-3xs, 0.625rem);
  color: var(--color-text-secondary);
  margin-top: calc(var(--spacing-xs, 4px) * -1);
}

/* Per-account stacked ranks */
.stat-ranks {
  align-items: flex-start;
}

.ranks-stacked {
  display: flex;
  flex-wrap: wrap;
  gap: var(--spacing-xs, 4px);
  margin-top: -4px;
}

.rank-pill {
  font-size: var(--font-size-xs);
  padding: 2px 8px;
  border-radius: var(--radius-sm);
  background-color: var(--color-background-elevated, rgba(255, 255, 255, 0.05));
  color: var(--color-text-secondary);
  border: 1px solid transparent;
  cursor: default;
}

.rank-pill--highlight {
  border-color: var(--color-primary-soft, rgba(109, 40, 217, 0.4));
  color: var(--color-text);
}

/* Responsive: stack on mobile */
@media (max-width: 480px) {
  .stats-container {
    flex-direction: column;
    gap: var(--spacing-md);
  }
}
</style>

