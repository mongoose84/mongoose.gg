<template>
  <div class="chart-card">
    <h4>{{ title }}</h4>
    <slot></slot>
  </div>
</template>

<script setup>
defineProps({
  title: {
    type: String,
    required: true,
  },
});
</script>

<style scoped>
/* Chart card border and background styling */
.chart-card {
  background: var(--color-bg-elev);
  border: 1px solid var(--color-border);
  border-radius: 12px;
  padding: 1.2rem 1.2rem 0.7rem 1.2rem;
  box-sizing: border-box;
  box-shadow: 0 2px 8px 0 rgba(44, 11, 58, 0.08);
  min-width: 0;
  width: 100%;
  max-width: 100%; /* Allow full width within grid cell */
  min-height: 320px; /* Minimum height to accommodate charts */
  height: auto; /* Allow height to grow with content */
  display: flex;
  flex-direction: column;
  align-items: stretch;
  justify-content: flex-start;
  position: relative;
  flex: 1 1 auto; /* Allow flexible sizing */
}

.chart-card h4 {
  margin: 0;
  font-size: 1.05rem;
  color: var(--color-text);
  font-weight: 600;
  text-align: left;
  align-self: flex-start;
  position: absolute;
  top: 0.7rem;
  left: 1.2rem;
  z-index: 2;
  background: transparent;
}

/* Mobile stacking */
@media (max-width: 1200px) {
  .chart-card {
    min-width: 0;
    max-width: 100%;
    width: 100%;
  }
}
</style>

