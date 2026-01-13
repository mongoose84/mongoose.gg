<template>
  <section class="solo-dashboard">
    <header class="dashboard-header">
      <h1>Solo Dashboard</h1>
      <div class="filters">
        <label>
          Queue
          <select v-model="queueFilter">
            <option value="ranked_all">All Ranked</option>
            <option value="ranked_solo">Ranked Solo/Duo</option>
            <option value="ranked_flex">Ranked Flex</option>
            <option value="normal">Normal</option>
            <option value="aram">ARAM</option>
          </select>
        </label>
        <label>
          Time Range
          <select v-model="timeRange">
            <option value="1w">Last Week</option>
            <option value="1m">Last Month</option>
            <option value="3m">Last 3 Months</option>
            <option value="6m">Last 6 Months</option>
          </select>
        </label>
      </div>
    </header>

    <div class="sections">
      <div class="section placeholder-card">
        <h2>Profile Header</h2>
        <p>Profile icon, Riot ID, level, ranks, overall winrate.</p>
      </div>

      <div class="section placeholder-card">
        <h2>Main Champion Card</h2>
        <p>Role tabs with top 3 champions per role.</p>
      </div>

      <div class="section placeholder-card">
        <h2>Winrate Over Time</h2>
        <p>Rolling average line chart (shared component).</p>
      </div>

      <div class="section placeholder-card">
        <h2>LP Over Time</h2>
        <p>Per-game LP changes with rank annotations (ranked only).</p>
      </div>

      <div class="section placeholder-card">
        <h2>Champion Matchups</h2>
        <p>Top 5 champions with expandable opponent details.</p>
      </div>

      <div class="section placeholder-card">
        <h2>Goals Panel</h2>
        <p>Active goals and progress (upgrade CTA for Free).</p>
      </div>
    </div>
  </section>
</template>

<script setup>
import { ref } from 'vue'

// UI-only state for filters (no functionality yet)
const queueFilter = ref('ranked_all')
const timeRange = ref('3m')
</script>

<style scoped>
.solo-dashboard {
  padding: 16px;
}
.dashboard-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16px;
}
.filters {
  display: flex;
  gap: 12px;
}
.sections {
  display: grid;
  grid-template-columns: 1fr;
  gap: 16px;
}
.placeholder-card {
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 8px;
  padding: 16px;
  background: rgba(255,255,255,0.02);
}
.placeholder-card h2 {
  margin: 0 0 8px 0;
}
</style>
