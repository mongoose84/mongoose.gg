<template>
  <section
    class="mp-hero mp-hero--match match-header"
    :class="{ 'match-header--plain': !hasArt }"
    aria-labelledby="match-header-title"
    data-testid="match-header"
  >
    <template v-if="hasArt">
      <img
        :src="splashUrl"
        :alt="`${match.championName} splash art`"
        class="mp-hero-art match-header__art"
        data-testid="match-header-art"
        @error="artFailed = true"
      />
      <div class="mp-hero-scrim match-header__scrim" aria-hidden="true" />
    </template>

    <div class="mp-hero-body match-header__body">
      <div class="match-header__heading">
        <p class="mp-eyebrow" :class="resultClass" data-testid="match-header-result">{{ eyebrow }}</p>
        <h2 id="match-header-title" data-testid="match-header-champion">{{ title }}</h2>
        <p class="match-header__meta" data-testid="match-header-meta">{{ metaLine }}</p>
      </div>

      <ul class="match-header__chips" aria-label="This match">
        <li class="mp-chip mp-chip--glass" data-testid="match-header-kda">
          <span class="match-header__chip-number">{{ match.kills }} / {{ match.deaths }} / {{ match.assists }}</span> KDA
        </li>
        <li v-if="lpText" class="mp-chip mp-chip--glass" data-testid="match-header-lp">
          <span class="match-header__chip-number" :class="lpClass">{{ lpText }}</span> LP
        </li>
        <li v-if="rankAfter" class="mp-chip mp-chip--glass" data-testid="match-header-rank">
          <span
            class="match-header__tier-dot"
            :style="{ background: `var(--color-rank-${rankAfter.tierKey}, var(--color-text-secondary))` }"
            aria-hidden="true"
          />{{ rankAfter.label }}
        </li>
      </ul>
    </div>
  </section>
</template>

<script setup>
/**
 * The open match's banner (ChampionHero match banner in the design system): the champion's
 * splash art, the result and time as the eyebrow, "Champion, Role" as the title, queue and
 * length, and glass chips: K / D / A, the signed LP change and the rank after the match with its
 * tier colour (the last two only for ranked matches with recorded LP).
 * Win → positive text, loss → warn text, remake → neither. Without art it is a plain card.
 */
import { computed, ref, watch } from 'vue'
import { formatRole, formatDuration, formatRelativeTime } from '@/utils/formatters'
import { getChampionSplashUrl } from '@/utils/leagueAssets'
import { isRemake, formatSigned, lpChangeClass, formatRankAfter } from '@/utils/matchesSummary'

const props = defineProps({
  match: {
    type: Object,
    required: true
  }
})

const artFailed = ref(false)
watch(() => props.match.matchId, () => { artFailed.value = false })

const splashUrl = computed(() => getChampionSplashUrl(props.match.championName))
const hasArt = computed(() => Boolean(splashUrl.value) && !artFailed.value)

const remake = computed(() => isRemake(props.match))

const resultText = computed(() => {
  if (remake.value) return 'Remake'
  return props.match.win ? 'Victory' : 'Defeat'
})

const eyebrow = computed(() => {
  const when = props.match.gameStartTime ? formatRelativeTime(props.match.gameStartTime) : null
  return [resultText.value, when].filter(Boolean).join(' · ')
})

const resultClass = computed(() => {
  if (remake.value) return 'match-header__result--remake'
  return props.match.win ? 'mp-up' : 'mp-down'
})

const title = computed(() => {
  const role = props.match.role && props.match.role !== 'UNKNOWN' ? formatRole(props.match.role) : null
  return role ? `${props.match.championName}, ${role}` : props.match.championName
})

// LP change and rank after the match (ranked queues with recorded LP only)
const lpText = computed(() => {
  const change = props.match.lpChange
  if (typeof change !== 'number') return null
  return change === 0 ? '±0' : formatSigned(change)
})
const lpClass = computed(() => {
  const cls = lpChangeClass(props.match.lpChange)
  return cls ? `match-header__chip-number--${cls === 'mp-up' ? 'up' : 'down'}` : null
})
const rankAfter = computed(() =>
  formatRankAfter(props.match.tierAfter, props.match.rankAfter, props.match.lpAfter)
)

const metaLine = computed(() => [
  props.match.queueType,
  formatDuration(props.match.gameDurationSec)
].filter(Boolean).join(' · '))
</script>

<style scoped>
.match-header__heading {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

/* mp-eyebrow sets the purple; the result classes must win over it */
.match-header__heading .mp-up { color: var(--color-positive-text); }
.match-header__heading .mp-down { color: var(--color-warn-text); }
.match-header__heading .match-header__result--remake { color: var(--color-text-secondary); }

.match-header__meta {
  font-size: 0.875rem;
}

.match-header__chips {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
  list-style: none;
}

.match-header__chip-number--up { color: var(--color-positive-text); }
.match-header__chip-number--down { color: var(--color-warn-text); }

.match-header__tier-dot {
  width: 0.625rem;
  height: 0.625rem;
  margin-right: 0.5rem;
  border-radius: 999px;
}

.match-header__chip-number {
  margin-right: 0.125rem;
  font-family: var(--font-display);
  font-size: 0.9375rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
}

.match-header__art {
  width: 70%;
  object-position: 70% 25%;
}

/* No art: a plain surface card with the same text, no fixed height */
.match-header--plain,
.match-header--plain .match-header__body {
  min-height: 0;
}

/* Phones: the art fills the banner with a bottom scrim, the text sits at the bottom */
@media (max-width: 899px) {
  .match-header,
  .match-header .match-header__body {
    min-height: 14.375rem;
  }

  .match-header--plain,
  .match-header--plain .match-header__body {
    min-height: 0;
  }

  .match-header__art {
    width: 100%;
  }

  .match-header__scrim {
    width: 100%;
    background: linear-gradient(180deg, rgba(10, 8, 16, 0.2) 0%, rgba(10, 8, 16, 0.8) 55%, var(--color-bg) 100%);
  }

  .match-header .match-header__body {
    justify-content: flex-end;
    gap: 0.625rem;
    padding: 1.125rem;
  }

  .match-header .match-header__body h2 {
    font-size: 1.875rem;
  }

  .match-header__heading {
    gap: 0.125rem;
  }
}
</style>
