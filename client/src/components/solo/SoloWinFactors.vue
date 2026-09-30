<template>
  <SoloCard
    test-id="solo-win-factors"
    title-id="solo-win-factors-title"
    :title="title"
    :caption="caption"
    error-title="Win factors"
    loading-label="Loading your win factors"
    :loading="loading"
    :error="error"
    :empty="empty"
    @retry="$emit('retry')"
  >
    <template #skeleton>
      <BaseSkeleton variant="title" />
      <div v-for="n in 4" :key="n" class="win-factors__skeleton-row">
        <BaseSkeleton width="45%" />
        <BaseSkeleton variant="block" height="0.875rem" />
      </div>
    </template>

    <template #empty-action>
      <BaseButton
        v-if="empty?.showAllQueues"
        variant="secondary"
        data-testid="solo-win-factors-show-all"
        @click="$emit('show-all-queues')"
      >Show all queues</BaseButton>
      <BaseButton v-else :disabled="syncing" data-testid="solo-win-factors-sync" @click="$emit('sync')">
        <template #icon-left><BaseIcon name="refresh-cw" :size="20" /></template>
        Sync matches
      </BaseButton>
    </template>

    <template #aside>
      <ul class="win-factors__key" aria-label="Chart key">
        <li><span class="win-factors__key-hit" aria-hidden="true" />Win rate when you hit it</li>
        <li><span class="win-factors__key-miss" aria-hidden="true" />When you missed it</li>
      </ul>
    </template>

    <ul class="win-factors__rows" data-testid="solo-win-factors-rows">
      <BaseWinFactorRow
        v-for="factor in data.factors"
        :key="factor.key"
        :label="factorLabel(factor)"
        :hit-win-rate="factor.hitWinRate"
        :miss-win-rate="factor.missWinRate"
        :description="describeWinFactor(factor)"
        :data-testid="`win-factor-${factor.key}`"
      />
    </ul>
  </SoloCard>
</template>

<script setup>
/**
 * Win factors: win rate when the player hits each mark vs misses it, largest gap first
 * (features/solo-trends.spec.md FR 21–23). Needs 20 matches and two rows.
 */
import { computed } from 'vue'
import SoloCard from './SoloCard.vue'
import BaseWinFactorRow from '../base/BaseWinFactorRow.vue'
import BaseSkeleton from '../base/BaseSkeleton.vue'
import BaseButton from '../base/BaseButton.vue'
import BaseIcon from '../base/BaseIcon.vue'
import {
  buildNoMatchesEmpty,
  buildWinFactorsEmpty,
  buildWinFactorsTitle,
  describeWinFactor,
  factorLabel,
  rangeText
} from '@/utils/soloSummary'

const props = defineProps({
  /** win-factors response, or null before the first load */
  data: { type: Object, default: null },
  loading: { type: Boolean, default: false },
  error: { type: Boolean, default: false },
  /** The selected queue, for the empty state */
  queue: { type: String, default: null },
  syncing: { type: Boolean, default: false }
})

defineEmits(['retry', 'show-all-queues', 'sync'])

const empty = computed(() => {
  if (!props.data) return null
  if (props.data.matches === 0) return buildNoMatchesEmpty(props.queue)
  return buildWinFactorsEmpty(props.data.matches, props.data.factors)
})

const title = computed(() => buildWinFactorsTitle(props.data?.factors) ?? '')
const caption = computed(() => (props.data ? `Win rate when you hit each mark, ${rangeText(props.data.range, props.data.matches)}` : null))
</script>

<style scoped>
.win-factors__rows {
  margin: 0;
  padding: 0;
  list-style: none;
}

.win-factors__skeleton-row {
  display: flex;
  flex-direction: column;
  gap: 0.625rem;
  padding: 0.875rem 0;
}

.win-factors__key {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem 1rem;
  margin: 0;
  padding: 0;
  list-style: none;
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.win-factors__key li {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
}

.win-factors__key-hit,
.win-factors__key-miss {
  width: 0.75rem;
  height: 0.75rem;
  border-radius: 999px;
}

.win-factors__key-hit {
  background: var(--color-primary-accent);
}

.win-factors__key-miss {
  border: 2px solid var(--color-warn);
}
</style>
