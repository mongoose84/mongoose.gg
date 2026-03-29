<template>
  <section v-if="showSection" class="flex flex-col gap-md" data-testid="display-preferences-section">
    <h2 class="text-lg font-semibold text-text tracking-tight">Display Preferences</h2>
    <div class="bg-background-surface border border-border rounded-lg p-xl">
      <!-- Default View -->
      <div class="py-md border-b border-border">
        <label for="default-view-select" class="text-sm font-medium text-text">Default View</label>
        <p id="default-view-description" class="text-xs text-text-secondary mt-xs mb-sm">
          The account context shown when you open the app
        </p>
        <select
          id="default-view-select"
          :value="defaultView"
          aria-describedby="default-view-description"
          class="w-full bg-background border border-border rounded-md px-md py-sm text-sm text-text cursor-pointer focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
          data-testid="default-view-select"
          @change="handleDefaultViewChange"
        >
          <option value="overall">Overall</option>
          <option
            v-for="account in riotAccounts"
            :key="account.puuid"
            :value="getAccountIdentifier(account)"
          >
            {{ account.gameName }}#{{ account.tagLine }}
          </option>
        </select>
      </div>

      <!-- Chart Display Mode -->
      <div class="py-md">
        <label for="chart-mode-select" class="text-sm font-medium text-text">Chart Display Mode</label>
        <p id="chart-mode-description" class="text-xs text-text-secondary mt-xs mb-sm">
          How trend charts show data across multiple accounts
        </p>
        <select
          id="chart-mode-select"
          :value="chartMode"
          aria-describedby="chart-mode-description"
          class="w-full bg-background border border-border rounded-md px-md py-sm text-sm text-text cursor-pointer focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
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
import { computed } from 'vue'
import { useAuthStore } from '@/stores/authStore'
import { useDefaultView } from '@/composables/useDefaultView'
import { useChartDisplayMode } from '@/composables/useChartDisplayMode'

const authStore = useAuthStore()
const { defaultView, setDefaultView } = useDefaultView()
const { chartMode, setChartMode } = useChartDisplayMode()

const riotAccounts = computed(() => authStore.riotAccounts)
const showSection = computed(() => riotAccounts.value.length >= 2)

function getAccountIdentifier(account) {
  if (account.accountId && account.accountId.trim().length > 0) return account.accountId
  if (account.puuid && account.puuid.trim().length > 0) return account.puuid
  return null
}

function handleDefaultViewChange(event) {
  setDefaultView(event.target.value)
}

function handleChartModeChange(event) {
  setChartMode(event.target.value)
}
</script>
