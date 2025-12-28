<template>
  <div class="team-role-container">
    <div v-if="loading" class="role-loading">Loading role composition data…</div>
    <div v-else-if="error" class="role-error">{{ error }}</div>
    <div v-else-if="!hasData" class="role-empty">No role composition data available.</div>

    <ChartCard v-else title="🎯 Team Role Distribution">
      <div class="role-content">
        <!-- Player Role Bars -->
        <div v-for="player in compositionData.players" :key="player.playerName" class="player-section">
          <div class="player-header">
            <span class="player-name">{{ getShortName(player.playerName) }}</span>
            <span class="primary-role">{{ player.primaryRole }}</span>
          </div>
          <div class="role-bars">
            <div 
              v-for="role in player.roles" 
              :key="role.position"
              class="role-bar"
              :style="{ width: role.percentage + '%', backgroundColor: getRoleColor(role.position) }"
              :title="`${role.position}: ${role.percentage}% (${role.gamesPlayed} games, ${role.winRate}% WR)`"
            >
              <span v-if="role.percentage >= 12" class="role-label">{{ role.position }}</span>
            </div>
          </div>
        </div>

        <!-- Role Conflicts -->
        <div v-if="compositionData.roleConflicts?.length" class="conflicts-section">
          <div class="conflicts-title">⚠️ Role Conflicts</div>
          <div v-for="conflict in compositionData.roleConflicts" :key="conflict.role" class="conflict-item">
            <span class="conflict-role">{{ conflict.role }}:</span>
            <span class="conflict-players">{{ conflict.players.map(p => getShortName(p)).join(', ') }}</span>
          </div>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getTeamComposition from '@/assets/getTeamComposition.js';

const props = defineProps({
  userId: { type: [String, Number], required: true },
});

const loading = ref(false);
const error = ref(null);
const compositionData = ref(null);

const hasData = computed(() => compositionData.value?.players?.length > 0);

const roleColors = {
  Top: '#7c3aed', Jungle: '#219c4e', Mid: '#f59e0b', ADC: '#ec4899', Support: '#06b6d4', Unknown: '#9ca3af'
};

function getRoleColor(position) { return roleColors[position] || roleColors.Unknown; }
function getShortName(fullName) { return fullName?.split('#')[0] || fullName; }

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    compositionData.value = await getTeamComposition(props.userId);
  } catch (e) {
    console.error('Error loading team composition:', e);
    error.value = e?.message || 'Failed to load role composition.';
    compositionData.value = null;
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.team-role-container { width: 100%; max-width: 100%; height: 100%; }
.team-role-container :deep(.chart-card) { max-width: 100%; height: 100%; min-height: 280px; }

.role-loading, .role-error, .role-empty { padding: 2rem; text-align: center; color: var(--color-text-muted); }
.role-error { color: var(--color-danger); }

.role-content { display: flex; flex-direction: column; gap: 1rem; padding: 0.5rem 0; padding-top: 1.5rem; }

.player-section { display: flex; flex-direction: column; gap: 0.3rem; }
.player-header { display: flex; justify-content: space-between; align-items: center; }
.player-name { font-size: 0.85rem; font-weight: 600; color: var(--color-text); }
.primary-role { font-size: 0.75rem; color: var(--color-text-muted); }

.role-bars {
  display: flex; width: 100%; height: 24px;
  background: var(--color-bg-elev); border-radius: 4px; overflow: hidden;
}

.role-bar {
  height: 100%; display: flex; align-items: center; justify-content: center;
  transition: opacity 0.2s ease; cursor: pointer;
}
.role-bar:hover { opacity: 0.8; }

.role-label { font-size: 0.65rem; font-weight: 600; color: white; text-shadow: 0 1px 2px rgba(0,0,0,0.5); }

.conflicts-section { margin-top: 0.5rem; padding-top: 0.5rem; border-top: 1px solid var(--color-border); }
.conflicts-title { font-size: 0.8rem; font-weight: 600; color: var(--color-warning, #f59e0b); margin-bottom: 0.3rem; }
.conflict-item { font-size: 0.75rem; color: var(--color-text-muted); }
.conflict-role { font-weight: 500; margin-right: 0.3rem; }
</style>

