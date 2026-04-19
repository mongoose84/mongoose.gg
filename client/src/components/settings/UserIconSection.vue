<template>
  <section class="flex flex-col gap-md" data-testid="user-icon-section">
    <h2 class="text-lg font-semibold text-text tracking-tight">User Icon</h2>
    <div class="bg-background-surface border border-border rounded-lg p-xl">
      <p id="user-icon-description" class="text-xs text-text-secondary mb-md">
        Choose a profile icon to display in the sidebar
      </p>
      <p v-if="persistError" class="text-xs text-error mb-md" data-testid="user-icon-persist-error">
        {{ persistError }}
      </p>

      <!-- Current selection preview -->
      <div class="flex items-center gap-md mb-lg" data-testid="user-icon-preview">
        <div class="w-12 h-12 rounded-full overflow-hidden bg-background-elevated flex items-center justify-center border-2 border-primary shrink-0">
          <img
            v-if="userIconUrl"
            :src="userIconUrl"
            alt="Selected profile icon"
            class="w-full h-full object-cover"
            @error="handlePreviewError"
          />
          <svg v-else xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="w-6 h-6 text-text-secondary">
            <path fill-rule="evenodd" d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z" clip-rule="evenodd" />
          </svg>
        </div>
        <div class="flex flex-col">
          <span class="text-sm font-medium text-text">{{ selectedIconId ? `Icon #${selectedIconId}` : 'No icon selected' }}</span>
          <button
            v-if="selectedIconId"
            class="text-xs text-text-secondary hover:text-error cursor-pointer bg-transparent border-none p-0 text-left"
            data-testid="user-icon-clear"
            @click="handleSetUserIcon(null)"
          >
            Remove icon
          </button>
        </div>
      </div>

      <!-- Icon grid -->
      <div
        class="grid gap-sm"
        style="grid-template-columns: repeat(auto-fill, minmax(40px, 1fr));"
        role="radiogroup"
        aria-label="Profile icon selection"
        aria-describedby="user-icon-description"
        data-testid="user-icon-grid"
      >
        <button
          v-for="iconId in iconOptions"
          :key="iconId"
          v-show="!failedIcons.includes(iconId)"
          role="radio"
          :aria-checked="selectedIconId === iconId"
          :aria-label="`Profile icon ${iconId}`"
          class="w-10 h-10 rounded-md overflow-hidden cursor-pointer border-2 transition-all duration-150 hover:opacity-80 hover:border-text-secondary bg-transparent p-0"
          :class="selectedIconId === iconId ? 'border-primary ring-2 ring-primary/30' : 'border-transparent'"
          :data-testid="`user-icon-option-${iconId}`"
          @click="handleSetUserIcon(iconId)"
        >
          <img
            :src="getIconUrl(iconId)"
            :alt="`Icon ${iconId}`"
            class="w-full h-full object-cover"
            loading="lazy"
            @error="handleIconError(iconId)"
          />
        </button>
      </div>
    </div>
  </section>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { useUserIcon, ICON_OPTIONS } from '@/composables/useUserIcon'
import { getProfileIconUrl } from '@/utils/leagueAssets'
import { useAuthStore } from '@/stores/authStore'

const { selectedIconId, userIconUrl, setUserIcon } = useUserIcon()
const authStore = useAuthStore()
const persistError = ref('')

const iconOptions = ICON_OPTIONS
const failedIcons = reactive([])

function getIconUrl(iconId) {
  return getProfileIconUrl(iconId)
}

function handleIconError(iconId) {
  if (!failedIcons.includes(iconId)) {
    failedIcons.push(iconId)
  }
}

function handlePreviewError() {
  handleSetUserIcon(null)
}

async function handleSetUserIcon(iconId) {
  persistError.value = ''
  setUserIcon(iconId)

  if (!authStore.isAuthenticated) {
    return
  }

  try {
    await authStore.updateUserIcon(iconId)
  } catch (error) {
    persistError.value = 'Could not save icon to your profile. This selection will be lost after you sign out.'
    console.error('Failed to persist user icon preference:', error)
  }
}
</script>
