<template>
  <div class="death-efficiency-container">
    <div v-if="loading" class="death-loading">Loading death efficiency data…</div>
    <div v-else-if="error" class="death-error">{{ error }}</div>
    <div v-else-if="!hasData" class="death-empty">No death efficiency data available.</div>

    <ChartCard v-else title="Death & Efficiency Analysis">
      <div class="death-content">
        <div class="charts-wrapper">
          <!-- Average Deaths Chart -->
          <div class="chart-section">
            <h5 class="chart-title">Avg Deaths</h5>
            <div class="bar-chart">
              <div
                v-for="record in sortedAvgDeaths"
                :key="record.gamerName"
                class="bar-row"
              >
                <div class="bar-container">
                  <div
                    class="bar"
                    :style="{
                      width: getBarWidth(record.value, maxDeaths) + '%',
                      backgroundColor: getGamerColor(record.gamerName)
                    }"
                  >
                    <span class="bar-value">{{ record.value.toFixed(1) }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Time Dead Chart -->
          <div class="chart-section">
            <h5 class="chart-title">Time Dead (sec)</h5>
            <div class="bar-chart">
              <div
                v-for="record in sortedAvgTimeDead"
                :key="record.gamerName"
                class="bar-row"
              >
                <div class="bar-container">
                  <div
                    class="bar"
                    :style="{
                      width: getBarWidth(record.value, maxTimeDead) + '%',
                      backgroundColor: getGamerColor(record.gamerName)
                    }"
                  >
                    <span class="bar-value">{{ record.value.toFixed(0) }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Legend -->
        <div class="legend">
          <div v-for="record in sortedAvgDeaths" :key="record.gamerName" class="legend-item">
            <span class="legend-color" :style="{ backgroundColor: getGamerColor(record.gamerName) }"></span>
            <span class="legend-label">{{ record.gamerName }}</span>
          </div>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from '@/components/shared/ChartCard.vue';
import { getComparison } from '@/api/solo.js';
import { sortGamerNames, getGamerColor as getGamerColorUtil } from '@/composables/useGamerColors.js';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
});

const loading = ref(false);
const error = ref(null);
const deathData = ref(null);

const hasData = computed(() =>
  deathData.value?.avgDeaths?.length > 0 &&
  deathData.value?.avgTimeDeadSeconds?.length > 0
);

// Get unique gamer names sorted alphabetically for consistent color mapping
const gamerNames = computed(() => {
  if (!deathData.value?.avgDeaths?.length) return [];
  const names = deathData.value.avgDeaths.map(r => r.gamerName);
  return sortGamerNames(names);
});

// Sort avgDeaths alphabetically by gamerName
const sortedAvgDeaths = computed(() => {
  if (!deathData.value?.avgDeaths?.length) return [];
  return [...deathData.value.avgDeaths].sort((a, b) => a.gamerName.localeCompare(b.gamerName));
});

// Sort avgTimeDeadSeconds alphabetically by gamerName
const sortedAvgTimeDead = computed(() => {
  if (!deathData.value?.avgTimeDeadSeconds?.length) return [];
  return [...deathData.value.avgTimeDeadSeconds].sort((a, b) => a.gamerName.localeCompare(b.gamerName));
});

// Calculate max values for scaling bars
const maxDeaths = computed(() => {
  if (!deathData.value?.avgDeaths?.length) return 10;
  return Math.max(...deathData.value.avgDeaths.map(r => r.value), 10);
});

const maxTimeDead = computed(() => {
  if (!deathData.value?.avgTimeDeadSeconds?.length) return 100;
  return Math.max(...deathData.value.avgTimeDeadSeconds.map(r => r.value), 100);
});

// Get bar width as percentage
function getBarWidth(value, max) {
  if (max === 0) return 0;
  return Math.min((value / max) * 100, 100);
}

// Get color based on gamer name (alphabetically sorted)
function getGamerColor(gamerName) {
  return getGamerColorUtil(gamerName, gamerNames.value);
}

async function fetchData() {
  if (!props.userId) return;
  
  loading.value = true;
  error.value = null;
  
  try {
    const data = await getComparison(props.userId);
    deathData.value = data;
  } catch (err) {
    console.error('Error fetching death efficiency data:', err);
    error.value = 'Failed to load death efficiency data';
  } finally {
    loading.value = false;
  }
}

// Watch for userId changes
watch(() => props.userId, fetchData, { immediate: false });

onMounted(() => {
  fetchData();
});
</script>

<style scoped>
.death-efficiency-container {
  width: 100%;
  max-width: 100%;
  height: 100%;
}

/* Override ChartCard max-width for death efficiency */
.death-efficiency-container :deep(.chart-card) {
  max-width: 100%;
  height: 100%;
  min-height: 250px;
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.death-loading,
.death-error,
.death-empty {
  padding: 2rem;
  text-align: center;
  color: var(--color-text-muted);
}

.death-error {
  color: var(--color-danger);
}

.death-content {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding: 0.5rem 0;
  align-items: center;
  justify-content: center;
}

.charts-wrapper {
  display: flex;
  gap: 1.5rem;
  align-items: center;
  justify-content: center;
  width: 100%;
}

.chart-section {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.chart-title {
  margin: 0;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-text);
  text-align: center;
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.bar-chart {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.bar-row {
  display: flex;
  align-items: center;
}

.bar-container {
  flex: 1;
  height: 28px;
  background-color: var(--color-bg-elev);
  border-radius: 4px;
  overflow: hidden;
  position: relative;
}

.bar {
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  padding-right: 0.5rem;
  transition: width 0.3s ease;
  border-radius: 4px;
  min-width: 40px;
}

.bar-value {
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--color-text);
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.3);
}

.legend {
  display: flex;
  gap: 1rem;
  justify-content: center;
  padding-top: 0.5rem;
  border-top: 1px solid var(--color-border);
  width: 100%;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.75rem;
}

.legend-color {
  width: 10px;
  height: 10px;
  border-radius: 2px;
  flex-shrink: 0;
}

.legend-label {
  color: var(--color-text);
  font-weight: 500;
}

/* Responsive: stack vertically on smaller screens */
@media (max-width: 768px) {
  .charts-wrapper {
    flex-direction: column;
  }
}
</style>

