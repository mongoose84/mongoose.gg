<template>
  <div
    :class="['profile-header-card', { clickable }]"
    :role="clickable ? 'button' : undefined"
    :tabindex="clickable ? 0 : undefined"
    @click="handleClick"
    @keydown.enter="handleClick"
    @keydown.space.prevent="handleClick"
  >
    <!-- Profile Icon -->
    <div class="profile-icon-container">
      <img
        v-if="profileIconUrl"
        :src="profileIconUrl"
        :alt="`${riotId} profile icon`"
        class="profile-icon"
        @error="handleIconError"
      />
      <div v-else class="profile-icon-placeholder">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
          <path fill-rule="evenodd" d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z" clip-rule="evenodd" />
        </svg>
      </div>
      <span v-if="summonerLevel" class="level-badge">{{ summonerLevel }}</span>
    </div>

    <!-- Identity & Stats -->
    <div class="profile-info">
      <div class="identity">
        <span class="region-badge">{{ regionLabel }}</span>
        <h2 class="riot-id">{{ riotId }}</h2>
      </div>

      <!-- Rank Badges -->
      <div class="rank-badges">
        <div :class="['rank-badge', { placeholder: !soloTier }]" title="Ranked Solo/Duo">
          <span class="rank-icon">🏆</span>
          <span class="rank-text">{{ soloRankDisplay }}</span>
        </div>
        <div :class="['rank-badge', { placeholder: !flexTier }]" title="Ranked Flex">
          <span class="rank-icon">👥</span>
          <span class="rank-text">{{ flexRankDisplay }}</span>
        </div>
      </div>

      <!-- Stats Row -->
      <div class="stats-row">
        <div class="stat">
	          <span :class="['stat-value', winRateColorClass]">{{ winRateDisplay }}</span>
          <span class="stat-label">Win Rate</span>
        </div>
        <div class="stat-divider"></div>
        <div class="stat">
          <span class="stat-value">{{ gamesPlayedDisplay }}</span>
          <span class="stat-label">Games</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'
import { getWinRateColorClass } from '../composables/useWinRateColor'

const props = defineProps({
  gameName: {
    type: String,
    required: true
  },
  tagLine: {
    type: String,
    required: true
  },
  region: {
    type: String,
    required: true
  },
  profileIconId: {
    type: Number,
    default: null
  },
  summonerLevel: {
    type: Number,
    default: null
  },
  soloTier: {
    type: String,
    default: null
  },
  soloRank: {
    type: String,
    default: null
  },
  soloLp: {
    type: Number,
    default: null
  },
  flexTier: {
    type: String,
    default: null
  },
  flexRank: {
    type: String,
    default: null
  },
  flexLp: {
    type: Number,
    default: null
  },
  winRate: {
    type: Number,
    default: null
  },
  gamesPlayed: {
    type: Number,
    default: null
  },
  clickable: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['click'])

const iconError = ref(false)

function handleClick() {
  if (props.clickable) {
    emit('click')
  }
}

// Data Dragon version - could be fetched dynamically in the future
const ddVersion = '16.1.1'

const riotId = computed(() => `${props.gameName}#${props.tagLine}`)

const profileIconUrl = computed(() => {
  if (!props.profileIconId || iconError.value) return null
  return `https://ddragon.leagueoflegends.com/cdn/${ddVersion}/img/profileicon/${props.profileIconId}.png`
})

const regionLabels = {
  euw1: 'EUW',
  eun1: 'EUNE',
  na1: 'NA',
  kr: 'KR',
  jp1: 'JP',
  br1: 'BR',
  la1: 'LAN',
  la2: 'LAS',
  oc1: 'OCE',
  tr1: 'TR',
  ru: 'RU',
  ph2: 'PH',
  sg2: 'SG',
  th2: 'TH',
  tw2: 'TW',
  vn2: 'VN'
}

const regionLabel = computed(() => regionLabels[props.region] || props.region.toUpperCase())

// Format rank display: "GOLD II (45 LP)" or "Unranked"
function formatRank(tier, rank, lp) {
  if (!tier) return '--'
  // Capitalize tier properly (GOLD -> Gold)
  const formattedTier = tier.charAt(0).toUpperCase() + tier.slice(1).toLowerCase()
  // For Master+ tiers, there's no division
  if (['MASTER', 'GRANDMASTER', 'CHALLENGER'].includes(tier.toUpperCase())) {
    return `${formattedTier} ${lp ?? 0} LP`
  }
  return `${formattedTier} ${rank || ''} ${lp !== null && lp !== undefined ? `(${lp} LP)` : ''}`.trim()
}

const soloRankDisplay = computed(() => formatRank(props.soloTier, props.soloRank, props.soloLp))
const flexRankDisplay = computed(() => formatRank(props.flexTier, props.flexRank, props.flexLp))

const winRateDisplay = computed(() => {
  if (props.winRate === null || props.winRate === undefined) return '--'
  return `${props.winRate.toFixed(1)}%`
})

	const gamesPlayedDisplay = computed(() => {
	  if (props.gamesPlayed === null || props.gamesPlayed === undefined) return '--'
	  return props.gamesPlayed.toString()
	})

	// Color the win rate value with a gradient from red -> green
	const winRateColorClass = computed(() => getWinRateColorClass(props.winRate))

function handleIconError() {
  iconError.value = true
}
</script>

<style scoped>
.profile-header-card {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  gap: var(--spacing-lg);
  padding: var(--spacing-xl);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  height: 100%;
  box-sizing: border-box;
}

/* Clickable variant (button styling per ui-design-guidelines) */
.profile-header-card.clickable {
  cursor: pointer;
  transition: all 0.2s ease;
  box-shadow: var(--shadow-sm);
}

.profile-header-card.clickable:hover {
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

.profile-header-card.clickable:active {
  transform: translateY(0);
}

.profile-icon-container {
  position: relative;
  flex-shrink: 0;
}

.profile-icon {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  border: 3px solid var(--color-primary);
  object-fit: cover;
}

.profile-icon-placeholder {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  border: 3px solid var(--color-border);
  background: var(--color-elevated);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--color-text-secondary);
}

.profile-icon-placeholder svg {
  width: 40px;
  height: 40px;
}

.level-badge {
  position: absolute;
  bottom: -4px;
  right: -4px;
  background: var(--color-primary);
  color: white;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-bold);
  padding: 2px 8px;
  border-radius: 10px;
  min-width: 28px;
  text-align: center;
}

.profile-info {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-md);
  text-align: center;
}

.identity {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-xs);
}

.riot-id {
  margin: 0;
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
  letter-spacing: var(--letter-spacing);
}

.region-badge {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
  background: var(--color-elevated);
  padding: 2px 8px;
  border-radius: var(--radius-sm);
  text-transform: uppercase;
}

.rank-badges {
  display: flex;
  justify-content: center;
  gap: var(--spacing-sm);
}

.rank-badge {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 4px 10px;
  background: var(--color-elevated);
  border-radius: var(--radius-sm);
  font-size: var(--font-size-sm);
}

.rank-badge.placeholder {
  opacity: 0.5;
}

.rank-icon {
  font-size: 14px;
}

.rank-text {
  color: var(--color-text-secondary);
  font-weight: var(--font-weight-medium);
}

.stats-row {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: var(--spacing-lg);
  margin-top: var(--spacing-sm);
}

.stat {
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: var(--font-size-lg);
	  font-weight: var(--font-weight-bold);
	  color: var(--color-text);
	}

	/* Win rate coloring gradient */
	.stat-value.winrate-red {
	  color: #ef4444; /* red */
	}

	.stat-value.winrate-redorange {
	  color: #f97316; /* red-orange */
	}

	.stat-value.winrate-orange {
	  color: #fdba74; /* orange */
	}

	.stat-value.winrate-yellow {
	  color: #eab308; /* yellow */
	}

	.stat-value.winrate-yellowgreen {
	  color: #84cc16; /* yellow-green */
	}

	.stat-value.winrate-green {
	  color: #22c55e; /* green */
	}

	.stat-value.winrate-neutral {
	  color: var(--color-text);
	}

.stat-label {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.stat-divider {
  width: 1px;
  height: 32px;
  background: var(--color-border);
}

/* Responsive adjustments */
@media (max-width: 640px) {
  .profile-header-card {
    flex-direction: column;
    text-align: center;
  }

  .identity {
    justify-content: center;
  }

  .rank-badges {
    justify-content: center;
  }

  .stats-row {
    justify-content: center;
  }
}
</style>

