<template>
  <router-link
    to="/app/solo"
    class="solo-analytics-cta"
  >
    <div
      class="cta-mural-layer"
      :class="muralClass"
      aria-hidden="true"
      data-testid="solo-analytics-mural"
    ></div>
    <div class="cta-overlay-layer" aria-hidden="true"></div>

    <div class="cta-foreground">
    <div class="cta-icon-wrapper">
      <ArrowTrendingUpIcon
        v-if="props.trendDirection === 'up'"
        class="cta-icon cta-icon--up"
        data-testid="solo-kda-trend-up-icon"
      />
      <ArrowTrendingDownIcon
        v-else-if="props.trendDirection === 'down'"
        class="cta-icon cta-icon--down"
        data-testid="solo-kda-trend-down-icon"
      />
      <ChartBarIcon
        v-else
        class="cta-icon cta-icon--neutral"
        data-testid="solo-kda-trend-neutral-icon"
      />
    </div>

    <div class="cta-content">
      <h3 class="cta-title">Solo Analytics</h3>
      <p class="cta-subtitle">{{ subtitle }}</p>
    </div>

    <div class="cta-arrow">
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="arrow-icon">
        <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd" />
      </svg>
    </div>
    </div>
  </router-link>
</template>

<script setup>
import { computed } from 'vue'
import { ChartBarIcon, ArrowTrendingUpIcon, ArrowTrendingDownIcon } from '@heroicons/vue/24/solid'

const props = defineProps({
  subtitle: {
    type: String,
    default: 'Track your trends and improve'
  },
  trendDirection: {
    type: String,
    default: 'neutral',
    validator: (value) => ['up', 'down', 'neutral'].includes(value)
  }
})

const muralClass = computed(() => {
  if (props.trendDirection === 'up') {
    return 'cta-mural-layer--up'
  }

  if (props.trendDirection === 'down') {
    return 'cta-mural-layer--down'
  }

  return 'cta-mural-layer--neutral'
})
</script>

<style scoped>
.solo-analytics-cta {
  position: relative;
  overflow: hidden;
  isolation: isolate;
  display: flex;
  align-items: center;
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
  --mural-color: var(--color-primary);
  position: absolute;
  inset: 0;
  pointer-events: none;
  z-index: 1;
  opacity: 0.35;
  background-image:
    linear-gradient(
      120deg,
      color-mix(in srgb, var(--mural-color) 35%, transparent) 0%,
      color-mix(in srgb, var(--mural-color) 18%, transparent) 42%,
      transparent 100%
    ),
    repeating-linear-gradient(
      -18deg,
      color-mix(in srgb, var(--mural-color) 20%, transparent) 0 2px,
      transparent 2px 13px
    );
}

.cta-mural-layer--neutral {
  --mural-color: var(--color-primary);
}

.cta-mural-layer--up {
  --mural-color: var(--color-success);
}

.cta-mural-layer--down {
  --mural-color: var(--color-error);
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

.solo-analytics-cta:hover {
  border-color: var(--color-primary);
  background: var(--color-elevated);
  transform: translateY(-2px);
  box-shadow: var(--shadow-md);
}

.solo-analytics-cta:focus-visible {
  outline: none;
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}

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
}

.cta-icon--neutral {
  color: var(--color-primary);
}

.cta-icon--up {
  color: var(--color-success);
}

.cta-icon--down {
  color: var(--color-error);
}

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

.cta-arrow {
  flex-shrink: 0;
  color: var(--color-text-tertiary);
  transition: color 0.2s ease, transform 0.2s ease;
}

.solo-analytics-cta:hover .cta-arrow {
  color: var(--color-primary);
  transform: translateX(2px);
}

.arrow-icon {
  width: 20px;
  height: 20px;
}

@media (max-width: 480px) {
  .cta-foreground {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-md);
  }

  .solo-analytics-cta {
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