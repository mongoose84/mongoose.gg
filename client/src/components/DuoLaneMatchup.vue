<template>
  <div class="duo-lane-matchup-container">
    <div v-if="loading" class="matchup-loading">Loading lane matchup data…</div>
    <div v-else-if="error" class="matchup-error">{{ error }}</div>
    <div v-else-if="!hasData" class="matchup-empty">No lane matchup data available.</div>

    <ChartCard v-else title="Lane Matchup Performance">
      <div class="matchup-content">
        <div class="matchup-table">
          <div class="table-header">
            <div class="col-lane">Lane Combo</div>
            <div class="col-winrate">Win Rate</div>
            <div class="col-games">Games</div>
          </div>
          <div 
            v-for="combo in sortedCombos" 
            :key="combo.laneCombo"
            class="table-row"
            :class="getWinRateClass(combo.winrate)"
          >
            <div class="col-lane">{{ combo.laneCombo }}</div>
            <div class="col-winrate">
              <div class="winrate-bar-container">
                <div 
                  class="winrate-bar" 
                  :style="{ width: combo.winrate + '%' }"
                  :class="getWinRateClass(combo.winrate)"
                ></div>
                <span class="winrate-text">{{ combo.winrate.toFixed(0) }}%</span>
              </div>
            </div>
            <div class="col-games">{{ combo.gamesPlayed }}</div>
          </div>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getDuoLaneMatchup from '@/assets/getDuoLaneMatchup.js';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
});

const loading = ref(false);
const error = ref(null);
const matchupData = ref(null);

const hasData = computed(() => matchupData.value?.laneCombos?.length > 0);

const sortedCombos = computed(() => {
  if (!hasData.value) return [];
  return [...matchupData.value.laneCombos]
    .sort((a, b) => b.winrate - a.winrate)
    .slice(0, 8); // Show top 8 combinations
});

function getWinRateClass(winrate) {
  if (winrate >= 55) return 'wr-good';
  if (winrate >= 50) return 'wr-neutral';
  return 'wr-bad';
}

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    matchupData.value = await getDuoLaneMatchup(props.userId);
  } catch (e) {
    console.error('Error loading duo lane matchup data:', e);
    error.value = e?.message || 'Failed to load lane matchup data.';
    matchupData.value = null;
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.duo-lane-matchup-container {
  width: 100%;
  max-width: 100%;
  height: 100%;
}

.duo-lane-matchup-container :deep(.chart-card) {
  max-width: 100%;
  height: 100%;
  min-height: 280px;
}

.matchup-loading,
.matchup-error,
.matchup-empty {
  padding: 2rem;
  text-align: center;
  color: var(--color-text-muted);
}

.matchup-error {
  color: var(--color-danger);
}

.matchup-content {
  padding: 0.5rem 0;
  padding-top: 1.5rem;
}

.matchup-table {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.table-header,
.table-row {
  display: grid;
  grid-template-columns: 2fr 3fr 1fr;
  gap: 0.75rem;
  align-items: center;
  padding: 0.5rem 0.75rem;
}

.table-header {
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--color-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.03em;
  border-bottom: 1px solid var(--color-border);
}

.table-row {
  font-size: 0.85rem;
  border-radius: 4px;
  transition: background-color 0.2s ease;
}

.table-row:hover {
  background-color: var(--color-bg-elev);
}

.col-lane {
  font-weight: 500;
  color: var(--color-text);
}

.col-winrate {
  display: flex;
  align-items: center;
}

.col-games {
  text-align: right;
  color: var(--color-text-muted);
  font-size: 0.8rem;
}

.winrate-bar-container {
  position: relative;
  width: 100%;
  height: 24px;
  background: var(--color-bg-elev);
  border-radius: 4px;
  overflow: hidden;
}

.winrate-bar {
  height: 100%;
  transition: width 0.3s ease;
  border-radius: 4px;
}

.winrate-bar.wr-good {
  background: var(--color-success);
}

.winrate-bar.wr-neutral {
  background: var(--color-text-muted);
}

.winrate-bar.wr-bad {
  background: var(--color-danger);
}

.winrate-text {
  position: absolute;
  right: 0.5rem;
  top: 50%;
  transform: translateY(-50%);
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--color-text);
}
</style>

