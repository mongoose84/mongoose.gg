<template>
  <div class="app-container">
    <RouterView />
    <VersionBadge />
  </div>
</template>

<script setup>
import { RouterView } from 'vue-router'
import VersionBadge from './components/VersionBadge.vue'
</script>

<style scoped>
.app-container {
  min-height: 100vh;
}
</style>
