<template>
  <nav aria-label="Main" class="mp-tabbar desk:hidden" data-testid="app-tabbar">
    <router-link
      v-for="item in navItems"
      :key="item.to"
      v-slot="{ href, navigate }"
      :to="item.to"
      custom
    >
      <!-- Current on sub-paths too: /app/matches/<id> keeps Matches current -->
      <a
        :href="href"
        class="mp-focusable"
        :aria-current="isCurrent(item.to) ? 'page' : undefined"
        :data-testid="item.testId"
        @click="navigate"
      >
        <BaseIcon :name="item.icon" :size="24" />
        <span>{{ item.label }}</span>
      </a>
    </router-link>
  </nav>
</template>

<script setup>
import { useRoute } from 'vue-router';
import { BaseIcon } from '@/components/base';

const route = useRoute();

function isCurrent(to) {
  return route.path === to || route.path.startsWith(`${to}/`);
}

const navItems = [
  { to: '/app/overview', label: 'Overview', icon: 'house', testId: 'tab-overview' },
  { to: '/app/champion-select', label: 'Champ Select', icon: 'shield', testId: 'tab-champion-select' },
  { to: '/app/matches', label: 'Matches', icon: 'swords', testId: 'tab-matches' },
  { to: '/app/solo', label: 'Solo', icon: 'chart-line', testId: 'tab-solo' }
];
</script>
