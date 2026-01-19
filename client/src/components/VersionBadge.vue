<template>
  <div class="version-badge" aria-label="App Version">
    <span class="version-label">Mongoose.gg</span>
    <span class="version-sep">•</span>
    <span class="version-value">v{{ version }}</span>
  </div>
</template>

<script setup>
import pkg from '../../package.json'

const version = pkg.version || '0.0.0'
</script>

<style scoped>
.version-badge {
  position: fixed;
  left: 16px;
  bottom: 16px;
  padding: 6px 10px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: rgba(0, 0, 0, 0.6);
  backdrop-filter: blur(8px);
  color: var(--color-text-secondary);
  font-size: var(--font-size-xs);
  letter-spacing: var(--letter-spacing);
  z-index: 1000;
}

.version-label {
  color: var(--color-text);
  font-weight: var(--font-weight-medium);
}

.version-sep {
  margin: 0 6px;
  opacity: 0.7;
}

.version-value {
  color: var(--color-primary);
  font-weight: var(--font-weight-semibold);
}
</style>
