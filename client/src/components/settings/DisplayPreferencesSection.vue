<template>
  <section
    v-if="authStore.riotAccounts.length >= 2"
    class="flex flex-col gap-md"
    data-testid="display-preferences-section"
  >
    <h2 class="text-lg font-semibold text-text tracking-tight">Display Preferences</h2>
    <div class="bg-background-surface border border-border rounded-lg p-xl">
      <!-- Default View -->
      <div class="py-md border-b border-border">
        <label :for="defaultViewId" class="text-sm font-medium text-text">Default View</label>
        <p :id="defaultViewDescId" class="text-xs text-text-secondary mt-xs mb-sm">
          The account context shown when you open the app
        </p>
        <select
          :id="defaultViewId"
          :value="defaultView"
          :aria-describedby="defaultViewDescId"
          class="settings-select"
          data-testid="default-view-select"
          @change="handleDefaultViewChange"
        >
          <option value="overall">Overall</option>
          <option
            v-for="account in authStore.riotAccounts"
            :key="account.puuid"
            :value="account.puuid"
          >
            {{ account.gameName }}#{{ account.tagLine }}
          </option>
        </select>
      </div>

      <!-- Chart Display Mode -->
      <div class="py-md">
        <label :for="chartModeId" class="text-sm font-medium text-text">Chart Display Mode</label>
        <p :id="chartModeDescId" class="text-xs text-text-secondary mt-xs mb-sm">
          How trend charts show data across multiple accounts
        </p>
        <select
          :id="chartModeId"
          :value="chartMode"
          :aria-describedby="chartModeDescId"
          class="settings-select"
          data-testid="chart-mode-select"
          @change="handleChartModeChange"
        >
          <option value="merged">Merged (single line)</option>
          <option value="per-account">Per-Account Lines</option>
        </select>
      </div>
    </div>
  </section>
</template>

<script setup>
import { useId } from 'vue'
import { useAuthStore } from '@/stores/authStore'
import { useDefaultView } from '@/composables/useDefaultView'
import { useChartDisplayMode } from '@/composables/useChartDisplayMode'

const authStore = useAuthStore()
const { defaultView, setDefaultView } = useDefaultView()
const { chartMode, setChartMode } = useChartDisplayMode()

const generatedId = useId()
const defaultViewId = `default-view-${generatedId}`
const defaultViewDescId = `default-view-desc-${generatedId}`
const chartModeId = `chart-mode-${generatedId}`
const chartModeDescId = `chart-mode-desc-${generatedId}`

function handleDefaultViewChange(event) {
  setDefaultView(event.target.value)
}

function handleChartModeChange(event) {
  setChartMode(event.target.value)
}
</script>

<style scoped>
.settings-select {
  padding: var(--spacing-sm) var(--spacing-md);
  background-color: #020617;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text);
  font-size: var(--font-size-sm);
  cursor: pointer;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
  width: 100%;
  max-width: 280px;
}

.settings-select:hover {
  border-color: var(--color-primary);
}

.settings-select:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px rgba(147, 51, 234, 0.1);
}

.settings-select option {
  background-color: #020617;
  color: var(--color-text);
}
</style>