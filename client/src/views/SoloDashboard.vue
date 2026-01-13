<template>
  <section class="solo-dashboard">
    <header class="dashboard-header">
      <h1>Solo Dashboard</h1>
      <div class="filters">
        <div class="filter-group">
          <label for="queue-filter">Queue</label>
          <select id="queue-filter" v-model="queueFilter" aria-label="Filter matches by queue type">
            <option value="all">All Queues</option>
            <option value="ranked_solo">Ranked Solo/Duo</option>
            <option value="ranked_flex">Ranked Flex</option>
            <option value="normal">Normal</option>
            <option value="aram">ARAM</option>
          </select>
        </div>
        <div class="filter-group">
          <label for="time-range-filter">Time Range</label>
          <select id="time-range-filter" v-model="timeRange" aria-label="Filter matches by time range">
            <option value="1w">Last Week</option>
            <option value="1m">Last Month</option>
            <option value="3m">Last 3 Months</option>
            <option value="6m">Last 6 Months</option>
          </select>
        </div>
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
const queueFilter = ref('all')
const timeRange = ref('3m')
</script>

<style scoped>
.solo-dashboard {
  padding: var(--spacing-lg);
}
.dashboard-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--spacing-lg);
}
.filters {
  display: flex;
  gap: var(--spacing-md);
}
.filter-group {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}
.filter-group label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
}
.filter-group select {
  padding: var(--spacing-sm) var(--spacing-md);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text);
  font-size: var(--font-size-sm);
  cursor: pointer;
  transition: border-color 0.2s ease;
}
.filter-group select:hover {
  border-color: var(--color-primary);
}
.filter-group select:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px rgba(147, 51, 234, 0.1);
}
.sections {
  display: grid;
  grid-template-columns: 1fr;
  gap: var(--spacing-lg);
}
.placeholder-card {
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-lg);
  background: var(--color-surface);
}
.placeholder-card h2 {
  margin: 0 0 var(--spacing-sm) 0;
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
}
.placeholder-card p {
  margin: 0;
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}
</style>
