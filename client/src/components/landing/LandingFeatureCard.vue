<template>
  <article class="p-5 desk:p-7 rounded-xl bg-background-surface flex flex-col gap-3.5 desk:gap-5" :data-testid="`feature-${page.toLowerCase().replace(/\s+/g, '-')}`">
    <div class="flex items-center justify-between">
      <span class="inline-flex items-center gap-1.5 h-7 px-3 rounded-pill bg-background-selected text-chip text-text" data-testid="feature-page-tag">
        <BaseIcon :name="icon" :size="16" />
        {{ page }}
      </span>
      <span class="text-chip text-text-secondary">Example</span>
    </div>
    <div class="flex flex-col gap-2">
      <h3 class="font-display font-semibold text-title desk:text-xl desk:leading-tight text-text">{{ title }}</h3>
      <p class="text-body text-text-soft max-w-[32.5rem]">{{ description }}</p>
    </div>
    <slot />
  </article>
</template>

<script setup>
import { BaseIcon } from '@/components/base'

defineProps({
  /** Lucide icon of the page, from the navigation vocabulary */
  icon: {
    type: String,
    required: true
  },
  /** App page the feature lives on */
  page: {
    type: String,
    required: true
  },
  title: {
    type: String,
    required: true
  },
  description: {
    type: String,
    required: true
  }
})
</script>
