<template>
  <section
    class="mp-card search"
    aria-labelledby="matchup-search-title"
    data-testid="champion-select-search"
  >
    <header class="search__header">
      <h2 id="matchup-search-title" class="mp-card-title">Check a matchup</h2>
      <p class="search__caption">See how your {{ roleName }} champions do against the enemy laner.</p>
    </header>

    <BaseInput
      v-model="query"
      label="Enemy champion"
      enterkeyhint="search"
      placeholder="Zed"
      :disabled="disabled"
      id="matchup-search-input"
    >
      <template #icon-left><BaseIcon name="search" :size="20" /></template>
    </BaseInput>

    <p v-if="statusText" class="search__status" aria-live="polite" data-testid="matchup-search-status">{{ statusText }}</p>

    <ul v-if="results.length" class="search__list" data-testid="matchup-search-results">
      <li
        v-for="result in results"
        :key="result.key"
        class="search__row"
        data-testid="matchup-search-row"
      >
        <img
          v-if="!failedIcons.has(result.championName)"
          :src="getChampionIconUrl(result.championName)"
          alt=""
          class="search__icon"
          @error="failedIcons.add(result.championName)"
        />
        <span v-else class="search__icon" aria-hidden="true" />
        <span class="search__name">
          <span class="search__champion">{{ result.championName }} <span class="search__vs">vs {{ result.opponentName }}</span></span>
          <span class="search__record" data-testid="matchup-search-record">{{ recordLine(result) }}</span>
        </span>
        <span
          class="search__winrate"
          :class="result.winRate >= 50 ? 'mp-up' : 'mp-down'"
          data-testid="matchup-search-winrate"
        >{{ Math.round(result.winRate) }}%<span class="visually-hidden"> win rate</span></span>
      </li>
    </ul>
  </section>
</template>

<script setup>
/**
 * "Check a matchup" on Champion Select: type the enemy laner and see how your champions in the
 * selected role did against them, lane record first.
 */
import { computed, reactive, ref } from 'vue'
import BaseInput from '../base/BaseInput.vue'
import BaseIcon from '../base/BaseIcon.vue'
import { getChampionIconUrl } from '@/utils/leagueAssets'
import { formatRecord, searchMatchups } from '@/utils/championSelectSummary'

const props = defineProps({
  /** Matchups from the API (`matchups[]`) */
  matchups: {
    type: Array,
    default: () => []
  },
  /** Role to search in (API role, e.g. MIDDLE) */
  role: {
    type: String,
    default: null
  },
  /** Role as players say it ("Mid") */
  roleName: {
    type: String,
    default: ''
  },
  /** While matchups are loading or failed */
  disabled: {
    type: Boolean,
    default: false
  }
})

const query = ref('')
const failedIcons = reactive(new Set())

const trimmedQuery = computed(() => query.value.trim())
const results = computed(() => searchMatchups(props.matchups, query.value, props.role))

const statusText = computed(() => {
  if (trimmedQuery.value.length < 2) return null
  if (!results.value.length) return `You haven't played ${props.roleName} against "${trimmedQuery.value}" yet.`
  const count = results.value.length
  return `${count} ${count === 1 ? 'matchup' : 'matchups'} found`
})

function recordLine(result) {
  const overall = `${formatRecord(result.wins, result.losses)} overall`
  if (!result.laneMatches) return overall
  return `${formatRecord(result.laneWins, result.laneLosses)} in lane · ${overall}`
}
</script>

<style scoped>
.search {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.search__header {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.search__caption,
.search__status {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.search__list {
  list-style: none;
}

.search__row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  min-height: 4rem;
  border-top: 1px solid var(--color-border);
}

.search__icon {
  width: 2.75rem;
  height: 2.75rem;
  flex-shrink: 0;
  border-radius: 0.75rem;
  background: var(--color-track);
}

.search__name {
  display: flex;
  flex-direction: column;
  flex-grow: 1;
  min-width: 0;
}

.search__champion {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 0.9375rem;
  font-weight: 600;
  color: var(--color-text);
}

.search__vs {
  font-weight: 400;
  color: var(--color-ink-soft);
}

.search__record {
  font-size: 0.75rem;
  font-variant-numeric: tabular-nums;
  color: var(--color-text-secondary);
}

.search__winrate {
  font-family: var(--font-display);
  font-size: 1.0625rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}
</style>
