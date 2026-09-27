<template>
  <section id="how-it-works" class="scroll-mt-14 desk:scroll-mt-20 px-4 desk:px-14 pb-16 desk:pb-28 flex flex-col gap-5 desk:gap-10" data-testid="landing-steps">
    <div class="flex flex-col gap-2.5 desk:gap-3">
      <span class="text-eyebrow text-text-secondary">HOW IT WORKS</span>
      <h2 class="font-display font-semibold text-[1.875rem] leading-[1.15] desk:text-headline text-text">Your Overview in three steps</h2>
    </div>
    <ol class="grid grid-cols-1 desk:grid-cols-3 gap-3 desk:gap-5">
      <li
        v-for="(step, i) in steps"
        :key="step.title"
        class="p-5 desk:p-7 rounded-xl bg-background-surface grid grid-cols-[2.75rem_minmax(0,1fr)] desk:flex desk:flex-col gap-3.5"
        data-testid="landing-step"
      >
        <div class="flex items-center justify-between">
          <span class="w-11 h-11 desk:w-12 desk:h-12 rounded-pill bg-background-selected flex items-center justify-center text-text">
            <BaseIcon :name="step.icon" :size="24" />
          </span>
          <span class="hidden desk:block font-display font-bold text-score-lg text-track" aria-hidden="true">0{{ i + 1 }}</span>
        </div>
        <div class="flex flex-col gap-1 desk:gap-3.5">
          <h3 class="font-display font-semibold text-lg desk:text-title text-text">
            <span class="visually-hidden">Step {{ i + 1 }}: </span>{{ step.title }}
          </h3>
          <p class="text-body-sm desk:text-body text-text-soft">{{ step.body }}</p>

          <div
            v-if="step.id === 'link'"
            class="hidden desk:flex h-11 px-4 rounded-pill bg-background border border-border items-center gap-1 text-body-sm"
            aria-hidden="true"
          >
            <span class="text-text-secondary">Game name</span>
            <span class="flex-grow" />
            <span class="h-7 inline-flex items-center px-2.5 rounded-pill bg-background-selected text-chip text-text">#EUW</span>
          </div>

          <div v-if="step.id === 'sync'" class="flex flex-col gap-2 mt-1">
            <div class="flex justify-between text-caption">
              <span class="text-text-soft">Syncing 12 of 40 matches</span>
              <span class="font-display font-bold text-text">30%</span>
            </div>
            <div
              class="h-1.5 rounded-[2px] bg-track"
              role="progressbar"
              aria-label="Example sync progress"
              aria-valuemin="0"
              aria-valuemax="40"
              aria-valuenow="12"
            >
              <div class="h-1.5 w-[30%] rounded-[2px] bg-primary-accent" />
            </div>
          </div>
        </div>
      </li>
    </ol>
  </section>
</template>

<script setup>
import { BaseIcon } from '@/components/base'

const steps = [
  { id: 'account', icon: 'user', title: 'Create your account', body: 'Sign up with your email. It is free, and there is no card to enter.' },
  { id: 'link', icon: 'link', title: 'Link your Riot ID', body: 'Add your Riot ID, like Game name #EUW, so we can find your matches.' },
  { id: 'sync', icon: 'refresh-cw', title: 'We sync your matches', body: 'Your recent matches come in over a few minutes, and your Overview fills in as they arrive.' }
]
</script>
