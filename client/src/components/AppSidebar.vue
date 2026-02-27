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
        data-testid="nav-overview"
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
        data-testid="nav-champion-select"
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
        data-testid="nav-matches"
        class="nav-item flex items-center gap-md p-md mx-sm text-text-secondary no-underline rounded-md cursor-pointer whitespace-nowrap hover:bg-background-elevated hover:text-text"
        :title="isCollapsed ? 'Matches' : ''"
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="nav-icon w-5 h-5 shrink-0">
          <path fill-rule="evenodd" d="M6 2a2 2 0 00-2 2v12a2 2 0 002 2h8a2 2 0 002-2V4a2 2 0 00-2-2H6zm1 2a1 1 0 000 2h6a1 1 0 100-2H7zm6 7a1 1 0 011 1v3a1 1 0 11-2 0v-3a1 1 0 011-1zm-3 3a1 1 0 100 2h.01a1 1 0 100-2H10zm-4 1a1 1 0 011-1h.01a1 1 0 110 2H7a1 1 0 01-1-1zm1-4a1 1 0 100 2h.01a1 1 0 100-2H7zm2 1a1 1 0 011-1h.01a1 1 0 110 2H10a1 1 0 01-1-1zm4-4a1 1 0 100 2h.01a1 1 0 100-2H13zM9 9a1 1 0 011-1h.01a1 1 0 110 2H10a1 1 0 01-1-1zM7 8a1 1 0 000 2h.01a1 1 0 000-2H7z" clip-rule="evenodd" />
        </svg>
        <span v-if="!isCollapsed" class="nav-label text-sm font-medium tracking-tight">Matches</span>
        <!-- Analysis in progress indicator - only when expanded and running -->
        <span
          v-if="!isCollapsed && isAnalysisRunning"
          class="analysis-indicator ml-auto"
          role="status"
          aria-live="polite"
          aria-label="Analysis in progress"
        />
      </router-link>

      <router-link
        to="/app/solo"
        data-testid="nav-solo"
        class="nav-item flex items-center gap-md p-md mx-sm text-text-secondary no-underline rounded-md cursor-pointer whitespace-nowrap hover:bg-background-elevated hover:text-text"
        :title="isCollapsed ? 'Solo Stats' : ''"
      >
        <ChartBarIcon class="nav-icon w-5 h-5 shrink-0" />
        <span v-if="!isCollapsed" class="nav-label text-sm font-medium tracking-tight">Solo Stats</span>
      </router-link>

      <router-link
        to="/app/team"
        data-testid="nav-team"
        class="nav-item flex items-center gap-md p-md mx-sm text-text-secondary no-underline rounded-md cursor-pointer whitespace-nowrap hover:bg-background-elevated hover:text-text"
        :title="isCollapsed ? 'Team Analytics (Pro)' : ''"
      >
        <UserGroupIcon class="nav-icon w-5 h-5 shrink-0" />
        <span v-if="!isCollapsed" class="nav-label text-sm font-medium tracking-tight">Team Analytics</span>
        <span v-if="!isCollapsed" class="pro-badge">PRO</span>
      </router-link>

      <router-link
        to="/app/goals"
        data-testid="nav-goals"
        class="nav-item flex items-center gap-md p-md mx-sm text-text-secondary no-underline rounded-md cursor-pointer whitespace-nowrap hover:bg-background-elevated hover:text-text"
        :title="isCollapsed ? 'Goals (Pro)' : ''"
      >
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="nav-icon w-5 h-5 shrink-0">
          <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
        </svg>
        <span v-if="!isCollapsed" class="nav-label text-sm font-medium tracking-tight">Goals</span>
        <span v-if="!isCollapsed" class="pro-badge">PRO</span>
      </router-link>
    </nav>

    <!-- Feedback Link - above user section -->
    <div class="border-t border-border py-sm">
      <router-link
        to="/app/feedback"
        data-testid="nav-feedback"
        class="nav-item flex items-center gap-md p-md mx-sm text-text-secondary no-underline rounded-md cursor-pointer whitespace-nowrap hover:bg-background-elevated hover:text-text"
        :title="isCollapsed ? 'Feedback' : ''"
      >
        <ChatBubbleLeftEllipsisIcon class="nav-icon w-5 h-5 shrink-0" />
        <span v-if="!isCollapsed" class="nav-label text-sm font-medium tracking-tight">Feedback</span>
      </router-link>
    </div>

    <!-- User Section at Bottom -->
    <div class="border-t border-border py-md">
      <router-link
        to="/app/user"
        class="user-item flex items-center gap-md mx-sm text-text no-underline rounded-md whitespace-nowrap hover:bg-background-elevated"
        :class="isCollapsed ? 'justify-center py-sm px-xs' : 'p-md'"
        :title="isCollapsed ? (hasLinkedAccount ? riotAccountName : username) : ''"
      >
        <!-- Profile Icon with Level Badge -->
        <div
          class="relative rounded-full overflow-visible bg-background-surface flex items-center justify-center shrink-0 border-2 border-primary transition-[width,height] duration-300 ease-out"
          :class="isCollapsed ? 'w-9 h-9' : 'w-[52px] h-[52px]'"
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
import { ChatBubbleLeftEllipsisIcon, ChartBarIcon, UserGroupIcon } from '@heroicons/vue/24/outline';
import { useAuthStore } from '../stores/authStore';
import { useUiStore } from '../stores/uiStore';
import { useAnalysisStatus } from '../composables/useAnalysisStatus';
import pkg from '../../package.json';

const authStore = useAuthStore();
const uiStore = useUiStore();
const { isRunning: isAnalysisRunning } = useAnalysisStatus();
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

.pro-badge {
  margin-left: auto;
  font-size: 10px;
  line-height: 1;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  padding: 3px 6px;
  border-radius: var(--border-radius-sm);
  background: var(--color-primary-soft);
  color: var(--color-primary);
}

/* Router-link-active state styles */
.nav-item.router-link-active {
  background: var(--color-primary-soft);
  color: var(--color-primary);
}

.user-item {
  transition: background-color 0.2s ease, padding 0.3s ease-out;
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

/* Analysis in progress indicator */
.analysis-indicator {
  display: inline-block;
  width: 12px;
  height: 12px;
  border: 2px solid rgba(59, 130, 246, 0.3);
  border-radius: 50%;
  border-top-color: #3b82f6;
  animation: analysis-spin 0.8s linear infinite;
  flex-shrink: 0;
}

@keyframes analysis-spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
