<template>
  <div class="min-h-screen bg-transparent pt-16">
    <NavBar />

    <div class="max-w-[800px] mx-auto py-2xl px-xl">
      <div class="bg-background-surface border border-border rounded-lg p-2xl backdrop-blur-[10px]" data-testid="legal-content">
        <h1 class="text-2xl font-bold tracking-tight mb-sm text-text">Privacy Policy</h1>
        <p class="text-sm text-text-secondary mb-2xl">Last updated: {{ currentDate }}</p>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Overview</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">Mongoose.gg ("we", "our", or "us") is committed to protecting your privacy. This Privacy Policy explains how we collect, use, and share information about you when you use our service.</p>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Information We Collect</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">We collect the following types of information:</p>
          <ul class="list-disc ml-xl text-text-secondary">
            <li class="text-base leading-relaxed mb-xs">Account information (email, username)</li>
            <li class="text-base leading-relaxed mb-xs">Riot Games account data (summoner name, match history)</li>
            <li class="text-base leading-relaxed mb-xs">Usage data and analytics</li>
            <li class="text-base leading-relaxed mb-xs">Payment information (processed securely through Mollie)</li>
          </ul>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">How We Use Your Information</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">We use your information to:</p>
          <ul class="list-disc ml-xl text-text-secondary">
            <li class="text-base leading-relaxed mb-xs">Provide and improve our services</li>
            <li class="text-base leading-relaxed mb-xs">Generate personalized insights and recommendations</li>
            <li class="text-base leading-relaxed mb-xs">Process payments and manage subscriptions</li>
            <li class="text-base leading-relaxed mb-xs">Communicate with you about updates and features</li>
          </ul>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Data Security</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">We implement industry-standard security measures to protect your data. However, no method of transmission over the internet is 100% secure.</p>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Your Rights</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">You have the right to access, update, or delete your personal data. Contact us at privacy@mongoose.gg for any requests.</p>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Contact Us</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">If you have questions about this Privacy Policy, please contact us at <a href="mailto:privacy@mongoose.gg" class="text-primary no-underline transition-opacity duration-200 hover:opacity-80">privacy@mongoose.gg</a>.</p>
        </section>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import NavBar from '../components/NavBar.vue';

const currentDate = computed(() => {
  return new Date().toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  });
});
</script>
