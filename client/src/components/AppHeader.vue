<template>
  <header class="fixed top-0 left-0 right-0 z-50 bg-background border-b border-border h-14 desk:h-20" data-testid="app-header">
    <div class="max-w-[1328px] mx-auto h-full px-4 desk:px-14 flex items-center gap-4 desk:gap-10">
      <!-- Logo - always returns to the Overview -->
      <router-link
        to="/app/overview"
        class="mp-focusable flex items-center gap-2.5 rounded-pill no-underline font-display font-bold text-[1.0625rem] desk:text-title text-text shrink-0"
        aria-label="Mongoose.gg home"
        data-testid="app-header-logo"
      >
        <img src="/mongoose.png" alt="" class="h-[1.875rem] w-[4.625rem] desk:h-10 desk:w-[6.1875rem] object-contain" />
        Mongoose.gg
      </router-link>

      <!-- Desktop PillNav -->
      <nav aria-label="Main" class="hidden desk:flex mp-nav mx-auto">
        <router-link
          v-for="item in navItems"
          :key="item.to"
          :to="item.to"
          :data-testid="item.testId"
        >{{ item.label }}</router-link>
      </nav>

      <div class="flex-grow desk:hidden" />

      <!-- Avatar menu -->
      <div class="relative ml-auto desk:ml-0" ref="menuRootRef">
        <button
          type="button"
          class="mp-focusable w-11 h-11 rounded-full overflow-hidden flex items-center justify-center bg-background-elevated text-text-secondary shrink-0"
          :aria-label="`Account menu, ${username}`"
          :aria-expanded="isMenuOpen"
          aria-controls="app-header-menu"
          ref="avatarRef"
          data-testid="app-header-avatar"
          @click="toggleMenu"
        >
          <img
            v-if="userIconUrl && !userIconError"
            :src="userIconUrl"
            alt=""
            class="w-full h-full object-cover"
            @error="handleUserIconError"
          />
          <BaseIcon v-else name="user" :size="24" />
        </button>

        <Transition name="menu">
          <div
            v-if="isMenuOpen"
            id="app-header-menu"
            ref="menuRef"
            class="absolute right-0 top-full mt-2 w-64 rounded-lg border border-border overflow-hidden z-50"
            style="background: var(--color-surface);"
            data-testid="app-header-menu"
            @keydown="handleMenuKeydown"
          >
            <template v-if="showAccountSwitcher">
              <div class="px-md pt-sm pb-xs text-3xs uppercase tracking-wide text-text-secondary" data-testid="app-header-accounts-label">Accounts</div>
              <AccountDropdownList
                :accounts="riotAccounts"
                :active-account-puuid="activeAccountPuuid"
                :show-overall="canUseOverallAccountView"
                :focused-index="-1"
                @select="handleAccountSelect"
              />
              <div class="border-t border-border my-xs" />
            </template>

            <router-link to="/app/user" class="menu-item mp-focusable" data-testid="app-header-settings" @click="closeMenu">
              <BaseIcon name="settings" :size="20" />
              Settings
            </router-link>
            <router-link to="/app/feedback" class="menu-item mp-focusable" data-testid="nav-feedback" @click="closeMenu">
              <BaseIcon name="message-square" :size="20" />
              Feedback
            </router-link>
            <button type="button" class="menu-item mp-focusable w-full" data-testid="app-header-logout" @click="handleLogout">
              <BaseIcon name="log-out" :size="20" />
              Log out
            </button>
          </div>
        </Transition>
      </div>
    </div>
  </header>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import { useUserIcon } from '@/composables/useUserIcon';
import { BaseIcon } from '@/components/base';
import AccountDropdownList from '@/components/header/AccountDropdownList.vue';
import { trackAuth } from '../services/analyticsApi';

const authStore = useAuthStore();
const route = useRoute();
const router = useRouter();
const { userIconUrl } = useUserIcon();
const userIconError = ref(false);

const isMenuOpen = ref(false);
const menuRootRef = ref(null);
const menuRef = ref(null);
const avatarRef = ref(null);

const navItems = [
  { to: '/app/overview', label: 'Overview', testId: 'nav-overview' },
  { to: '/app/champion-select', label: 'Champion Select', testId: 'nav-champion-select' },
  { to: '/app/matches', label: 'Matches', testId: 'nav-matches' },
  { to: '/app/solo', label: 'Solo', testId: 'nav-solo' }
];

const username = computed(() => authStore.username || 'User');
const riotAccounts = computed(() => authStore.riotAccounts);
const activeAccountPuuid = computed(() => authStore.activeAccountPuuid);
const canUseOverallAccountView = computed(() => authStore.canUseOverallAccountView);
const showAccountSwitcher = computed(() => riotAccounts.value.length > 1 || canUseOverallAccountView.value);

function handleUserIconError() {
  userIconError.value = true;
}

function toggleMenu() {
  isMenuOpen.value = !isMenuOpen.value;
}

function closeMenu() {
  isMenuOpen.value = false;
}

function handleAccountSelect(identifier) {
  authStore.setActiveAccount(identifier);
  closeMenu();
}

async function handleLogout() {
  try {
    await authStore.logout();
    trackAuth('logout', true);
  } catch (e) {
    console.error('Logout failed:', e);
    trackAuth('logout', false);
  } finally {
    closeMenu();
    router.push('/');
  }
}

function handleMenuKeydown(event) {
  if (event.key === 'Escape') {
    event.preventDefault();
    closeMenu();
    avatarRef.value?.focus();
  }
}

function handleClickOutside(event) {
  if (menuRootRef.value && !menuRootRef.value.contains(event.target)) {
    closeMenu();
  }
}

// Close the menu whenever the route changes
watch(() => route.fullPath, () => {
  closeMenu();
});

onMounted(() => {
  document.addEventListener('mousedown', handleClickOutside);
});

onUnmounted(() => {
  document.removeEventListener('mousedown', handleClickOutside);
});
</script>

<style scoped>
.menu-item {
  display: flex;
  align-items: center;
  gap: 0.625rem;
  height: 2.75rem;
  padding: 0 1rem;
  border: 0;
  background: transparent;
  color: var(--color-text-secondary);
  font-size: 0.875rem;
  font-weight: 500;
  text-align: left;
  text-decoration: none;
  cursor: pointer;
  transition: background-color 150ms ease-out, color 150ms ease-out;
}

.menu-item:hover {
  background: var(--color-elevated);
  color: var(--color-text);
}

.menu-enter-active,
.menu-leave-active {
  transition: opacity 150ms ease-out, transform 150ms ease-out;
}

.menu-enter-from,
.menu-leave-to {
  opacity: 0;
  transform: translateY(-4px) scale(0.98);
}

@media (prefers-reduced-motion: reduce) {
  .menu-item,
  .menu-enter-active,
  .menu-leave-active {
    transition: none;
  }
}
</style>
