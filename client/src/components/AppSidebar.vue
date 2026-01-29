<template>
  <aside
    class="fixed top-0 left-0 h-screen bg-background-surface border-r border-border backdrop-blur-[10px] flex flex-col transition-[width] duration-300 ease-out z-[100]"
    :class="isCollapsed ? 'w-16 overflow-visible' : 'w-64'"
    data-testid="app-sidebar"
    :data-collapsed="isCollapsed"
  >
    <!-- Logo / Toggle Section -->
    <div class="flex items-center justify-between px-md py-lg border-b border-border min-h-[64px]">
      <router-link to="/app/overview" class="flex items-center gap-sm no-underline text-text flex-1 min-w-0">
        <img src="/mongoose.png" alt="Mongoose" class="w-8 h-4 shrink-0" />
        <Transition name="fade">
          <span v-if="!isCollapsed" class="text-base font-bold tracking-tight whitespace-nowrap">Mongoose.gg <span class="text-[0.5em] text-text-secondary font-normal align-top">Beta</span></span>
        </Transition>
      </router-link>
      <button
        @click="toggleSidebar"
        class="bg-transparent border-none text-text-secondary cursor-pointer p-xs rounded-sm flex items-center justify-center transition-all duration-200 shrink-0 hover:bg-background-elevated hover:text-text"
        :title="isCollapsed ? 'Expand sidebar' : 'Collapse sidebar'"
      >
        <!-- Chevron right (>) when collapsed, chevron left (<) when expanded -->
        <svg v-if="isCollapsed" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-5 h-5">
          <path fill-rule="evenodd" d="M8.22 5.22a.75.75 0 0 1 1.06 0l4.25 4.25a.75.75 0 0 1 0 1.06l-4.25 4.25a.75.75 0 0 1-1.06-1.06L11.94 10 8.22 6.28a.75.75 0 0 1 0-1.06Z" clip-rule="evenodd" />
        </svg>
        <svg v-else xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-5 h-5">
          <path fill-rule="evenodd" d="M11.78 5.22a.75.75 0 0 1 0 1.06L8.06 10l3.72 3.72a.75.75 0 1 1-1.06 1.06l-4.25-4.25a.75.75 0 0 1 0-1.06l4.25-4.25a.75.75 0 0 1 1.06 0Z" clip-rule="evenodd" />
        </svg>
      </button>
    </div>

    <!-- Navigation Items -->
    <nav class="flex-1 py-md overflow-y-auto overflow-x-hidden" :class="{ 'overflow-visible': isCollapsed }">
      <router-link
        to="/app/overview"
        class="nav-item flex items-center gap-md p-md mx-sm text-text-secondary no-underline rounded-md cursor-pointer whitespace-nowrap hover:bg-background-elevated hover:text-text"
        :title="isCollapsed ? 'Overview' : ''"
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="nav-icon w-5 h-5 shrink-0">
          <path d="M10.707 2.293a1 1 0 00-1.414 0l-7 7a1 1 0 001.414 1.414L4 10.414V17a1 1 0 001 1h2a1 1 0 001-1v-2a1 1 0 011-1h2a1 1 0 011 1v2a1 1 0 001 1h2a1 1 0 001-1v-6.586l.293.293a1 1 0 001.414-1.414l-7-7z" />
        </svg>
        <span v-if="!isCollapsed" class="nav-label text-sm font-medium tracking-tight">Overview</span>
      </router-link>

      <router-link
        to="/app/champion-select"
        class="nav-item flex items-center gap-md p-md mx-sm text-text-secondary no-underline rounded-md cursor-pointer whitespace-nowrap hover:bg-background-elevated hover:text-text"
        :title="isCollapsed ? 'Champion Select' : ''"
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="nav-icon w-5 h-5 shrink-0">
          <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.857-9.809a.75.75 0 00-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 10-1.06 1.061l2.5 2.5a.75.75 0 001.137-.089l4-5.5z" clip-rule="evenodd" />
        </svg>
        <span v-if="!isCollapsed" class="nav-label text-sm font-medium tracking-tight">Champion Select</span>
      </router-link>

      <router-link
        to="/app/matches"
        class="nav-item flex items-center gap-md p-md mx-sm text-text-secondary no-underline rounded-md cursor-pointer whitespace-nowrap hover:bg-background-elevated hover:text-text"
        :title="isCollapsed ? 'Matches' : ''"
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="nav-icon w-5 h-5 shrink-0">
          <path fill-rule="evenodd" d="M6 2a2 2 0 00-2 2v12a2 2 0 002 2h8a2 2 0 002-2V4a2 2 0 00-2-2H6zm1 2a1 1 0 000 2h6a1 1 0 100-2H7zm6 7a1 1 0 011 1v3a1 1 0 11-2 0v-3a1 1 0 011-1zm-3 3a1 1 0 100 2h.01a1 1 0 100-2H10zm-4 1a1 1 0 011-1h.01a1 1 0 110 2H7a1 1 0 01-1-1zm1-4a1 1 0 100 2h.01a1 1 0 100-2H7zm2 1a1 1 0 011-1h.01a1 1 0 110 2H10a1 1 0 01-1-1zm4-4a1 1 0 100 2h.01a1 1 0 100-2H13zM9 9a1 1 0 011-1h.01a1 1 0 110 2H10a1 1 0 01-1-1zM7 8a1 1 0 000 2h.01a1 1 0 000-2H7z" clip-rule="evenodd" />
        </svg>
        <span v-if="!isCollapsed" class="nav-label text-sm font-medium tracking-tight">Matches</span>
      </router-link>

      <!-- Analysis Section with Submenu -->
      <!-- Expanded: show inline submenu -->
      <div v-if="!isCollapsed" class="mb-xs" data-testid="nav-section-analysis">
        <div
          class="flex items-center gap-md p-md mx-sm text-text-secondary no-underline rounded-md transition-all duration-200 cursor-default whitespace-nowrap hover:bg-background-elevated hover:text-text"
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-5 h-5 shrink-0">
            <path d="M2 11a1 1 0 011-1h2a1 1 0 011 1v5a1 1 0 01-1 1H3a1 1 0 01-1-1v-5zM8 7a1 1 0 011-1h2a1 1 0 011 1v9a1 1 0 01-1 1H9a1 1 0 01-1-1V7zM14 4a1 1 0 011-1h2a1 1 0 011 1v12a1 1 0 01-1 1h-2a1 1 0 01-1-1V4z" />
          </svg>
          <span class="text-sm font-medium tracking-tight">Analysis</span>
        </div>
        <div class="ml-xl mt-xs">
          <router-link to="/app/solo" class="nav-subitem flex items-center py-sm px-md mx-sm text-text-secondary no-underline rounded-md transition-all duration-200 text-sm hover:bg-background-elevated hover:text-text" data-testid="nav-subitem-solo">
            <span class="font-medium tracking-tight">Solo</span>
          </router-link>
          <router-link to="/app/duo" class="nav-subitem flex items-center py-sm px-md mx-sm text-text-secondary no-underline rounded-md transition-all duration-200 text-sm hover:bg-background-elevated hover:text-text" data-testid="nav-subitem-duo">
            <span class="font-medium tracking-tight">Duo</span>
          </router-link>
          <router-link to="/app/team" class="nav-subitem flex items-center py-sm px-md mx-sm text-text-secondary no-underline rounded-md transition-all duration-200 text-sm hover:bg-background-elevated hover:text-text" data-testid="nav-subitem-team">
            <span class="font-medium tracking-tight">Team</span>
          </router-link>
        </div>
      </div>

      <!-- Collapsed: show popout menu using HeadlessUI Popover -->
      <Popover v-else class="relative mb-xs" data-testid="nav-section-analysis">
        <PopoverButton
          class="nav-item flex items-center gap-md p-md mx-sm text-text-secondary no-underline rounded-md cursor-pointer whitespace-nowrap hover:bg-background-elevated hover:text-text w-[calc(100%-1rem)] focus:outline-none"
        >
          <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="nav-icon w-5 h-5 shrink-0">
            <path d="M2 11a1 1 0 011-1h2a1 1 0 011 1v5a1 1 0 01-1 1H3a1 1 0 01-1-1v-5zM8 7a1 1 0 011-1h2a1 1 0 011 1v9a1 1 0 01-1 1H9a1 1 0 01-1-1V7zM14 4a1 1 0 011-1h2a1 1 0 011 1v12a1 1 0 01-1 1h-2a1 1 0 01-1-1V4z" />
          </svg>
        </PopoverButton>

        <transition
          enter-active-class="transition duration-200 ease-out"
          enter-from-class="opacity-0 -translate-x-2"
          enter-to-class="opacity-100 translate-x-0"
          leave-active-class="transition duration-150 ease-in"
          leave-from-class="opacity-100 translate-x-0"
          leave-to-class="opacity-0 -translate-x-2"
        >
          <PopoverPanel
            class="absolute left-full top-0 min-w-[140px] bg-background-surface border border-border rounded-md p-xs shadow-[0_4px_12px_rgba(0,0,0,0.3)] z-[100] ml-xs focus:outline-none"
            v-slot="{ close }"
          >
            <div class="py-sm px-md text-xs font-semibold text-text-secondary uppercase tracking-wider border-b border-border mb-xs">Analysis</div>
            <router-link to="/app/solo" class="popout-item block py-sm px-md text-text-secondary no-underline text-sm font-medium rounded-sm transition-all duration-150 hover:bg-background-elevated hover:text-text" @click="close" data-testid="popout-item-solo">Solo</router-link>
            <router-link to="/app/duo" class="popout-item block py-sm px-md text-text-secondary no-underline text-sm font-medium rounded-sm transition-all duration-150 hover:bg-background-elevated hover:text-text" @click="close" data-testid="popout-item-duo">Duo</router-link>
            <router-link to="/app/team" class="popout-item block py-sm px-md text-text-secondary no-underline text-sm font-medium rounded-sm transition-all duration-150 hover:bg-background-elevated hover:text-text" @click="close" data-testid="popout-item-team">Team</router-link>
          </PopoverPanel>
        </transition>
      </Popover>

      <router-link
        to="/app/goals"
        class="nav-item flex items-center gap-md p-md mx-sm text-text-secondary no-underline rounded-md cursor-pointer whitespace-nowrap hover:bg-background-elevated hover:text-text"
        :title="isCollapsed ? 'Goals' : ''"
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="nav-icon w-5 h-5 shrink-0">
          <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
        </svg>
        <span v-if="!isCollapsed" class="nav-label text-sm font-medium tracking-tight">Goals</span>
      </router-link>
    </nav>

    <!-- User Section at Bottom -->
    <div class="border-t border-border py-md">
      <router-link
        to="/app/user"
        class="user-item flex items-center gap-md p-md mx-sm text-text no-underline rounded-md transition-all duration-200 whitespace-nowrap hover:bg-background-elevated"
        :title="isCollapsed ? (hasLinkedAccount ? riotAccountName : username) : ''"
      >
        <!-- Profile Icon with Level Badge -->
        <div
          class="relative rounded-full overflow-visible bg-background-surface flex items-center justify-center shrink-0 border-2 border-primary transition-[width,height] duration-300 ease-out"
          :class="isCollapsed ? 'w-11 h-11' : 'w-[52px] h-[52px]'"
        >
          <img
            v-if="linkedAccountIconUrl"
            :src="linkedAccountIconUrl"
            :alt="`${hasLinkedAccount ? riotAccountName : username} profile icon`"
            class="w-full h-full object-cover rounded-full"
            @error="handleLinkedIconError"
          />
          <svg v-else xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor" class="w-6 h-6 text-text-secondary">
            <path fill-rule="evenodd" d="M7.5 6a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM3.751 20.105a8.25 8.25 0 0116.498 0 .75.75 0 01-.437.695A18.683 18.683 0 0112 22.5c-2.786 0-5.433-.608-7.812-1.7a.75.75 0 01-.437-.695z" clip-rule="evenodd" />
          </svg>
          <span
            v-if="summonerLevel"
            class="absolute -bottom-0.5 -right-0.5 bg-primary text-white font-bold rounded-[10px] text-center leading-none"
            :class="isCollapsed ? 'text-[10px] py-0.5 px-[5px] min-w-[20px]' : 'text-[11px] py-[3px] px-1.5 min-w-[24px]'"
          >{{ summonerLevel }}</span>
        </div>

        <!-- User Info (expanded only) -->
        <Transition name="fade">
          <div v-if="!isCollapsed" class="flex flex-col gap-0.5 min-w-0 flex-1">
            <!-- Riot Account Info -->
            <template v-if="hasLinkedAccount">
              <span class="text-sm font-semibold text-text overflow-hidden text-ellipsis whitespace-nowrap">{{ riotAccountName }}</span>
              <span class="text-xs text-text-secondary uppercase tracking-wider">{{ regionLabel }}</span>
            </template>
            <template v-else>
              <span class="text-sm font-semibold text-text overflow-hidden text-ellipsis whitespace-nowrap">{{ username }}</span>
              <span class="text-xs text-text-secondary">No account linked</span>
            </template>
          </div>
        </Transition>
      </router-link>

      <!-- Version Badge -->
      <Transition name="fade">
        <div v-if="!isCollapsed" class="py-sm px-md text-center text-xs text-[#6b7280] border-t border-border mt-sm">
          v{{ version }}
        </div>
      </Transition>
    </div>
  </aside>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { Popover, PopoverButton, PopoverPanel } from '@headlessui/vue';
import { useAuthStore } from '../stores/authStore';
import { useUiStore } from '../stores/uiStore';
import pkg from '../../package.json';

const authStore = useAuthStore();
const uiStore = useUiStore();
const version = pkg.version || '0.0.0';

// Local state
const linkedIconError = ref(false);

// Sidebar state from store
const isCollapsed = computed(() => uiStore.isSidebarCollapsed);

// Data Dragon version for profile icons
const ddVersion = '16.1.1';

// Region labels for display
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
};

// Initialize sidebar state
onMounted(() => {
  uiStore.initializeSidebar();
  window.addEventListener('resize', uiStore.handleResize);
});

onUnmounted(() => {
  window.removeEventListener('resize', uiStore.handleResize);
});

// Toggle sidebar
function toggleSidebar() {
  uiStore.toggleSidebar();
}

// User data
const username = computed(() => authStore.username || 'User');

// Profile icon from first Riot account
const primaryRiotAccount = computed(() => authStore.primaryRiotAccount);

// Linked Riot Account data
const hasLinkedAccount = computed(() => authStore.hasLinkedAccount);

const riotAccountName = computed(() => {
  const account = primaryRiotAccount.value;
  if (!account) return '';
  return `${account.gameName}#${account.tagLine}`;
});

const summonerLevel = computed(() => primaryRiotAccount.value?.summonerLevel);

const regionLabel = computed(() => {
  const region = primaryRiotAccount.value?.region;
  return region ? (regionLabels[region] || region.toUpperCase()) : '';
});

const linkedAccountIconUrl = computed(() => {
  const profileIconId = primaryRiotAccount.value?.profileIconId;
  if (!profileIconId || linkedIconError.value) return null;
  return `https://ddragon.leagueoflegends.com/cdn/${ddVersion}/img/profileicon/${profileIconId}.png`;
});

function handleLinkedIconError() {
  linkedIconError.value = true;
}
</script>


<style scoped>
/* Vue Transition classes for fade animation */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

/* Nav item transitions - only for hover states, not layout */
.nav-item {
  transition: background-color 0.2s ease, color 0.2s ease;
}

/* Nav label smooth hide/show - synced with sidebar width transition */
.nav-label {
  opacity: 1;
  transition: opacity 0.15s ease-out;
}

/* Router-link-active state styles */
.nav-item.router-link-active {
  background: var(--color-primary-soft);
  color: var(--color-primary);
}

.nav-subitem.router-link-active {
  background: var(--color-primary-soft);
  color: var(--color-primary);
}

.popout-item.router-link-active {
  background: var(--color-primary-soft);
  color: var(--color-primary);
}

.user-item.router-link-active {
  background: var(--color-primary-soft);
}

/* Scrollbar styling */
nav::-webkit-scrollbar {
  width: 4px;
}

nav::-webkit-scrollbar-track {
  background: transparent;
}

nav::-webkit-scrollbar-thumb {
  background: var(--color-border);
  border-radius: 2px;
}

nav::-webkit-scrollbar-thumb:hover {
  background: var(--color-text-secondary);
}
</style>
