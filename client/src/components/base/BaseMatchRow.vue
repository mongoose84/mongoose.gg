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
      <span class="match-row__champion" data-testid="match-row-champion">{{ championName }}</span>
      <span class="match-row__meta" data-testid="match-row-meta">
        {{ metaLine }}<span v-if="kda" class="match-row__meta-kda"> · {{ kda }}</span>
      </span>
    </span>

    <span class="match-row__kda" data-testid="match-row-kda">
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
      :class="lpChange >= 0 ? 'mp-up' : 'mp-down'"
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
  if (props.remake) return 'match-row__portrait--remake'
  return props.win ? null : 'mp-portrait--loss'
})

const resultText = computed(() => {
  if (props.remake) return 'Remake'
  return props.win ? 'Victory' : 'Defeat'
})

const resultClass = computed(() => {
  if (props.remake) return 'match-row__result--remake'
  return props.win ? 'mp-up' : 'mp-down'
})

// Signed LP with a real minus sign
const lpDisplay = computed(() => {
  if (props.lpChange == null) return ''
  return props.lpChange >= 0 ? `+${props.lpChange} LP` : `−${Math.abs(props.lpChange)} LP`
})
</script>

<style scoped>
.match-row--no-lp {
  grid-template-columns: 3.25rem minmax(0, 1fr) 6.875rem 5.625rem;
}

.match-row__portrait-fallback {
  display: block;
  background: var(--color-track);
}

.match-row__portrait--remake {
  border-color: var(--color-track-strong);
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
  color: var(--color-text);
}

.match-row__meta {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 0.8125rem;
  line-height: 1.4;
  color: var(--color-text-secondary);
}

.match-row__meta-kda {
  display: none;
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

.match-row__result--remake {
  color: var(--color-text-secondary);
}

.match-row:hover .match-row__champion {
  color: var(--color-positive-text-strong);
}

/* The open match: the selected ground reaches into the card padding so the columns stay aligned */
.match-row[aria-current="page"] {
  margin-inline: -0.75rem;
  padding-inline: 0.75rem;
  border-radius: 0.75rem;
  border-top-color: transparent;
  background: var(--color-surface-selected);
}

.match-row[aria-current="page"] + .match-row {
  border-top-color: transparent;
}

/* Phones: KDA moves into the meta line */
@media (max-width: 899px) {
  .mp-match-row,
  .match-row--no-lp {
    grid-template-columns: 3.25rem minmax(0, 1fr) auto;
  }

  .match-row__kda {
    display: none;
  }

  .match-row__meta {
    white-space: normal;
  }

  .match-row__meta-kda {
    display: inline;
  }

  .mp-lp {
    grid-column: 3;
  }
}

/* Same layout in a narrow list column (the Matches page list is a `match-list` container) */
@container match-list (max-width: 30rem) {
  .mp-match-row,
  .match-row--no-lp {
    grid-template-columns: 3.25rem minmax(0, 1fr) auto;
  }

  .match-row__kda {
    display: none;
  }

  .match-row__meta {
    white-space: normal;
  }

  .match-row__meta-kda {
    display: inline;
  }

  .mp-lp {
    grid-column: 3;
  }
}
</style>
