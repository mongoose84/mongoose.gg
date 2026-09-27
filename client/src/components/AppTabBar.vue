<template>
  <nav aria-label="Main" class="mp-tabbar desk:hidden" data-testid="app-tabbar">
    <router-link
      v-for="item in navItems"
      :key="item.to"
      :to="item.to"
      class="mp-focusable"
      :data-testid="item.testId"
    >
      <BaseIcon :name="item.icon" :size="24" />
      <span>{{ item.label }}</span>
    </router-link>
  </nav>
</template>

<script setup>
import { BaseIcon } from '@/components/base';

const navItems = [
  { to: '/app/overview', label: 'Overview', icon: 'house', testId: 'tab-overview' },
  { to: '/app/champion-select', label: 'Champ Select', icon: 'shield', testId: 'tab-champion-select' },
  { to: '/app/matches', label: 'Matches', icon: 'swords', testId: 'tab-matches' },
  { to: '/app/solo', label: 'Solo', icon: 'chart-line', testId: 'tab-solo' }
];
</script>
