<template>
  <section class="px-4 desk:px-14 pb-14 desk:pb-24" data-testid="landing-cta">
    <div class="py-7 px-5 desk:p-16 rounded-xl desk:rounded-2xl bg-background-surface grid grid-cols-1 desk:grid-cols-[minmax(0,1fr)_auto] gap-4 desk:gap-10 items-center">
      <div class="flex flex-col gap-3.5">
        <h2 class="font-display font-semibold text-[1.75rem] leading-[1.15] desk:text-headline text-text max-w-[45rem]">Find the one fix in your next match</h2>
        <p class="text-body desk:text-body-lg text-text-soft max-w-[35rem]">
          Create a free account, link your Riot ID, and your Overview is ready in a few minutes.
        </p>
      </div>
      <div class="flex flex-col desk:flex-row desk:items-center gap-1 desk:gap-3">
        <router-link to="/auth?mode=signup" class="mp-btn mp-btn--primary h-[3.25rem] text-base" data-testid="cta-signup">
          Create free account
          <BaseIcon name="arrow-right" :size="20" />
        </router-link>
        <router-link to="/auth?mode=login" class="mp-btn mp-btn--ghost h-11 desk:h-[3.25rem] text-base" data-testid="cta-login">
          Log in
        </router-link>
      </div>
    </div>
  </section>
</template>

<script setup>
import { BaseIcon } from '@/components/base'
</script>
