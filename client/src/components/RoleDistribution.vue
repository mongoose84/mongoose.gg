<template>
  <div class="role-distribution-container">
    <div v-if="loading" class="role-loading">Loading role data…</div>
    <div v-else-if="error" class="role-error">{{ error }}</div>
    <div v-else-if="!hasData" class="role-empty">No role data available.</div>

    <ChartCard v-else title="Role & Lane Consistency">
      <div class="role-content">
        <!-- Pie chart for each server -->
        <div v-for="gamer in roleData.gamers" :key="gamer.gamerName" class="pie-chart-wrapper">
          <svg :viewBox="`0 0 ${pieSize} ${pieSize}`" class="pie-chart" :aria-label="`Role distribution for ${gamer.serverName}`">
            <!-- Pie slices -->
            <g v-for="(slice, index) in getPieSlices(getTop3Roles(gamer.roles))" :key="index">
              <path
                :d="slice.path"
                :fill="getRoleColor(slice.position)"
                :stroke="'var(--color-bg-elev)'"
                stroke-width="2"
                class="pie-slice"
              />
            </g>
          </svg>

          <!-- Legend -->
          <div class="legend">
            <div v-for="role in getTop3Roles(gamer.roles)" :key="role.position" class="legend-item">
              <span class="legend-color" :style="{ backgroundColor: getRoleColor(role.position) }"></span>
              <span class="legend-label">{{ formatPosition(role.position) }}: {{ role.percentage.toFixed(1) }}%</span>
            </div>
          </div>

          <!-- Gamer name at bottom -->
          <div class="gamer-name">{{ gamer.gamerName }}</div>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getRoleDistribution from '@/assets/getRoleDistribution.js';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
});

const loading = ref(false);
const error = ref(null);
const roleData = ref(null);

const pieSize = 160;
const pieCenter = pieSize / 2;
const pieRadius = 60;

const hasData = computed(() => roleData.value?.gamers?.length > 0);

// Role color mapping - aligned with app color scheme
const roleColors = {
  TOP: '#7c3aed',      // Purple (var(--color-primary))
  JUNGLE: '#219c4e',   // Green (var(--color-success))
  MIDDLE: '#f59e0b',   // Amber
  BOTTOM: '#ec4899',   // Pink
  UTILITY: '#06b6d4',  // Cyan
  UNKNOWN: '#d1c4e9'   // Muted text color
};

function getRoleColor(position) {
  return roleColors[position] || roleColors.UNKNOWN;
}

function formatPosition(position) {
  const positionMap = {
    TOP: 'Top',
    JUNGLE: 'Jungle',
    MIDDLE: 'Mid',
    BOTTOM: 'ADC',
    UTILITY: 'Support',
    UNKNOWN: 'Other'
  };
  return positionMap[position] || position;
}

// Get top 3 roles by percentage
function getTop3Roles(roles) {
  if (!roles || roles.length === 0) return [];
  // Roles are already sorted by gamesPlayed DESC from backend
  // Take top 3 and recalculate percentages to sum to 100%
  const top3 = roles.slice(0, 3);
  const totalGames = top3.reduce((sum, role) => sum + role.gamesPlayed, 0);

  return top3.map(role => ({
    ...role,
    percentage: totalGames > 0 ? (role.gamesPlayed / totalGames) * 100 : 0
  }));
}

// Calculate pie slice paths
function getPieSlices(roles) {
  if (!roles || roles.length === 0) return [];
  
  const slices = [];
  let currentAngle = 0;
  
  roles.forEach(role => {
    const sliceAngle = (role.percentage / 100) * 2 * Math.PI;
    const startAngle = currentAngle;
    const endAngle = currentAngle + sliceAngle;
    
    // Calculate start and end points
    const x1 = pieCenter + pieRadius * Math.cos(startAngle);
    const y1 = pieCenter + pieRadius * Math.sin(startAngle);
    const x2 = pieCenter + pieRadius * Math.cos(endAngle);
    const y2 = pieCenter + pieRadius * Math.sin(endAngle);
    
    // Large arc flag: 1 if angle > 180 degrees
    const largeArcFlag = sliceAngle > Math.PI ? 1 : 0;
    
    // Create SVG path
    const path = [
      `M ${pieCenter} ${pieCenter}`,  // Move to center
      `L ${x1} ${y1}`,                 // Line to start point
      `A ${pieRadius} ${pieRadius} 0 ${largeArcFlag} 1 ${x2} ${y2}`, // Arc to end point
      'Z'                              // Close path
    ].join(' ');
    
    slices.push({
      position: role.position,
      path,
      percentage: role.percentage
    });
    
    currentAngle = endAngle;
  });
  
  return slices;
}

// Load data
async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    roleData.value = await getRoleDistribution(props.userId);
  } catch (e) {
    console.error('Error loading role distribution data:', e);
    error.value = e?.message || 'Failed to load role distribution data.';
    roleData.value = null;
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.role-distribution-container {
  width: 100%;
  max-width: 100%;
  height: 100%;
}

/* Override ChartCard max-width for role distribution */
.role-distribution-container :deep(.chart-card) {
  max-width: 100%;
  height: 100%;
  min-height: 250px;
}

.role-loading,
.role-error,
.role-empty {
  text-align: center;
  padding: 2rem;
  color: var(--color-text-muted);
}

.role-error {
  color: var(--color-danger);
}

.role-content {
  display: flex;
  gap: 1rem;
  align-items: flex-start;
  justify-content: space-around;
  padding: 0.5rem 0;
  flex-wrap: wrap;
}

.pie-chart-wrapper {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  flex: 1;
  min-width: 120px;
}

.server-title {
  margin: 0;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-text);
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.pie-chart {
  width: 100%;
  max-width: 160px;
  height: auto;
  display: block;
}

.pie-slice {
  transition: opacity 0.2s ease;
  cursor: pointer;
}

.pie-slice:hover {
  opacity: 0.8;
}

.pie-center-label {
  font-size: 0.75rem;
  font-weight: 600;
  fill: var(--color-text);
  pointer-events: none;
}

.legend {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  width: 100%;
  max-width: 160px;
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
  white-space: nowrap;
}

.gamer-name {
  margin-top: 0.5rem;
  font-size: 0.75rem;
  color: var(--color-text-muted);
  text-align: center;
  font-weight: 500;
}

/* Responsive: stack vertically on smaller screens */
@media (max-width: 768px) {
  .role-content {
    flex-direction: column;
    align-items: center;
  }

  .pie-chart-wrapper {
    width: 100%;
  }
}
</style>
