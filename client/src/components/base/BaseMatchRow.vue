<template>
  <router-link
    :to="to"
    class="mp-match-row match-row"
    :class="{ 'match-row--no-lp': lpChange == null }"
    data-testid="match-row"
  >
    <img
      v-if="portraitUrl && !iconFailed"
      :src="portraitUrl"
      alt=""
      class="mp-portrait"
      :class="portraitClass"
      data-testid="match-row-portrait"
      @error="iconFailed = true"
    />
    <span
      v-else
      class="mp-portrait match-row__portrait-fallback"
      :class="portraitClass"
      aria-hidden="true"
    />

    <span class="match-row__main">
      <span class="mp-match-name match-row__champion" data-testid="match-row-champion">{{ championName }}</span>
      <span class="match-row__meta" data-testid="match-row-meta">
        {{ metaLine }}<span v-if="kda" class="mp-match-meta-kda"> · {{ kda }}</span>
      </span>
    </span>

    <span class="mp-match-kda match-row__kda" data-testid="match-row-kda">
      <template v-if="kda">{{ kda }}</template>
    </span>

    <span
      class="match-row__result"
      :class="resultClass"
      data-testid="match-row-result"
    >{{ resultText }}</span>

    <span
      v-if="lpChange != null"
      class="mp-lp"
      :class="lpClass"
      data-testid="match-row-lp"
    >{{ lpDisplay }}</span>
  </router-link>
</template>

<script setup>
/**
 * MatchRow (design system): one match in a list — portrait, meta, KDA, result and LP change.
 * The whole row is one link. Win → primary portrait border and positive text; loss → warn;
 * a remake is neither. Opening a match never animates. The row of the open match gets
 * aria-current="page" from router-link and the surface-selected ground.
 */
import { computed, ref } from 'vue'
import { getChampionIconUrl } from '@/utils/leagueAssets'
import { formatDuration, formatRelativeTime } from '@/utils/formatters'
import { formatLpChange, lpChangeClass } from '@/utils/matchesSummary'

const props = defineProps({
  /** Where the row leads (router-link target) */
  to: {
    type: [String, Object],
    required: true
  },
  championName: {
    type: String,
    required: true
  },
  /** Icon URL from the API; falls back to Data Dragon by champion name */
  championIconUrl: {
    type: String,
    default: null
  },
  win: {
    type: Boolean,
    required: true
  },
  /** "K / D / A" string, e.g. "7/2/9" */
  kda: {
    type: String,
    default: null
  },
  /** Queue label, e.g. "Ranked Solo/Duo" */
  queue: {
    type: String,
    default: null
  },
  /** Match length in seconds */
  durationSeconds: {
    type: Number,
    default: null
  },
  /** Match end time, epoch milliseconds */
  timestamp: {
    type: Number,
    default: null
  },
  /** Signed LP change; hidden when unknown */
  lpChange: {
    type: Number,
    default: null
  },
  /** Ended early (remake): neither a win nor a loss */
  remake: {
    type: Boolean,
    default: false
  },
  /** Riot ID the match was played on, shown in Overall mode */
  riotId: {
    type: String,
    default: null
  }
})

const iconFailed = ref(false)

const portraitUrl = computed(() => props.championIconUrl || getChampionIconUrl(props.championName))

const metaLine = computed(() => [
  props.queue,
  props.durationSeconds ? formatDuration(props.durationSeconds) : null,
  props.timestamp ? formatRelativeTime(props.timestamp) : null,
  props.riotId
].filter(Boolean).join(' · '))

const portraitClass = computed(() => {
  if (props.remake) return 'mp-portrait--remake'
  return props.win ? null : 'mp-portrait--loss'
})

const resultText = computed(() => {
  if (props.remake) return 'Remake'
  return props.win ? 'Victory' : 'Defeat'
})

const resultClass = computed(() => {
  if (props.remake) return 'mp-result--remake'
  return props.win ? 'mp-up' : 'mp-down'
})

// Signed LP with a real minus sign; no change is neutral
const lpDisplay = computed(() => formatLpChange(props.lpChange) ?? '')
const lpClass = computed(() => lpChangeClass(props.lpChange))
</script>

<style scoped>
.match-row--no-lp {
  grid-template-columns: 3.25rem minmax(0, 1fr) 6.875rem 5.625rem;
}

.match-row__portrait-fallback {
  display: block;
  background: var(--color-track);
}

.match-row__main {
  display: flex;
  flex-direction: column;
  gap: 0.125rem;
  min-width: 0;
}

.match-row__champion {
  font-family: var(--font-display);
  font-size: 1rem;
  font-weight: 600;
  line-height: 1.3;
}

.match-row__meta {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 0.8125rem;
  line-height: 1.4;
  color: var(--color-text-secondary);
}

.match-row__kda {
  font-family: var(--font-display);
  font-size: 1rem;
  font-weight: 600;
  font-variant-numeric: tabular-nums;
  color: var(--color-text);
}

.match-row__result {
  font-size: 0.875rem;
  font-weight: 700;
}

/* Phones: KDA moves into the meta line. A narrow list column does the same through mp-match-list. */
@media (max-width: 899px) {
  .mp-match-row,
  .match-row--no-lp {
    grid-template-columns: 3.25rem minmax(0, 1fr) auto;
  }

  .mp-match-kda {
    display: none;
  }

  .mp-match-meta-kda {
    display: inline;
  }

  .match-row__meta {
    white-space: normal;
  }

  /* LP above the result, both in the last column */
  .mp-lp {
    grid-column: 3;
    grid-row: 1;
    align-self: end;
  }

  .match-row__result:has(~ .mp-lp) {
    grid-column: 3;
    grid-row: 2;
    align-self: start;
    font-size: 0.75rem;
    text-align: right;
  }

  .match-row:has(.mp-lp) > .mp-portrait,
  .match-row:has(.mp-lp) > .match-row__main {
    grid-row: 1 / span 2;
  }
}

@container match-list (max-width: 30rem) {
  .match-row--no-lp {
    grid-template-columns: 3.25rem minmax(0, 1fr) auto;
  }

  .match-row__meta {
    white-space: normal;
  }

  /* LP above the result, both in the last column */
  .mp-lp {
    grid-column: 3;
    grid-row: 1;
    align-self: end;
  }

  .match-row__result:has(~ .mp-lp) {
    grid-column: 3;
    grid-row: 2;
    align-self: start;
    font-size: 0.75rem;
    text-align: right;
  }

  .match-row:has(.mp-lp) > .mp-portrait,
  .match-row:has(.mp-lp) > .match-row__main {
    grid-row: 1 / span 2;
  }
}
</style>
