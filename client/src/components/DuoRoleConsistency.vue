<template>
  <div class="duo-role-consistency-container">
    <div v-if="loading" class="role-loading">Loading role consistency data…</div>
    <div v-else-if="error" class="role-error">{{ error }}</div>
    <div v-else-if="!hasData" class="role-empty">No role consistency data available.</div>

    <ChartCard v-else title="Role & Lane Consistency">
      <div class="role-content">
        <!-- Player A Role Distribution -->
        <div v-for="(player, index) in roleData.players" :key="player.playerName" class="player-section">
          <div class="player-name">{{ player.playerName }}</div>
          <div class="role-bars">
            <div 
              v-for="role in player.roles" 
              :key="role.position"
              class="role-bar-wrapper"
              :title="`${formatPosition(role.position)}: ${role.percentage.toFixed(1)}% (${role.gamesPlayed} games)`"
            >
              <div 
                class="role-bar"
                :style="{ 
                  width: role.percentage + '%',
                  backgroundColor: getRoleColor(role.position)
                }"
              >
                <span v-if="role.percentage >= 8" class="role-label">
                  {{ formatPosition(role.position) }} {{ role.percentage.toFixed(0) }}%
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getDuoRoleConsistency from '@/assets/getDuoRoleConsistency.js';

const props = defineProps({
  userId: {
    type: [String, Number],
    required: true,
  },
});

const loading = ref(false);
const error = ref(null);
const roleData = ref(null);

const hasData = computed(() => roleData.value?.players?.length > 0);

// Role color mapping - aligned with app color scheme
const roleColors = {
  TOP: '#7c3aed',      // Purple
  JUNGLE: '#219c4e',   // Green
  MIDDLE: '#f59e0b',   // Amber
  BOTTOM: '#ec4899',   // Pink
  UTILITY: '#06b6d4',  // Cyan
  UNKNOWN: '#9ca3af'   // Gray for unknown
};

function getRoleColor(position) {
  return roleColors[position] || roleColors.UNKNOWN;
}

function formatPosition(position) {
  const map = {
    TOP: 'Top',
    JUNGLE: 'Jungle',
    MIDDLE: 'Mid',
    BOTTOM: 'ADC',
    UTILITY: 'Support',
    UNKNOWN: 'Unknown'
  };
  return map[position] || position;
}

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    roleData.value = await getDuoRoleConsistency(props.userId);
  } catch (e) {
    console.error('Error loading duo role consistency data:', e);
    error.value = e?.message || 'Failed to load role consistency data.';
    roleData.value = null;
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.duo-role-consistency-container {
  width: 100%;
  max-width: 100%;
  height: 100%;
}

.duo-role-consistency-container :deep(.chart-card) {
  max-width: 100%;
  height: 100%;
  min-height: 200px;
}

.role-loading,
.role-error,
.role-empty {
  padding: 2rem;
  text-align: center;
  color: var(--color-text-muted);
}

.role-error {
  color: var(--color-danger);
}

.role-content {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
  padding: 0.5rem 0;
  padding-top: 1.5rem;
}

.player-section {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.player-name {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-text);
}

.role-bars {
  display: flex;
  width: 100%;
  height: 32px;
  background: var(--color-bg-elev);
  border-radius: 4px;
  overflow: hidden;
}

.role-bar-wrapper {
  display: flex;
}

.role-bar {
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: opacity 0.2s ease;
  cursor: pointer;
}

.role-bar:hover {
  opacity: 0.8;
}

.role-label {
  font-size: 0.7rem;
  font-weight: 600;
  color: white;
  text-shadow: 0 1px 2px rgba(0, 0, 0, 0.5);
  white-space: nowrap;
}
</style>

