<template>
  <div class="match-duration-container">
    <div v-if="loading" class="duration-loading">Loading match duration data…</div>
    <div v-else-if="error" class="duration-error">{{ error }}</div>
    <div v-else-if="!hasData" class="duration-empty">No match duration data available.</div>

    <ChartCard v-else title="Match Duration Performance">
      <div class="duration-content">
        <!-- Grouped bar chart for each duration bucket -->
        <div class="duration-chart">
          <div 
            v-for="bucket in commonBuckets" 
            :key="bucket.range"
            class="bucket-group"
          >
            <div class="bucket-label">{{ bucket.range }} min</div>
            <div class="bars-container">
              <div 
                v-for="gamer in durationData.gamers" 
                :key="gamer.gamerName"
                class="bar-wrapper"
              >
                <div 
                  class="bar"
                  :style="{ 
                    width: getBarWidth(getBucketWinrate(gamer, bucket.range)) + '%',
                    backgroundColor: getGamerColor(gamer.gamerName),
                    opacity: getBucketGames(gamer, bucket.range) > 0 ? 1 : 0.2
                  }"
                  :title="`${gamer.serverName}: ${getBucketWinrate(gamer, bucket.range).toFixed(1)}% (${getBucketGames(gamer, bucket.range)} games)`"
                >
                  <span v-if="getBucketGames(gamer, bucket.range) > 0" class="bar-value">
                    {{ getBucketWinrate(gamer, bucket.range).toFixed(0) }}%
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Legend -->
        <div class="legend">
          <div v-for="gamer in durationData.gamers" :key="gamer.gamerName" class="legend-item">
            <span class="legend-color" :style="{ backgroundColor: getGamerColor(gamer.gamerName) }"></span>
            <span class="legend-label">{{ gamer.gamerName }}</span>
          </div>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from '@/components/shared/ChartCard.vue';
import { getMatchDuration } from '@/api/solo.js';
import { sortGamerNames, getGamerColor as getGamerColorUtil } from '@/composables/useGamerColors.js';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
});

const loading = ref(true);
const error = ref(null);
const durationData = ref(null);

const hasData = computed(() => {
  return durationData.value?.gamers?.length > 0;
});

// Get all unique duration buckets across all gamers
const commonBuckets = computed(() => {
  if (!hasData.value) return [];
  
  const bucketSet = new Set();
  durationData.value.gamers.forEach(gamer => {
    gamer.buckets.forEach(bucket => {
      bucketSet.add(bucket.durationRange);
    });
  });
  
  // Sort buckets by min minutes
  const buckets = Array.from(bucketSet).map(range => {
    const [min] = range.split('–').map(Number);
    return { range, min };
  }).sort((a, b) => a.min - b.min);
  
  // Limit to most common buckets (e.g., 20-45 minutes)
  return buckets.filter(b => b.min >= 20 && b.min < 45);
});

// Get winrate for a specific gamer and bucket
function getBucketWinrate(gamer, bucketRange) {
  const bucket = gamer.buckets.find(b => b.durationRange === bucketRange);
  return bucket?.winrate ?? 0;
}

// Get games played for a specific gamer and bucket
function getBucketGames(gamer, bucketRange) {
  const bucket = gamer.buckets.find(b => b.durationRange === bucketRange);
  return bucket?.gamesPlayed ?? 0;
}

// Calculate bar width (0-100%)
function getBarWidth(winrate) {
  return Math.min(100, Math.max(0, winrate));
}

// Get sorted gamer names for consistent color mapping
const sortedGamerNames = computed(() => {
  const names = durationData.value?.gamers?.map(g => g.gamerName) || [];
  return sortGamerNames(names);
});

// Get color for each gamer (alphabetically sorted)
function getGamerColor(gamerName) {
  return getGamerColorUtil(gamerName, sortedGamerNames.value);
}

async function load() {
  loading.value = true;
  error.value = null;
  
  try {
    const data = await getMatchDuration(props.userId);
    durationData.value = data;
  } catch (err) {
    console.error('Failed to load match duration data:', err);
    error.value = 'Failed to load match duration data';
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.match-duration-container {
  width: 100%;
  max-width: 100%;
  height: 100%;
}

/* Override ChartCard max-width for match duration */
.match-duration-container :deep(.chart-card) {
  max-width: 100%;
  height: 100%;
  min-height: 250px;
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.duration-loading,
.duration-error,
.duration-empty {
  padding: 2rem;
  text-align: center;
  color: var(--color-text-muted);
}

.duration-error {
  color: var(--color-danger);
}

.duration-content {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding: 0.5rem 0;
  padding-top: 1.1rem;
}

.duration-chart {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.bucket-group {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.bucket-label {
  min-width: 70px;
  font-size: 0.75rem;
  font-weight: 600;
  color: var(--color-text);
  text-align: right;
}

.bars-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.bar-wrapper {
  width: 100%;
}

.bar {
  height: 18px;
  border-radius: 3px;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  padding-right: 0.4rem;
  transition: all 0.2s ease;
  min-width: 20px;
}

.bar:hover {
  opacity: 0.8 !important;
  transform: scaleY(1.1);
}

.bar-value {
  font-size: 0.7rem;
  font-weight: 600;
  color: white;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.3);
}

.legend {
  display: flex;
  gap: 1rem;
  justify-content: center;
  padding-top: 0.5rem;
  border-top: 1px solid var(--color-border);
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
</style>

