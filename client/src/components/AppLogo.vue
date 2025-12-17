<template>
  <!-- Logo uses currentColor so parent can control color -->
  <svg
    :width="size"
    :height="size"
    viewBox="0 0 64 64"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
    aria-hidden="true"
  >
    <defs>
      <linearGradient id="grad" x1="0" y1="0" x2="64" y2="64" gradientUnits="userSpaceOnUse">
        <stop offset="0" />
        <stop offset="1" stop-opacity="0.85" />
      </linearGradient>
    </defs>
    <!-- Crest outline -->
    <path
      d="M32 6c8.8 0 20 3.3 20 12.5V34c0 8.7-7.9 15.9-20 24-12.1-8.1-20-15.3-20-24V18.5C12 9.3 23.2 6 32 6Z"
      stroke="currentColor"
      stroke-width="2.5"
      fill="url(#grad)"
    />
    <!-- Bars for statistics -->
    <rect x="20" y="28" width="6" height="18" rx="1.5" fill="currentColor" />
    <rect x="29" y="22" width="6" height="24" rx="1.5" fill="currentColor" opacity="0.85" />
    <rect x="38" y="32" width="6" height="14" rx="1.5" fill="currentColor" opacity="0.7" />
    <!-- Subtle divider -->
    <path d="M18 18h28" stroke="currentColor" stroke-opacity="0.4" stroke-width="2" />
  </svg>
</template>

<script setup>
const props = defineProps({
  size: { type: [Number, String], default: 64 }
})
</script>

<style scoped>
/* No component-scoped styles; parent controls color/size */
</style>
