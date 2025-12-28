<template>
  <div class="team-role-pair-container">
    <div v-if="loading" class="role-loading">Loading role pair analysis…</div>
    <div v-else-if="error" class="role-error">{{ error }}</div>
    <div v-else-if="!hasData" class="role-empty">
      Not enough data for role pair analysis.
      <span class="requirement-hint">(Minimum: 3 games with the same role pair)</span>
    </div>

    <ChartCard v-else title="🎯 Role Pair Effectiveness">
      <div class="role-content">
        <!-- Best/Worst Pairs -->
        <div class="pair-highlights">
          <div v-if="roleData.bestPair" class="highlight best">
            <span class="highlight-label">Best Pair:</span>
            <span class="highlight-roles">{{ roleData.bestPair.role1 }} + {{ roleData.bestPair.role2 }}</span>
            <span class="highlight-wr good">{{ roleData.bestPair.winRate }}%</span>
          </div>
          <div v-if="roleData.worstPair" class="highlight worst">
            <span class="highlight-label">Needs Work:</span>
            <span class="highlight-roles">{{ roleData.worstPair.role1 }} + {{ roleData.worstPair.role2 }}</span>
            <span class="highlight-wr bad">{{ roleData.worstPair.winRate }}%</span>
          </div>
        </div>

        <!-- Heatmap Grid -->
        <div class="heatmap-container">
          <svg :viewBox="`0 0 ${heatmapSize} ${heatmapSize}`" class="heatmap">
            <!-- Column headers -->
            <text v-for="(role, i) in roles" :key="'col-' + i"
              :x="cellSize + cellSize * i + cellSize / 2" :y="15" text-anchor="middle" class="header-label">
              {{ role.short }}
            </text>
            <!-- Row headers -->
            <text v-for="(role, i) in roles" :key="'row-' + i"
              :x="cellSize - 5" :y="cellSize + cellSize * i + cellSize / 2 + 4" text-anchor="end" class="header-label">
              {{ role.short }}
            </text>
            <!-- Cells -->
            <g v-for="(cell, i) in heatmapCells" :key="'cell-' + i">
              <rect :x="cell.x" :y="cell.y" :width="cellSize - 4" :height="cellSize - 4" rx="4" :class="cell.class" />
              <text v-if="cell.hasData" :x="cell.x + cellSize / 2 - 2" :y="cell.y + cellSize / 2" 
                text-anchor="middle" dominant-baseline="middle" class="cell-value">
                {{ cell.winRate }}%
              </text>
            </g>
          </svg>
        </div>

        <!-- Legend -->
        <div class="legend">
          <span class="legend-item"><span class="swatch low"></span>&lt;45%</span>
          <span class="legend-item"><span class="swatch mid"></span>45-55%</span>
          <span class="legend-item"><span class="swatch high"></span>&gt;55%</span>
        </div>
      </div>
    </ChartCard>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import ChartCard from './ChartCard.vue';
import getTeamRolePairEffectiveness from '@/assets/getTeamRolePairEffectiveness.js';

const props = defineProps({ userId: { type: [String, Number], required: true } });

const loading = ref(false);
const error = ref(null);
const roleData = ref(null);

const roles = [
  { name: 'Top', short: 'TOP' }, { name: 'Jungle', short: 'JNG' },
  { name: 'Mid', short: 'MID' }, { name: 'Bot', short: 'BOT' }, { name: 'Support', short: 'SUP' }
];
const cellSize = 50;
const heatmapSize = cellSize * 6;

const hasData = computed(() => roleData.value?.rolePairs?.length > 0);

const heatmapCells = computed(() => {
  if (!roleData.value?.rolePairs) return [];
  const pairMap = {};
  roleData.value.rolePairs.forEach(p => {
    const key = `${p.role1}|${p.role2}`;
    const keyRev = `${p.role2}|${p.role1}`;
    pairMap[key] = p;
    pairMap[keyRev] = p;
  });

  const cells = [];
  for (let row = 0; row < roles.length; row++) {
    for (let col = 0; col < roles.length; col++) {
      if (row >= col) continue; // Only upper triangle
      const pair = pairMap[`${roles[row].name}|${roles[col].name}`];
      const hasData = pair && pair.gamesPlayed >= 3;
      let cellClass = 'cell-empty';
      if (hasData) {
        cellClass = pair.winRate >= 55 ? 'cell-high' : pair.winRate >= 45 ? 'cell-mid' : 'cell-low';
      }
      cells.push({
        x: cellSize + col * cellSize + 2, y: cellSize + row * cellSize + 2,
        winRate: hasData ? Math.round(pair.winRate) : '', hasData, class: cellClass
      });
    }
  }
  return cells;
});

async function load() {
  if (!props.userId) return;
  loading.value = true;
  error.value = null;
  try {
    roleData.value = await getTeamRolePairEffectiveness(props.userId);
  } catch (e) {
    error.value = e?.message || 'Failed to load role pair data.';
  } finally {
    loading.value = false;
  }
}

watch(() => props.userId, load);
onMounted(load);
</script>

<style scoped>
.team-role-pair-container { width: 100%; }
.role-loading, .role-error, .role-empty { padding: 2rem; text-align: center; color: var(--color-text-muted); }
.role-error { color: var(--color-danger); }
.role-empty .requirement-hint { display: block; margin-top: 0.5rem; font-size: 0.85rem; opacity: 0.7; }
.role-content { padding: 2rem 0 0.5rem 0; display: flex; flex-direction: column; gap: 1rem; }
.pair-highlights { display: flex; gap: 1rem; flex-wrap: wrap; }
.highlight { display: flex; align-items: center; gap: 0.5rem; padding: 0.5rem 0.75rem; background: var(--color-bg-elev); border-radius: 6px; }
.highlight-label { font-size: 0.85rem; color: var(--color-text-muted); }
.highlight-roles { font-weight: 600; }
.highlight-wr { font-weight: 600; }
.highlight-wr.good { color: var(--color-success); }
.highlight-wr.bad { color: var(--color-danger); }
.heatmap-container { display: flex; justify-content: center; }
.heatmap { width: 100%; max-width: 300px; height: auto; }
.header-label { fill: var(--color-text-muted); font-size: 10px; font-weight: 600; }
.cell-empty { fill: var(--color-bg-elev); }
.cell-low { fill: rgba(239, 68, 68, 0.6); }
.cell-mid { fill: rgba(245, 158, 11, 0.6); }
.cell-high { fill: rgba(34, 197, 94, 0.6); }
.cell-value { fill: var(--color-text); font-size: 11px; font-weight: 600; }
.legend { display: flex; justify-content: center; gap: 1rem; font-size: 0.8rem; }
.legend-item { display: flex; align-items: center; gap: 0.25rem; }
.swatch { width: 12px; height: 12px; border-radius: 2px; }
.swatch.low { background: rgba(239, 68, 68, 0.6); }
.swatch.mid { background: rgba(245, 158, 11, 0.6); }
.swatch.high { background: rgba(34, 197, 94, 0.6); }
</style>

