<template>
  <div
    class="px-sm py-xs"
    data-testid="account-dropdown-list"
  >
    <!-- Overall option -->
    <div
      v-if="showOverall"
      role="option"
      :aria-selected="activeAccountPuuid === 'overall'"
      :data-focused="focusedIndex === 0"
      class="flex items-center gap-sm px-md py-xs cursor-pointer transition-colors duration-150 rounded-md"
      :class="[
        activeAccountPuuid === 'overall'
          ? 'text-text font-medium'
          : 'text-text-secondary hover:bg-background-elevated hover:text-text',
        focusedIndex === 0 ? 'bg-background-elevated text-text' : ''
      ]"
      data-testid="account-option-overall"
      aria-label="View all accounts combined"
      tabindex="-1"
      @click="$emit('select', 'overall')"
    >
      <!-- Leading slot: checkmark or spacer -->
      <span class="w-4 h-4 shrink-0 flex items-center justify-center">
        <svg v-if="activeAccountPuuid === 'overall'" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4 text-primary" aria-hidden="true">
          <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 0 1 .143 1.052l-8 10.5a.75.75 0 0 1-1.127.075l-4.5-4.5a.75.75 0 0 1 1.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 0 1 1.05-.143Z" clip-rule="evenodd" />
        </svg>
        <span v-else class="w-4 h-4" aria-hidden="true" />
      </span>

      <!-- Σ icon -->
      <span class="w-5 h-5 rounded-full bg-background-elevated flex items-center justify-center text-[10px] font-bold text-text-secondary shrink-0" aria-hidden="true">Σ</span>

      <!-- Label -->
      <span class="text-sm">Overall</span>
    </div>

    <!-- Account options -->
    <div
      v-for="(account, index) in accounts"
      :key="account.puuid || account.accountId"
      role="option"
      :aria-selected="isAccountActive(account)"
      :data-focused="focusedIndex === accountOptionIndex(index)"
      class="flex items-center gap-sm px-md py-xs cursor-pointer transition-colors duration-150 rounded-md"
      :class="[
        isAccountActive(account)
          ? 'text-text font-medium'
          : 'text-text-secondary hover:bg-background-elevated hover:text-text',
        focusedIndex === accountOptionIndex(index) ? 'bg-background-elevated text-text' : ''
      ]"
      :data-testid="`account-option-${account.gameName || index}`"
      tabindex="-1"
      @click="$emit('select', account.accountId || account.puuid)"
    >
      <!-- Leading slot: checkmark or spacer -->
      <span class="w-4 h-4 shrink-0 flex items-center justify-center">
        <svg v-if="isAccountActive(account)" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4 text-primary" aria-hidden="true">
          <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 0 1 .143 1.052l-8 10.5a.75.75 0 0 1-1.127.075l-4.5-4.5a.75.75 0 0 1 1.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 0 1 1.05-.143Z" clip-rule="evenodd" />
        </svg>
        <span v-else class="w-4 h-4" aria-hidden="true" />
      </span>

      <!-- Profile icon -->
      <span class="shrink-0">
        <img
          v-if="getIconUrl(account)"
          :src="getIconUrl(account)"
          :alt="`${account.gameName} profile icon`"
          class="w-5 h-5 rounded-full object-cover"
        />
        <span v-else class="w-5 h-5 rounded-full bg-background-elevated flex items-center justify-center" aria-hidden="true">
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="w-3 h-3 text-text-secondary">
            <path fill-rule="evenodd" d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z" clip-rule="evenodd" />
          </svg>
        </span>
      </span>

      <!-- Account name + region -->
      <span class="flex-1 min-w-0">
        <span class="text-sm truncate block" :title="`${account.gameName}#${account.tagLine}`">
          {{ account.gameName }}<span class="text-text-secondary opacity-70">#{{ account.tagLine }}</span>
        </span>
        <span v-if="account.region" class="text-3xs text-text-secondary">{{ formatRegion(account.region) }}</span>
      </span>
    </div>

    <!-- Divider + Link Account -->
    <div class="border-t border-border my-xs" />
    <button
      type="button"
      class="flex items-center gap-sm px-md py-xs text-xs text-primary hover:text-primary cursor-pointer hover:bg-background-elevated rounded-md transition-colors duration-150 w-full text-left"
      data-testid="account-switcher-link-button"
      tabindex="-1"
      @click="$emit('link')"
    >
      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-4 h-4 shrink-0" aria-hidden="true">
        <path d="M10.75 4.75a.75.75 0 0 0-1.5 0v4.5h-4.5a.75.75 0 0 0 0 1.5h4.5v4.5a.75.75 0 0 0 1.5 0v-4.5h4.5a.75.75 0 0 0 0-1.5h-4.5v-4.5Z" />
      </svg>
      Link Account
    </button>
  </div>
</template>

<script setup>
import { formatRegion } from '@/utils/leagueAssets'

const props = defineProps({
  accounts: {
    type: Array,
    default: () => []
  },
  activeAccountPuuid: {
    type: String,
    default: 'overall'
  },
  showOverall: {
    type: Boolean,
    default: false
  },
  focusedIndex: {
    type: Number,
    default: -1
  },
  ddVersion: {
    type: String,
    default: '16.1.1'
  }
})

defineEmits(['select', 'link'])

function isAccountActive(account) {
  const id = props.activeAccountPuuid
  return (account.accountId && account.accountId === id) || (account.puuid && account.puuid === id)
}

/** Map account list index to the overall focused-index space (accounting for the Overall slot) */
function accountOptionIndex(index) {
  return index + (props.showOverall ? 1 : 0)
}

function getIconUrl(account) {
  const iconId = account?.profileIconId
  if (!iconId) return null
  return `https://ddragon.leagueoflegends.com/cdn/${props.ddVersion}/img/profileicon/${iconId}.png`
}
</script>
