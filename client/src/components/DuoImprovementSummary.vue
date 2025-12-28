<template>
  <div class="duo-improvement-summary-container">
    <div v-if="loading" class="summary-loading">Loading improvement insights…</div>
    <div v-else-if="error" class="summary-error">{{ error }}</div>
    <div v-else-if="!hasData" class="summary-empty">No improvement insights available.</div>

    <ChartCard v-else title="📌 Duo Improvement Insights">
      <div class="summary-content">
        <div class="insights-list">
          <div 
            v-for="(insight, index) in summaryData.insights" 
            :key="index"
            class="insight-item"
            :class="insight.type"
          >
            <span class="insight-icon">{{ getInsightIcon(insight.type) }}</span>
            <span class="insight-text">{{ insight.text }}</span>
          </div>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getDuoImprovementSummary from '@/assets/getDuoImprovementSummary.js';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
});

const loading = ref(false);
const error = ref(null);
const summaryData = ref(null);

const hasData = computed(() => summaryData.value?.insights?.length > 0);

function getInsightIcon(type) {
  const icons = {
    positive: '✓',
    negative: '⚠',
    neutral: '•',
    champion: '🏆',
    duration: '⏱',
    role: '🎯'
  };
  return icons[type] || '•';
}

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    summaryData.value = await getDuoImprovementSummary(props.userId);
  } catch (e) {
    console.error('Error loading duo improvement summary:', e);
    error.value = e?.message || 'Failed to load improvement insights.';
    summaryData.value = null;
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.duo-improvement-summary-container {
  width: 100%;
  max-width: 100%;
  height: 100%;
}

.duo-improvement-summary-container :deep(.chart-card) {
  max-width: 100%;
  height: 100%;
  min-height: 200px;
}

.summary-loading,
.summary-error,
.summary-empty {
  padding: 2rem;
  text-align: center;
  color: var(--color-text-muted);
}

.summary-error {
  color: var(--color-danger);
}

.summary-content {
  padding: 0.5rem 0;
  padding-top: 1.5rem;
}

.insights-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.insight-item {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 0.75rem;
  background: var(--color-bg-elev);
  border-radius: 6px;
  border-left: 3px solid var(--color-border);
  transition: all 0.2s ease;
}

.insight-item:hover {
  background: var(--color-bg);
  transform: translateX(2px);
}

.insight-item.positive {
  border-left-color: var(--color-success);
}

.insight-item.negative {
  border-left-color: var(--color-danger);
}

.insight-item.champion {
  border-left-color: var(--color-primary);
}

.insight-item.duration {
  border-left-color: #f59e0b;
}

.insight-item.role {
  border-left-color: #06b6d4;
}

.insight-icon {
  font-size: 1rem;
  line-height: 1.4;
  flex-shrink: 0;
}

.insight-text {
  font-size: 0.85rem;
  line-height: 1.4;
  color: var(--color-text);
}
</style>

