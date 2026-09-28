<template>
  <section class="mp-card match-header" aria-labelledby="match-header-title" data-testid="match-header">
    <div class="match-header__top">
      <img
        v-if="portraitUrl && !iconFailed"
        :src="portraitUrl"
        alt=""
        class="mp-portrait match-header__portrait"
        :class="portraitClass"
        data-testid="match-header-portrait"
        @error="iconFailed = true"
      />
      <span
        v-else
        class="mp-portrait match-header__portrait match-header__portrait--fallback"
        :class="portraitClass"
        aria-hidden="true"
      />

      <div class="match-header__heading">
        <p class="mp-eyebrow" :class="resultClass" data-testid="match-header-result">{{ resultText }}</p>
        <h2 id="match-header-title" class="match-header__title" data-testid="match-header-champion">{{ match.championName }}</h2>
        <p class="match-header__meta" data-testid="match-header-meta">{{ metaLine }}</p>
      </div>

      <BaseButton
        variant="secondary"
        size="sm"
        class="match-header__download"
        data-testid="match-download"
        @click="$emit('download')"
      >Download data</BaseButton>
    </div>

    <dl class="match-header__stats">
      <div class="match-header__stat">
        <dt>Your KDA</dt>
        <dd class="match-header__number" data-testid="match-header-kda">
          {{ match.kills }} / {{ match.deaths }} / {{ match.assists }}
        </dd>
      </div>
      <div class="match-header__stat">
        <dt>Team kills</dt>
        <dd class="match-header__number" data-testid="match-header-score">
          {{ match.teamKills }} – {{ match.enemyTeamKills }}
        </dd>
      </div>
      <div class="match-header__stat">
        <dt>CS</dt>
        <dd class="match-header__number" data-testid="match-header-cs">
          {{ match.creepScore }}<span class="match-header__unit"> · {{ csPerMinute }} per min</span>
        </dd>
      </div>
    </dl>

    <p v-if="badge?.text" class="match-header__badge">
      <span
        class="mp-chip"
        :class="badge.type === 'positive' ? 'mp-chip--strength' : 'mp-chip--trend'"
        data-testid="match-header-badge"
      >
        <BaseIcon :name="badge.type === 'positive' ? 'trending-up' : 'trending-down'" :size="16" />
        {{ badge.text }}
      </span>
    </p>
  </section>
</template>

<script setup>
/**
 * The open match's summary card: result, champion, meta line and the three numbers players
 * check first (KDA, team kills, CS). Win → purple, loss → orange, remake → neither.
 */
import { computed, ref, watch } from 'vue'
import BaseButton from '../base/BaseButton.vue'
import BaseIcon from '../base/BaseIcon.vue'
import { formatRole, formatDuration, formatRelativeTime } from '@/utils/formatters'
import { getChampionIconUrl } from '@/utils/leagueAssets'
import { isRemake } from '@/utils/matchesSummary'

const props = defineProps({
  match: {
    type: Object,
    required: true
  },
  /** The match's standout finding from the list ({ text, type }), if any */
  badge: {
    type: Object,
    default: null
  }
})

defineEmits(['download'])

const iconFailed = ref(false)
watch(() => props.match.matchId, () => { iconFailed.value = false })

const remake = computed(() => isRemake(props.match))

const portraitUrl = computed(() => props.match.championIconUrl || getChampionIconUrl(props.match.championName))

const portraitClass = computed(() => {
  if (remake.value) return 'match-header__portrait--remake'
  return props.match.win ? null : 'mp-portrait--loss'
})

const resultText = computed(() => {
  if (remake.value) return 'Remake'
  return props.match.win ? 'Victory' : 'Defeat'
})

const resultClass = computed(() => {
  if (remake.value) return 'match-header__result--remake'
  return props.match.win ? 'mp-up' : 'mp-down'
})

const metaLine = computed(() => {
  const role = props.match.role && props.match.role !== 'UNKNOWN' ? formatRole(props.match.role) : null
  return [
    role,
    props.match.queueType,
    formatDuration(props.match.gameDurationSec),
    formatRelativeTime(props.match.gameStartTime)
  ].filter(Boolean).join(' · ')
})

const csPerMinute = computed(() => (props.match.csPerMin ?? 0).toFixed(1))
</script>

<style scoped>
.match-header {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.match-header__top {
  display: flex;
  align-items: center;
  gap: 1.25rem;
}

.match-header__portrait {
  width: 4rem;
  height: 4rem;
  flex-shrink: 0;
}

.match-header__portrait--fallback {
  display: block;
  background: var(--color-track);
}

.match-header__portrait--remake {
  border-color: var(--color-track-strong);
}

.match-header__heading {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  flex-grow: 1;
  min-width: 0;
}

.match-header__result--remake {
  color: var(--color-text-secondary);
}

.match-header__title {
  font-family: var(--font-display);
  font-size: 1.75rem;
  font-weight: 600;
  line-height: 1.15;
  color: var(--color-text);
}

.match-header__meta {
  font-size: 0.875rem;
  color: var(--color-text-secondary);
}

.match-header__download {
  flex-shrink: 0;
  align-self: flex-start;
}

.match-header__stats {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 1.25rem;
  padding-top: 1.25rem;
  border-top: 1px solid var(--color-border);
}

.match-header__stat {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  min-width: 0;
}

.match-header__stat dt {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.match-header__number {
  font-family: var(--font-display);
  font-size: 1.5rem;
  font-weight: 600;
  line-height: 1.2;
  font-variant-numeric: tabular-nums;
  color: var(--color-text);
}

.match-header__unit {
  font-family: var(--font-body);
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-text-secondary);
}

@media (max-width: 599px) {
  .match-header__top {
    flex-wrap: wrap;
  }

  .match-header__download {
    order: 3;
  }

  .match-header__stats {
    grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  }
}
</style>
