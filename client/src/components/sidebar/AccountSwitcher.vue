<template>
  <div
    class="account-switcher relative"
    data-testid="account-switcher"
    ref="switcherRef"
  >
    <!-- ── Expanded: Trigger Row ── -->
    <template v-if="!collapsed">
      <button
        class="w-full flex items-center gap-sm px-sm py-xs rounded-md cursor-pointer transition-all duration-200 hover:bg-background-elevated text-left"
        :aria-expanded="isOpen"
        aria-haspopup="listbox"
        data-testid="account-switcher-trigger"
        @click="toggle"
        @keydown="handleTriggerKeydown"
      >
        <!-- Active account icon -->
        <span class="shrink-0 w-5 h-5 flex items-center justify-center">
          <span v-if="isOverall" class="w-5 h-5 rounded-full bg-background-elevated flex items-center justify-center text-[10px] font-bold text-text-secondary" aria-hidden="true">Σ</span>
          <img
            v-else-if="activeIconUrl"
            :src="activeIconUrl"
            alt=""
            class="w-5 h-5 rounded-full object-cover"
            aria-hidden="true"
            @error="handleActiveIconError"
          />
          <span v-else class="w-5 h-5 rounded-full bg-background-elevated flex items-center justify-center" aria-hidden="true">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="w-3 h-3 text-text-secondary">
              <path fill-rule="evenodd" d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z" clip-rule="evenodd" />
            </svg>
          </span>
        </span>

        <!-- Account label -->
        <span class="flex-1 min-w-0">
          <span v-if="isOverall" class="text-sm font-medium text-text">Overall</span>
          <template v-else-if="activeAccount">
            <span class="text-sm font-medium text-text truncate block" :title="`${activeAccount.gameName}#${activeAccount.tagLine}`">
              {{ activeAccount.gameName }}<span class="text-text-secondary">#{{ activeAccount.tagLine }}</span>
            </span>
            <span class="text-3xs text-text-secondary">{{ formatRegion(activeAccount.region) }}</span>
          </template>
          <span v-else class="text-sm font-medium text-text-secondary">No account</span>
        </span>

        <!-- Chevron -->
        <svg
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 20 20"
          fill="currentColor"
          class="w-4 h-4 text-text-secondary ml-auto shrink-0 transition-transform duration-200"
          :class="{ 'rotate-180': isOpen }"
          aria-hidden="true"
        >
          <path fill-rule="evenodd" d="M5.22 8.22a.75.75 0 0 1 1.06 0L10 11.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 9.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd" />
        </svg>
      </button>

      <!-- Dropdown: opens above trigger row, same width as parent -->
      <Transition name="dropdown">
        <div
          v-if="isOpen"
          ref="listboxRef"
          role="listbox"
          aria-label="Switch account"
          tabindex="0"
          data-testid="account-switcher-dropdown"
          class="absolute bottom-full left-0 right-0 mb-1 z-50 rounded-lg border border-border shadow-lg overflow-y-auto bg-background-overlay"
          style="max-height: 280px;"
          @keydown="handleListKeydown"
        >
          <AccountDropdownList
            :accounts="accounts"
            :active-account-puuid="activeAccountPuuid"
            :show-overall="showOverall"
            :focused-index="focusedIndex"
            :dd-version="ddVersion"
            @select="handleSelect"
          />
        </div>
      </Transition>
    </template>

    <!-- ── Collapsed: Icon only + Popover ── -->
    <template v-else>
      <button
        class="w-8 h-8 rounded-full flex items-center justify-center cursor-pointer hover:opacity-80 transition-opacity mx-auto"
        :aria-expanded="isOpen"
        aria-haspopup="listbox"
        data-testid="account-switcher-trigger-collapsed"
        :title="isOverall ? 'Overall' : (activeAccount ? `${activeAccount.gameName}#${activeAccount.tagLine}` : 'Switch account')"
        :aria-label="isOverall ? 'Overall' : (activeAccount ? `${activeAccount.gameName}#${activeAccount.tagLine}` : 'Switch account')"
        @click="toggle"
        @keydown="handleTriggerKeydown"
      >
        <span v-if="isOverall" class="w-8 h-8 rounded-full bg-background-elevated flex items-center justify-center text-xs font-bold text-text-secondary" aria-hidden="true">Σ</span>
        <img
          v-else-if="activeIconUrl"
          :src="activeIconUrl"
          alt=""
          class="w-8 h-8 rounded-full object-cover"
          aria-hidden="true"
          @error="handleActiveIconError"
        />
        <span v-else class="w-8 h-8 rounded-full bg-background-elevated flex items-center justify-center" aria-hidden="true">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="w-4 h-4 text-text-secondary">
            <path fill-rule="evenodd" d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z" clip-rule="evenodd" />
          </svg>
        </span>
      </button>

      <!-- Popover: fixed position to the right of collapsed sidebar -->
      <Transition name="dropdown">
        <div
          v-if="isOpen"
          ref="listboxRef"
          role="listbox"
          aria-label="Switch account"
          tabindex="0"
          data-testid="account-switcher-popover"
          class="fixed z-50 rounded-lg border border-border shadow-lg overflow-y-auto bg-background-overlay"
          style="min-width: 220px; max-height: 280px;"
          :style="popoverStyle"
          @keydown="handleListKeydown"
        >
          <AccountDropdownList
            :accounts="accounts"
            :active-account-puuid="activeAccountPuuid"
            :show-overall="showOverall"
            :focused-index="focusedIndex"
            :dd-version="ddVersion"
            @select="handleSelect"
          />
        </div>
      </Transition>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, watch, nextTick, onMounted, onUnmounted } from 'vue'
import AccountDropdownList from './AccountDropdownList.vue'
import { formatRegion } from '@/utils/leagueAssets'

// ── Props ──
const props = defineProps({
  /** Whether the sidebar is in collapsed mode */
  collapsed: {
    type: Boolean,
    default: false
  },
  /** Array of linked Riot accounts from authStore.riotAccounts */
  accounts: {
    type: Array,
    default: () => []
  },
  /** Active account identifier or 'overall' */
  activeAccountPuuid: {
    type: String,
    default: 'overall'
  },
  /** Whether to show the Overall option (true when 2+ accounts) */
  showOverall: {
    type: Boolean,
    default: false
  }
})

// ── Emits ──
const emit = defineEmits(['select'])

// ── Constants ──
const ddVersion = '16.1.1'
const POPOVER_HORIZONTAL_OFFSET = 8

// ── State ──
const isOpen = ref(false)
const switcherRef = ref(null)
const listboxRef = ref(null)
const focusedIndex = ref(-1)
const activeIconError = ref(false)
const popoverStyle = ref({ top: '-9999px', left: '-9999px' })

// ── Computed ──
const isOverall = computed(() => props.activeAccountPuuid === 'overall')

const activeAccount = computed(() => {
  if (isOverall.value) return null
  return props.accounts.find(a =>
    (a.accountId && a.accountId === props.activeAccountPuuid) ||
    (a.puuid && a.puuid === props.activeAccountPuuid)
  ) ?? null
})

const activeIconUrl = computed(() => {
  const iconId = activeAccount.value?.profileIconId
  if (!iconId || activeIconError.value) return null
  return `https://ddragon.leagueoflegends.com/cdn/${ddVersion}/img/profileicon/${iconId}.png`
})

/** Total navigable options (Overall slot + accounts) */
const totalOptions = computed(() => props.accounts.length + (props.showOverall ? 1 : 0))

// ── Helpers ──
function handleActiveIconError() {
  activeIconError.value = true
}

// ── Dropdown control ──
function open(focusListbox = false) {
  isOpen.value = true
  focusedIndex.value = -1
  if (props.collapsed) nextTick(updatePopoverPosition)
  if (focusListbox) nextTick(() => listboxRef.value?.focus())
}

function close() {
  isOpen.value = false
  focusedIndex.value = -1
}

function toggle() {
  if (isOpen.value) close()
  else open()
}

function updatePopoverPosition() {
  if (!switcherRef.value) return
  const rect = switcherRef.value.getBoundingClientRect()
  popoverStyle.value = {
    top: `${rect.top}px`,
    left: `${rect.right + POPOVER_HORIZONTAL_OFFSET}px`
  }
}

// ── Selection handlers ──
function handleSelect(identifier) {
  emit('select', identifier)
  close()
}

// ── Keyboard navigation ──
function handleTriggerKeydown(event) {
  if (event.key === 'ArrowDown') {
    event.preventDefault()
    if (!isOpen.value) open(true)
    else nextTick(() => listboxRef.value?.focus())
    focusedIndex.value = 0
  } else if (event.key === 'Escape') {
    event.preventDefault()
    close()
  }
}

function handleListKeydown(event) {
  switch (event.key) {
    case 'Escape':
      event.preventDefault()
      close()
      break
    case 'ArrowDown':
      event.preventDefault()
      focusedIndex.value = Math.min(focusedIndex.value + 1, totalOptions.value - 1)
      break
    case 'ArrowUp':
      event.preventDefault()
      focusedIndex.value = Math.max(focusedIndex.value - 1, 0)
      break
    case 'Home':
      event.preventDefault()
      focusedIndex.value = 0
      break
    case 'End':
      event.preventDefault()
      focusedIndex.value = totalOptions.value - 1
      break
  }
}

// ── Click outside ──
function handleClickOutside(event) {
  if (switcherRef.value && !switcherRef.value.contains(event.target)) {
    close()
  }
}

// ── Watch collapsed changes ──
watch(() => props.collapsed, () => { close() })

// Reset icon error when active account changes so a previous error doesn't hide future icons
watch(() => props.activeAccountPuuid, () => { activeIconError.value = false })

// ── Lifecycle ──
onMounted(() => { document.addEventListener('mousedown', handleClickOutside) })
onUnmounted(() => { document.removeEventListener('mousedown', handleClickOutside) })
</script>

<style scoped>
.dropdown-enter-active,
.dropdown-leave-active {
  transition: opacity 0.15s ease-out, transform 0.15s ease-out;
}

.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: scale(0.95);
}
</style>
