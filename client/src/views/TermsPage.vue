<template>
  <div class="min-h-screen bg-transparent pt-16">
    <NavBar />

    <div class="max-w-[800px] mx-auto py-2xl px-xl">
      <div class="bg-background-surface border border-border rounded-lg p-2xl backdrop-blur-[10px]" data-testid="legal-content">
        <h1 class="text-2xl font-bold tracking-tight mb-sm text-text">Terms of Service</h1>
        <p class="text-sm text-text-secondary mb-2xl">Last updated: {{ currentDate }}</p>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Agreement to Terms</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">By accessing and using Mongoose.gg, you agree to be bound by these Terms of Service. If you disagree with any part of the terms, you may not access the service.</p>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Use License</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">We grant you a limited, non-exclusive, non-transferable license to use Mongoose.gg for personal, non-commercial purposes, subject to these terms.</p>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Riot Games Disclaimer</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">Mongoose.gg is not endorsed by Riot Games and does not reflect the views or opinions of Riot Games or anyone officially involved in producing or managing Riot Games properties. Riot Games and all associated properties are trademarks or registered trademarks of Riot Games, Inc.</p>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Subscription Terms</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">Subscription fees are billed in advance on a monthly or annual basis. You may cancel your subscription at any time. Cancellations take effect at the end of the current billing period.</p>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Refund Policy</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">We offer a 30-day money-back guarantee for all paid subscriptions. Contact support@mongoose.gg within 30 days of your initial purchase for a full refund.</p>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Prohibited Uses</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">You may not use Mongoose.gg for any unlawful purpose or to violate any laws. You may not attempt to gain unauthorized access to any part of the service.</p>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Limitation of Liability</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">In no event shall Mongoose.gg be liable for any indirect, incidental, special, consequential, or punitive damages resulting from your use or inability to use the service.</p>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Changes to Terms</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">We reserve the right to modify these terms at any time. We will notify users of any material changes via email or through the service.</p>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Contact Us</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">If you have questions about these Terms of Service, please contact us at <a href="mailto:legal@mongoose.gg" class="text-primary no-underline transition-opacity duration-200 hover:opacity-80">legal@mongoose.gg</a>.</p>
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
