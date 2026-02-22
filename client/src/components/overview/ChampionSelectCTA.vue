<template>
  <router-link
    to="/app/champion-select"
    class="champion-select-cta"
    data-testid="champion-select-cta"
  >
    <div v-if="hasMural" class="cta-mural-layer" aria-hidden="true">
      <img
        :src="muralUrl"
        :alt="''"
        class="cta-mural-image"
        @error="handleMuralError"
      />
    </div>
    <div v-if="hasMural" class="cta-overlay-layer" aria-hidden="true"></div>

    <!-- Icon -->
    <div class="cta-foreground">
      <div class="cta-icon-wrapper">
        <SparklesIcon class="cta-icon" />
      </div>

      <!-- Content -->
      <div class="cta-content">
        <h3 class="cta-title">Champion Select Helper</h3>
        <p class="cta-subtitle">Get personal matchup tips before you lock in</p>
      </div>

      <!-- Arrow indicator -->
      <div class="cta-arrow">
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="arrow-icon">
          <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd" />
        </svg>
      </div>
    </div>
  </router-link>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { SparklesIcon } from '@heroicons/vue/24/solid'

const props = defineProps({
  muralUrl: {
    type: String,
    default: ''
  },
  championName: {
    type: String,
    default: ''
  }
})

const isMuralErrored = ref(false)

const hasMural = computed(() => Boolean(props.muralUrl) && !isMuralErrored.value)

function handleMuralError() {
  isMuralErrored.value = true
}

watch(() => props.muralUrl, () => {
  isMuralErrored.value = false
})
</script>

<style scoped>
.champion-select-cta {
  position: relative;
  overflow: hidden;
  isolation: isolate;
  display: flex;
  align-items: center;
  height: 100%;
  gap: var(--spacing-lg);
  padding: var(--spacing-lg);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  backdrop-filter: blur(10px);
  text-decoration: none;
  color: inherit;
  cursor: pointer;
  transition: all 0.2s ease;
}

.cta-mural-layer {
  position: absolute;
  inset: 0;
  pointer-events: none;
  z-index: 1;
}

.cta-mural-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  opacity: 0.5;
}

.cta-overlay-layer {
  position: absolute;
  inset: 0;
  pointer-events: none;
  z-index: 2;
  background: linear-gradient(
    120deg,
    color-mix(in srgb, var(--color-surface) 98%, transparent) 0%,
    color-mix(in srgb, var(--color-surface) 92%, transparent) 45%,
    color-mix(in srgb, var(--color-surface) 78%, transparent) 100%
  );
}

.cta-foreground {
  position: relative;
  z-index: 3;
  display: flex;
  align-items: center;
  gap: var(--spacing-lg);
  width: 100%;
}

.champion-select-cta:hover {
  border-color: var(--color-primary);
  background: var(--color-elevated);
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

.champion-select-cta:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}

/* Icon wrapper - matches rank emblem size from RankSnapshot */
.cta-icon-wrapper {
  width: 72px;
  height: 72px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--color-primary-soft);
  border-radius: 50%;
}

.cta-icon {
  width: 36px;
  height: 36px;
  color: var(--color-primary);
}

/* Content */
.cta-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
  min-width: 0;
  flex: 1;
}

.cta-title {
  margin: 0;
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  letter-spacing: var(--letter-spacing);
}

.cta-subtitle {
  margin: 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  line-height: 1.5;
}

/* Arrow indicator */
.cta-arrow {
  flex-shrink: 0;
  color: var(--color-text-tertiary);
  transition: color 0.2s ease, transform 0.2s ease;
}

.champion-select-cta:hover .cta-arrow {
  color: var(--color-primary);
  transform: translateX(2px);
}

.arrow-icon {
  width: 20px;
  height: 20px;
}

/* Mobile Responsive */
@media (max-width: 480px) {
  .cta-foreground {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-md);
  }

  .champion-select-cta {
    padding: var(--spacing-md);
  }

  .cta-icon-wrapper {
    width: 56px;
    height: 56px;
  }

  .cta-icon {
    width: 28px;
    height: 28px;
  }

  .cta-title {
    font-size: var(--font-size-md);
  }

  .cta-arrow {
    display: none;
  }
}
</style>