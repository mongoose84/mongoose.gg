<template>
  <SoloCard
    test-id="solo-climb"
    title-id="solo-climb-title"
    :title="title"
    :caption="caption"
    error-title="Your climb"
    loading-label="Loading your climb"
    :loading="loading"
    :error="error"
    :empty="empty"
    @retry="$emit('retry')"
  >
    <template #skeleton>
      <BaseSkeleton variant="title" />
      <BaseSkeleton variant="block" height="12.5rem" />
    </template>

    <template #empty-action>
      <BaseButton
        v-if="empty?.showAllQueues"
        variant="secondary"
        data-testid="solo-climb-show-all"
        @click="$emit('show-all-queues')"
      >Show all queues</BaseButton>
      <BaseButton v-else :disabled="syncing" data-testid="solo-climb-sync" @click="$emit('sync')">
        <template #icon-left><BaseIcon name="refresh-cw" :size="20" /></template>
        Sync matches
      </BaseButton>
    </template>

    <template #aside>
      <dl class="climb__stats" data-testid="solo-climb-stats">
        <div v-for="stat in stats" :key="stat.key" class="climb__stat">
          <dt>{{ stat.label }}</dt>
          <dd>{{ stat.value }}</dd>
        </div>
      </dl>
    </template>

    <BaseLineChart
      :points="chart.points"
      :length="data.matches"
      :y-min="chart.yMin"
      :y-max="chart.yMax"
      :guides="chart.guides"
      :markers="chart.markers"
      :aria-label="description"
    />
    <p v-if="showLpNote" class="climb__note" data-testid="solo-climb-lp-note">LP appears here as your ranked matches sync.</p>
  </SoloCard>
</template>

<script setup>
/**
 * The climb card (features/solo-trends.spec.md FR 9–12): the LP ladder with its divisions,
 * promotions and biggest drop, or the 10-match win rate when LP coverage is too thin.
 */
import { computed } from 'vue'
import SoloCard from './SoloCard.vue'
import BaseLineChart from '../base/BaseLineChart.vue'
import BaseSkeleton from '../base/BaseSkeleton.vue'
import BaseButton from '../base/BaseButton.vue'
import BaseIcon from '../base/BaseIcon.vue'
import {
  buildClimbCaption,
  buildClimbEmpty,
  buildClimbStats,
  buildClimbTitle,
  buildNoMatchesEmpty,
  describeClimb,
  formatLp,
  ladderDivision,
  rankName
} from '@/utils/soloSummary'

// Fewer guides read better than one per division on a long climb
const MAX_GUIDES = 5

// Promotions this close together (bouncing on a boundary) keep their dots, but only the last is labelled
const MIN_LABEL_GAP = 5

const props = defineProps({
  /** climb response, or null before the first load */
  data: { type: Object, default: null },
  loading: { type: Boolean, default: false },
  error: { type: Boolean, default: false },
  /** The selected queue, for the empty state */
  queue: { type: String, default: null },
  syncing: { type: Boolean, default: false },
  /** One account in scope: LP can appear once it syncs */
  singleAccount: { type: Boolean, default: true }
})

defineEmits(['retry', 'show-all-queues', 'sync'])

const isLp = computed(() => props.data?.mode === 'lp' && props.data.lp)

const empty = computed(() => {
  if (!props.data) return null
  if (props.data.matches === 0) return buildNoMatchesEmpty(props.queue)
  return buildClimbEmpty(props.data)
})

const title = computed(() => (props.data ? buildClimbTitle(props.data) ?? '' : ''))
const caption = computed(() => (props.data ? buildClimbCaption(props.data) : null))
const stats = computed(() => (props.data?.matches ? buildClimbStats(props.data) : []))
const description = computed(() => (props.data && !empty.value ? describeClimb(props.data) : ''))

const showLpNote = computed(() =>
  !isLp.value && props.singleAccount && ['ranked_solo', 'ranked_flex'].includes(props.data?.queueType)
)

function divisionGuides(min, max) {
  const guides = []
  for (let y = Math.ceil(min / 100) * 100; y <= max && y <= 2800; y += 100) {
    guides.push({ y, label: ladderDivision(y) })
  }
  // Too many divisions: keep tier floors only
  return guides.length > MAX_GUIDES ? guides.filter((g) => g.y % 400 === 0) : guides
}

const chart = computed(() => {
  if (!props.data) return { points: [], guides: [], markers: [] }

  if (isLp.value) {
    const { points, events, biggestDrop } = props.data.lp
    const ladderAt = new Map(points.map((p) => [p.index, p.ladder]))
    const ladders = points.map((p) => p.ladder)
    const markers = events.map((e, i) => {
      const next = events[i + 1]
      return {
        x: e.index,
        y: ladderAt.get(e.index),
        label: next && next.index - e.index < MIN_LABEL_GAP ? '' : rankName(e.tier, e.division),
        tone: 'primary'
      }
    })
    if (biggestDrop && ladderAt.has(biggestDrop.index)) {
      markers.push({ x: biggestDrop.index, y: ladderAt.get(biggestDrop.index), label: `▼ ${formatLp(biggestDrop.lp)}`, tone: 'warn' })
    }
    return {
      points: points.map((p) => ({ x: p.index, y: p.ladder })),
      guides: divisionGuides(Math.min(...ladders), Math.max(...ladders)),
      markers,
      yMin: null,
      yMax: null
    }
  }

  return {
    points: (props.data.winRate?.points ?? []).map((p) => ({ x: p.index, y: p.rate })),
    guides: [{ y: 50, label: '50%' }],
    markers: [],
    yMin: 0,
    yMax: 100
  }
})
</script>

<style scoped>
.climb__stats {
  display: flex;
  gap: 1.5rem;
  margin: 0;
}

.climb__stat {
  display: flex;
  flex-direction: column;
  gap: 0.125rem;
}

.climb__stat dt {
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.climb__stat dd {
  margin: 0;
  font-family: var(--font-display);
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--color-text);
  font-variant-numeric: tabular-nums;
}

.climb__note {
  margin: 0;
  font-size: 0.875rem;
  color: var(--color-text-secondary);
}
</style>
