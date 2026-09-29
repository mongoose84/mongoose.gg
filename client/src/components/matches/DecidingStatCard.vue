<template>
  <section
    class="mp-card mp-card--highlight deciding-stat-card"
    aria-labelledby="deciding-stat-finding"
    data-testid="deciding-stat-card"
  >
    <p class="mp-eyebrow">What decided it</p>
    <h2 id="deciding-stat-finding" class="deciding-stat-card__finding">{{ finding }}</h2>

    <div class="deciding-stat-card__meters">
      <BaseUsualMeter
        v-for="meter in meters"
        :key="meter.stat"
        :label="meter.label"
        :value="meter.value"
        :now="meter.now"
        :min="meter.min"
        :max="meter.max"
        :usual="meter.usual"
        :better="meter.better"
        :value-text="meter.valueText"
        :data-testid="`deciding-stat-meter-${meter.stat}`"
      />
    </div>

    <p class="deciding-stat-card__note">{{ usualNote }}</p>

    <div v-if="fix" class="deciding-stat-card__fix" data-testid="deciding-stat-fix">
      <BaseIcon :name="fix.icon" :size="20" class="deciding-stat-card__fix-icon" />
      <p><strong>Next match:</strong> {{ fix.text }}</p>
    </div>
  </section>
</template>

<script setup>
/**
 * "What decided it" (design system: one surface-highlight card, "show, then say"): the eyebrow,
 * the one-line finding, up to three UsualMeters as evidence, the usual key, and the "Next match"
 * fix. Replaces WinPredictionStats. All copy lives in utils/decidingStat.js.
 */
import { computed } from 'vue'
import BaseIcon from '../base/BaseIcon.vue'
import BaseUsualMeter from '../base/BaseUsualMeter.vue'
import { buildFinding, buildFix, buildUsualNote, buildMeterProps } from '../../utils/decidingStat'

const props = defineProps({
  /** DecidingStat from the match-details response */
  decidingStat: {
    type: Object,
    required: true
  },
  /** The match's role (raw backend value, e.g. "JUNGLE"), for the usual note and fix variants */
  role: {
    type: String,
    required: true
  }
})

const finding = computed(() => buildFinding(props.decidingStat))
const meters = computed(() => (props.decidingStat.meters ?? []).map(buildMeterProps))
const usualNote = computed(() => buildUsualNote(props.decidingStat, props.role))
const fix = computed(() => buildFix(props.decidingStat.fix, props.role))
</script>

<style scoped>
.deciding-stat-card {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.deciding-stat-card__finding {
  margin: 0;
  font-family: var(--font-display);
  font-size: 1.75rem;
  font-weight: 600;
  line-height: 1.25;
  color: var(--color-text);
}

.deciding-stat-card__meters {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 1.25rem;
}

@media (max-width: 899px) {
  .deciding-stat-card__meters {
    grid-template-columns: minmax(0, 1fr);
  }
}

.deciding-stat-card__note {
  margin: 0;
  font-size: 0.8125rem;
  color: var(--color-text-secondary);
}

.deciding-stat-card__fix {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 1rem 1.25rem;
  border-radius: 1rem;
  background: var(--color-surface);
}

.deciding-stat-card__fix p {
  margin: 0;
  font-size: 0.9375rem;
  line-height: 1.5;
  color: var(--color-text);
}

.deciding-stat-card__fix-icon {
  flex-shrink: 0;
  margin-top: 0.125rem;
  color: var(--color-text-secondary);
}
</style>
