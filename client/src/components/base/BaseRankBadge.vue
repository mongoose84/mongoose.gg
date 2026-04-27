<template>
  <div
    v-if="effectiveHasRank"
    class="rank-badge"
    :class="[sizeClass, `rank-badge--${tierLower}`]"
    data-testid="rank-badge"
  >
    <img
      :src="rankEmblemUrl"
      :alt="`${tier} rank emblem`"
      class="rank-emblem"
      data-testid="rank-emblem"
    />
    <div class="rank-info">
      <span class="rank-tier" data-testid="rank-tier">{{ formattedTier }}</span>
      <span
        v-if="showLp && lp !== null && lp !== undefined"
        class="rank-lp"
        data-testid="rank-lp"
      >
        {{ lp }} LP
      </span>
    </div>
  </div>
  <div
    v-else
    class="rank-badge rank-badge--unranked"
    :class="sizeClass"
    data-testid="rank-badge-unranked"
  >
    <span class="unranked-text">Unranked</span>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  /** Rank tier (IRON, BRONZE, SILVER, GOLD, PLATINUM, EMERALD, DIAMOND, MASTER, GRANDMASTER, CHALLENGER) */
  tier: {
    type: String,
    default: null
  },
  /** Rank division (I, II, III, IV) - not used for Master+ */
  division: {
    type: String,
    default: null
  },
  /** League points */
  lp: {
    type: Number,
    default: null
  },
  /** Whether player has a rank */
  hasRank: {
    type: Boolean,
    default: false
  },
  /** Badge size variant: sm, md, lg */
  size: {
    type: String,
    default: 'sm',
    validator: (v) => ['sm', 'md', 'lg'].includes(v)
  },
  /** Whether to show LP value */
  showLp: {
    type: Boolean,
    default: true
  }
})

// Computed: Effective hasRank - only true if hasRank and tier are both valid
const effectiveHasRank = computed(() => {
  return props.hasRank && !!props.tier && props.tier.trim() !== ''
})

// Computed: lowercase tier for CSS class
const tierLower = computed(() => {
  return props.tier?.toLowerCase() || 'unranked'
})

// Computed: Size class
const sizeClass = computed(() => `rank-badge--${props.size}`)

// Computed: Rank emblem URL
const rankEmblemUrl = computed(() => {
  if (!props.tier) return ''
  return `/assets/ranked/emblem-${tierLower.value}.png`
})

// Computed: Formatted tier with division (if applicable)
const formattedTier = computed(() => {
  if (!props.tier) return 'Unranked'

  const tierDisplay = props.tier.charAt(0) + props.tier.slice(1).toLowerCase()

  // Master, Grandmaster, Challenger don't have divisions
  const highTiers = ['MASTER', 'GRANDMASTER', 'CHALLENGER']
  if (highTiers.includes(props.tier.toUpperCase())) {
    return tierDisplay
  }

  return props.division ? `${tierDisplay} ${props.division}` : tierDisplay
})
</script>

<style scoped>
.rank-badge {
  display: flex;
  align-items: center;
  gap: var(--spacing-xs);
}

.rank-emblem {
  object-fit: contain;
}

.rank-info {
  display: flex;
  flex-direction: column;
  gap: 1px;
}

.rank-tier {
  font-weight: 600;
  color: var(--color-text);
  line-height: 1.2;
}

.rank-lp {
  font-size: 0.75em;
  color: var(--color-text-secondary);
  line-height: 1.2;
}

.unranked-text {
  color: var(--color-text-secondary);
  font-style: italic;
}

/* Size variants */
.rank-badge--sm .rank-emblem {
  width: 28px;
  height: 28px;
}

.rank-badge--sm .rank-tier {
  font-size: var(--font-size-sm);
}

.rank-badge--sm .rank-lp {
  font-size: var(--font-size-xs);
}

.rank-badge--md .rank-emblem {
  width: 32px;
  height: 32px;
}

.rank-badge--md .rank-tier {
  font-size: var(--font-size-sm);
}

.rank-badge--md .rank-lp {
  font-size: var(--font-size-xs);
}

.rank-badge--lg .rank-emblem {
  width: 48px;
  height: 48px;
}

.rank-badge--lg .rank-tier {
  font-size: var(--font-size-md);
}

.rank-badge--lg .rank-lp {
  font-size: var(--font-size-sm);
}

/* Tier-specific accent colors for visual recognition */
.rank-badge--iron .rank-tier {
  color: var(--color-rank-iron);
}

.rank-badge--bronze .rank-tier {
  color: var(--color-rank-bronze);
}

.rank-badge--silver .rank-tier {
  color: var(--color-rank-silver);
}

.rank-badge--gold .rank-tier {
  color: var(--color-rank-gold);
}

.rank-badge--platinum .rank-tier {
  color: var(--color-rank-platinum);
}

.rank-badge--emerald .rank-tier {
  color: var(--color-rank-emerald);
}

.rank-badge--diamond .rank-tier {
  color: var(--color-rank-diamond);
}

.rank-badge--master .rank-tier {
  color: var(--color-rank-master);
}

.rank-badge--grandmaster .rank-tier {
  color: var(--color-rank-grandmaster);
}

.rank-badge--challenger .rank-tier {
  color: var(--color-rank-challenger);
}
</style>
