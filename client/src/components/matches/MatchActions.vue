<template>
  <div class="match-actions">
    <button class="action-btn primary" @click="viewAnalysis" aria-label="View analysis on Solo Dashboard">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="btn-icon">
        <path d="M15.5 2A1.5 1.5 0 0014 3.5v13a1.5 1.5 0 001.5 1.5h1a1.5 1.5 0 001.5-1.5v-13A1.5 1.5 0 0016.5 2h-1zM9.5 6A1.5 1.5 0 008 7.5v9A1.5 1.5 0 009.5 18h1a1.5 1.5 0 001.5-1.5v-9A1.5 1.5 0 0010.5 6h-1zM3.5 10A1.5 1.5 0 002 11.5v5A1.5 1.5 0 003.5 18h1A1.5 1.5 0 006 16.5v-5A1.5 1.5 0 004.5 10h-1z" />
      </svg>
      <span>View Analysis</span>
    </button>
    <button class="action-btn secondary" disabled title="Coming soon">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="btn-icon">
        <path fill-rule="evenodd" d="M10.868 2.884c-.321-.772-1.415-.772-1.736 0l-1.83 4.401-4.753.381c-.833.067-1.171 1.107-.536 1.651l3.62 3.102-1.106 4.637c-.194.813.691 1.456 1.405 1.02L10 15.591l4.069 2.485c.713.436 1.598-.207 1.404-1.02l-1.106-4.637 3.62-3.102c.635-.544.297-1.584-.536-1.65l-4.752-.382-1.831-4.401z" clip-rule="evenodd" />
      </svg>
      <span>View Goal Impact</span>
    </button>
  </div>
</template>

<script setup>
import { useRouter } from 'vue-router'

const router = useRouter()

function viewAnalysis() {
  router.push({ name: 'app-solo' })
}
</script>

<style scoped>
.match-actions {
  display: flex;
  gap: var(--spacing-sm);
}

.action-btn {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-xs);
  padding: var(--spacing-sm) var(--spacing-md);
  border-radius: var(--radius-md);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  transition: all 0.15s ease;
  border: 1px solid transparent;
}

.action-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.action-btn.primary {
  background: var(--color-primary);
  color: white;
  border-color: var(--color-primary);
}

.action-btn.primary:hover:not(:disabled) {
  background: #7c3aed;
}

.action-btn.secondary {
  background: var(--color-surface);
  color: var(--color-text);
  border-color: var(--color-border);
}

.action-btn.secondary:hover:not(:disabled) {
  border-color: var(--color-primary);
  background: var(--color-elevated);
}

.btn-icon {
  width: 16px;
  height: 16px;
}
</style>

