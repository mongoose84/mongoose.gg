<template>
  <footer class="border-t border-border" data-testid="landing-footer">
    <div class="max-w-[1440px] mx-auto px-4 desk:px-14 pt-7 desk:pt-9 pb-9 desk:pb-11 flex flex-col gap-4 desk:gap-5 text-body-sm text-text-secondary">
      <div class="flex flex-col desk:flex-row desk:items-center gap-2 desk:gap-7">
        <span class="flex items-center gap-2.5 font-display font-bold text-base text-text">
          <img src="/mongoose.png" alt="" class="hidden desk:block h-7 w-[4.375rem] object-contain" />
          Mongoose.gg
        </span>
        <span class="hidden desk:block flex-grow" />
        <nav aria-label="Legal and contact" class="flex flex-wrap gap-x-5 gap-y-1">
          <router-link
            v-for="link in links"
            :key="link.label"
            :to="link.to"
            class="mp-focusable flex items-center h-11 desk:h-auto text-text-secondary no-underline hover:text-text"
          >{{ link.label }}</router-link>
          <a href="mailto:hello@mongoose.gg" class="mp-focusable flex items-center h-11 desk:h-auto text-text-secondary no-underline hover:text-text">Contact</a>
        </nav>
      </div>
      <p class="max-w-[56.25rem] text-caption text-text-faint">
        Not affiliated with Riot Games. Mongoose.gg isn't endorsed by Riot Games and doesn't reflect the views or opinions of Riot Games or anyone officially involved in producing or managing Riot Games properties. League of Legends is a trademark of Riot Games, Inc.
      </p>
      <p class="text-caption text-text-faint">© {{ currentYear }} Mongoose.gg</p>
    </div>
  </footer>
</template>

<script setup>
const currentYear = new Date().getFullYear()

const links = [
  { label: 'Privacy', to: '/privacy' },
  { label: 'Cookies', to: '/cookies' },
  { label: 'Terms', to: '/terms' }
]
</script>
