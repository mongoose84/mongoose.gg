<template>
  <div class="team-improvement-container">
    <div v-if="loading" class="summary-loading">Loading team insights…</div>
    <div v-else-if="error" class="summary-error">{{ error }}</div>
    <div v-else-if="!hasData" class="summary-empty">No team insights available.</div>

    <ChartCard v-else title="📌 Team Improvement Insights">
      <div class="summary-content">
        <!-- Insights List -->
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

        <!-- Strengths & Weaknesses -->
        <div class="strengths-weaknesses">
          <div v-if="summaryData.strengths?.length" class="section strengths">
            <div class="section-title">💪 Strengths</div>
            <ul>
              <li v-for="(s, i) in summaryData.strengths" :key="i">{{ s }}</li>
            </ul>
          </div>
          <div v-if="summaryData.weaknesses?.length" class="section weaknesses">
            <div class="section-title">🎯 Areas to Improve</div>
            <ul>
              <li v-for="(w, i) in summaryData.weaknesses" :key="i">{{ w }}</li>
            </ul>
          </div>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getTeamImprovementSummary from '@/assets/getTeamImprovementSummary.js';

const props = defineProps({
  userId: { type: [String, Number], required: true },
});

const loading = ref(false);
const error = ref(null);
const summaryData = ref(null);

const hasData = computed(() => summaryData.value?.insights?.length > 0);

function getInsightIcon(type) {
  const icons = {
    positive: '✓', negative: '⚠', info: 'ℹ', synergy: '🤝', duration: '⏱', role: '🎯'
  };
  return icons[type] || '•';
}

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    summaryData.value = await getTeamImprovementSummary(props.userId);
  } catch (e) {
    console.error('Error loading team improvement summary:', e);
    error.value = e?.message || 'Failed to load team insights.';
    summaryData.value = null;
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.team-improvement-container { width: 100%; max-width: 100%; height: 100%; }
.team-improvement-container :deep(.chart-card) { max-width: 100%; height: 100%; min-height: 300px; }

.summary-loading, .summary-error, .summary-empty { padding: 2rem; text-align: center; color: var(--color-text-muted); }
.summary-error { color: var(--color-danger); }

.summary-content { padding: 0.5rem 0; padding-top: 1.5rem; display: flex; flex-direction: column; gap: 1rem; }

.insights-list { display: flex; flex-direction: column; gap: 0.5rem; }

.insight-item {
  display: flex; align-items: flex-start; gap: 0.6rem;
  padding: 0.6rem; background: var(--color-bg-elev); border-radius: 6px;
  border-left: 3px solid var(--color-border);
}

.insight-item.positive { border-left-color: var(--color-success); }
.insight-item.negative { border-left-color: var(--color-danger); }
.insight-item.synergy { border-left-color: var(--color-primary); }
.insight-item.duration { border-left-color: #f59e0b; }
.insight-item.role { border-left-color: #06b6d4; }

.insight-icon { font-size: 0.9rem; flex-shrink: 0; }
.insight-text { font-size: 0.8rem; line-height: 1.4; color: var(--color-text); }

.strengths-weaknesses { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }

.section { padding: 0.5rem; background: var(--color-bg-elev); border-radius: 6px; }
.section-title { font-size: 0.8rem; font-weight: 600; margin-bottom: 0.3rem; }
.section ul { margin: 0; padding-left: 1.2rem; font-size: 0.75rem; color: var(--color-text-muted); }
.section li { margin-bottom: 0.2rem; }

@media (max-width: 600px) {
  .strengths-weaknesses { grid-template-columns: 1fr; }
}
</style>

