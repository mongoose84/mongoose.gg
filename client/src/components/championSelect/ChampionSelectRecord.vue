<template>
  <section class="mp-card record" aria-labelledby="cs-record-title" data-testid="champion-select-record">
    <header>
      <h2 id="cs-record-title" class="mp-card-title" data-testid="champion-select-record-title">{{ title }}</h2>
      <p class="record__caption">{{ caption }}</p>
    </header>

    <ul v-if="records.length" class="record__rows" data-testid="champion-select-record-rows">
      <li
        v-for="row in rows"
        :key="row.championId"
        class="record__row"
        :data-testid="`champion-record-${row.championId}`"
      >
        <img :src="getChampionIconUrl(row.championName)" alt="" class="record__icon" />
        <div class="record__name">
          <span class="record__champion">{{ row.championName }}</span>
          <span class="record__meta">{{ row.meta }}</span>
        </div>
        <span
          v-if="row.form"
          class="mp-chip record__form"
          :class="`record__form--${row.form.kind}`"
          :data-testid="`champion-record-form-${row.championId}`"
        >{{ row.form.text }}</span>
        <span v-else class="record__form-gap" aria-hidden="true" />
        <BaseDivergingBar :value="row.net" :max="maxNet" :description="row.description" />
        <span
          class="record__value"
          :class="{ 'mp-up': row.net > 0, 'mp-down': row.net < 0 }"
          data-testid="champion-record-value"
        >{{ formatNet(row.net) }}</span>
      </li>
    </ul>
    <p v-else class="record__empty" data-testid="champion-select-record-empty">
      Play 3 matches on a champion in this role to see its record here.
    </p>

    <p v-if="leftOutNote" class="record__note" data-testid="champion-select-record-left-out">{{ leftOutNote }}</p>
  </section>
</template>

<script setup>
/**
 * "Your record" (features/champion-records.spec.md FR 4–6): every champion with 3 or more matches
 * in the role shown, as wins minus losses around zero, with its form over the last 20 matches and
 * the names left out. The title names a champion only when the margin is clear.
 */
import { computed } from 'vue'
import BaseDivergingBar from '../base/BaseDivergingBar.vue'
import { getChampionIconUrl } from '@/utils/leagueAssets'
import {
  FORM_CHIP_MIN_MATCHES,
  buildRecordCaption,
  buildRecordLeftOut,
  buildRecordTitle,
  formatNet,
  formatRecentRecord,
  recentForm
} from '@/utils/championSelectSummary'

const props = defineProps({
  /** The role's records: [{ championId, championName, matches, wins, net, recent }] */
  records: { type: Array, default: () => [] },
  /** Names with fewer than 3 matches in the role */
  leftOut: { type: Array, default: () => [] },
  role: { type: String, required: true },
  queueLabel: { type: String, required: true },
  timeLabel: { type: String, required: true }
})

const FORM_WORDS = { cold: '▼ Cold', hot: '▲ Hot' }

const title = computed(() => buildRecordTitle(props.records, props.role))
const caption = computed(() => buildRecordCaption(props.queueLabel, props.timeLabel))
const leftOutNote = computed(() => buildRecordLeftOut(props.leftOut))
const maxNet = computed(() => Math.max(1, ...props.records.map((r) => Math.abs(r.net))))

const rows = computed(() => props.records.map((record) => {
  const rate = Math.round((record.wins / record.matches) * 100)
  const matchWord = record.matches === 1 ? 'match' : 'matches'
  let form = null
  if (record.recent?.matches >= FORM_CHIP_MIN_MATCHES) {
    const kind = recentForm(record.recent, rate)
    const words = FORM_WORDS[kind]
    form = { kind, text: `Last 20: ${formatRecentRecord(record.recent)}${words ? ` ${words}` : ''}` }
  }
  return {
    ...record,
    meta: `${record.matches} ${matchWord} · ${rate}% win rate`,
    form,
    description: `${record.championName}: ${record.wins} wins, ${record.matches - record.wins} losses, ${formatNet(record.net)} net`
  }
}))
</script>

<style scoped>
.record {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.record__caption {
  margin: 0.25rem 0 0;
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.record__rows {
  margin: 0;
  padding: 0;
  list-style: none;
}

.record__row {
  display: grid;
  grid-template-columns: 2.25rem minmax(0, 11rem) 9.5rem minmax(0, 1fr) 3.5rem;
  align-items: center;
  gap: 0.75rem;
  min-height: 3.5rem;
  border-top: 1px solid var(--color-border);
}

.record__icon {
  width: 2.25rem;
  height: 2.25rem;
  border-radius: 0.75rem;
}

.record__name {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.record__champion {
  font-family: var(--font-display);
  font-size: 0.9375rem;
  font-weight: 600;
  color: var(--color-text);
}

.record__meta {
  font-size: 0.75rem;
  color: var(--color-text-secondary);
  white-space: nowrap;
}

.record__form {
  justify-self: start;
  background: var(--color-track);
  color: var(--color-text-secondary);
}

.record__form--cold {
  background: var(--color-warn-soft);
  color: var(--color-warn-text);
}

.record__form--hot {
  background: var(--color-primary-soft);
  color: var(--color-positive-text-strong);
}

.record__value {
  font-family: var(--font-display);
  font-size: 0.9375rem;
  font-weight: 600;
  text-align: right;
  font-variant-numeric: tabular-nums;
  color: var(--color-text-secondary);
}

.record__empty,
.record__note {
  margin: 0;
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

@media (max-width: 599px) {
  .record__row {
    grid-template-columns: 2.25rem minmax(0, 1fr) 3.5rem;
    padding: 0.5rem 0;
  }

  .record__value {
    grid-row: 1;
    grid-column: 3;
  }

  .record__form,
  .record__form-gap {
    grid-row: 2;
    grid-column: 2 / -1;
  }

  .record__form-gap {
    display: none;
  }

  .record__row :deep(.diverging-bar) {
    grid-row: 3;
    grid-column: 2 / -1;
  }
}
</style>
