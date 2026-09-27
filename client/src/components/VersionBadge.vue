<template>
  <div
    class="fixed left-4 bottom-4 py-1.5 px-3 border border-border rounded-pill bg-background-surface text-text-secondary text-chip font-medium z-40"
    aria-label="App Version"
    data-testid="version-badge"
  >
    <span class="text-text font-medium" data-testid="version-label">Mongoose.gg <span class="text-[0.5em] text-text-secondary font-normal align-top">Beta</span></span>
    <span class="mx-1.5 opacity-70" data-testid="version-sep">•</span>
    <span class="text-positive font-bold" data-testid="version-value">v{{ version }}</span>
  </div>
</template>

<script setup>
import pkg from '../../package.json'

const version = pkg.version || '0.0.0'
</script>
