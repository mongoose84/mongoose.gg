<template>
  <section class="overview-player-header">
    <!-- Profile Icon with Level Badge -->
    <div class="profile-icon-wrapper">
      <img
        v-if="profileIconUrl"
        :src="profileIconUrl"
        :alt="`${summonerName} profile icon`"
        class="profile-icon"
        @error="handleIconError"
      />
      <svg v-else xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="profile-icon-fallback">
        <path fill-rule="evenodd" d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z" clip-rule="evenodd" />
      </svg>
      <span v-if="level" class="level-badge">{{ level }}</span>
    </div>

    <!-- Player Info -->
    <div class="player-info">
      <div class="name-row">
        <h2 class="summoner-name">{{ summonerName }}</h2>
        <span class="region-tag">{{ regionDisplay }}</span>
      </div>

      <!-- Context Badges -->
      <div v-if="activeContexts && activeContexts.length > 0" class="context-badges">
        <span
          v-for="context in activeContexts"
          :key="context"
          :class="['context-badge', `context-${context.toLowerCase()}`]"
        >
          {{ contextLabel(context) }}
        </span>
      </div>
    </div>
  </section>
</template>

<script setup>
import { ref, computed } from 'vue'

const props = defineProps({
  summonerName: {
    type: String,
    required: true
  },
  level: {
    type: Number,
    default: null
  },
  region: {
    type: String,
    required: true
  },
  profileIconUrl: {
    type: String,
    default: null
  },
  activeContexts: {
    type: Array,
    default: () => []
  }
})

const iconError = ref(false)

// Region labels for display (convert API codes to friendly names)
const regionLabels = {
  euw1: 'EUW',
  eun1: 'EUNE',
  na1: 'NA',
  kr: 'KR',
  jp1: 'JP',
  br1: 'BR',
  la1: 'LAN',
  la2: 'LAS',
  oc1: 'OCE',
  tr1: 'TR',
  ru: 'RU',
  ph2: 'PH',
  sg2: 'SG',
  th2: 'TH',
  tw2: 'TW',
  vn2: 'VN'
}

const regionDisplay = computed(() => {
  const region = props.region?.toLowerCase()
  return regionLabels[region] || props.region?.toUpperCase() || ''
})

function handleIconError() {
  iconError.value = true
}

function contextLabel(context) {
  const labels = {
    'Solo': 'Solo',
    'Duo': 'Duo',
    'Team': 'Team'
  }
  return labels[context] || context
}
</script>

<style scoped>
.overview-player-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-lg);
  padding: var(--spacing-lg);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  backdrop-filter: blur(10px);
}

.profile-icon-wrapper {
  position: relative;
  width: 72px;
  height: 72px;
  border-radius: 50%;
  overflow: visible;
  flex-shrink: 0;
  border: 2px solid var(--color-primary);
  background: var(--color-elevated);
}

.profile-icon {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 50%;
}

.profile-icon-fallback {
  width: 40px;
  height: 40px;
  color: var(--color-text-secondary);
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
}

.level-badge {
  position: absolute;
  bottom: -4px;
  right: -4px;
  background: var(--color-primary);
  color: white;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-bold);
  padding: 2px 6px;
  border-radius: 10px;
  min-width: 28px;
  text-align: center;
  line-height: 1.2;
}

.player-info {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
  min-width: 0;
}

.name-row {
  display: flex;
  align-items: baseline;
  gap: var(--spacing-sm);
  flex-wrap: wrap;
}

.summoner-name {
  margin: 0;
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
  letter-spacing: var(--letter-spacing);
}

.region-tag {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
  text-transform: uppercase;
}

.context-badges {
  display: flex;
  gap: var(--spacing-xs);
  flex-wrap: wrap;
}

.context-badge {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  padding: 4px 10px;
  border-radius: var(--radius-sm);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.context-solo {
  background: rgba(109, 40, 217, 0.2);
  color: var(--color-primary);
}

.context-duo {
  background: rgba(34, 197, 94, 0.2);
  color: #22c55e;
}

.context-team {
  background: rgba(59, 130, 246, 0.2);
  color: #3b82f6;
}

/* Mobile responsive */
@media (max-width: 480px) {
  .overview-player-header {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-md);
    padding: var(--spacing-md);
  }

  .profile-icon-wrapper {
    width: 56px;
    height: 56px;
  }

  .summoner-name {
    font-size: var(--font-size-lg);
  }

  .name-row {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-xs);
  }
}
</style>

