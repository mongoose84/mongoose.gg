<template>
  <div class="legal-page">
    <NavBar />
    
    <div class="legal-container">
      <div class="legal-content">
        <h1 class="legal-title">Terms of Service</h1>
        <p class="legal-updated">Last updated: {{ currentDate }}</p>
        
        <section class="legal-section">
          <h2>Agreement to Terms</h2>
          <p>By accessing and using Pulse.gg, you agree to be bound by these Terms of Service. If you disagree with any part of the terms, you may not access the service.</p>
        </section>
        
        <section class="legal-section">
          <h2>Use License</h2>
          <p>We grant you a limited, non-exclusive, non-transferable license to use Pulse.gg for personal, non-commercial purposes, subject to these terms.</p>
        </section>
        
        <section class="legal-section">
          <h2>Riot Games Disclaimer</h2>
          <p>Pulse.gg is not endorsed by Riot Games and does not reflect the views or opinions of Riot Games or anyone officially involved in producing or managing Riot Games properties. Riot Games and all associated properties are trademarks or registered trademarks of Riot Games, Inc.</p>
        </section>
        
        <section class="legal-section">
          <h2>Subscription Terms</h2>
          <p>Subscription fees are billed in advance on a monthly or annual basis. You may cancel your subscription at any time. Cancellations take effect at the end of the current billing period.</p>
        </section>
        
        <section class="legal-section">
          <h2>Refund Policy</h2>
          <p>We offer a 30-day money-back guarantee for all paid subscriptions. Contact support@pulse.gg within 30 days of your initial purchase for a full refund.</p>
        </section>
        
        <section class="legal-section">
          <h2>Prohibited Uses</h2>
          <p>You may not use Pulse.gg for any unlawful purpose or to violate any laws. You may not attempt to gain unauthorized access to any part of the service.</p>
        </section>
        
        <section class="legal-section">
          <h2>Limitation of Liability</h2>
          <p>In no event shall Pulse.gg be liable for any indirect, incidental, special, consequential, or punitive damages resulting from your use or inability to use the service.</p>
        </section>
        
        <section class="legal-section">
          <h2>Changes to Terms</h2>
          <p>We reserve the right to modify these terms at any time. We will notify users of any material changes via email or through the service.</p>
        </section>
        
        <section class="legal-section">
          <h2>Contact Us</h2>
          <p>If you have questions about these Terms of Service, please contact us at <a href="mailto:legal@pulse.gg">legal@pulse.gg</a>.</p>
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

<style scoped>
.legal-page {
  min-height: 100vh;
  background: transparent;
  padding-top: 64px;
}

.legal-container {
  max-width: 800px;
  margin: 0 auto;
  padding: var(--spacing-2xl) var(--spacing-xl);
}

.legal-content {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-2xl);
  backdrop-filter: blur(10px);
}

.legal-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
  margin-bottom: var(--spacing-sm);
  color: var(--color-text);
}

.legal-updated {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  margin-bottom: var(--spacing-2xl);
}

.legal-section {
  margin-bottom: var(--spacing-xl);
}

.legal-section h2 {
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
  margin-bottom: var(--spacing-md);
  color: var(--color-text);
}

.legal-section p {
  font-size: var(--font-size-md);
  color: var(--color-text-secondary);
  line-height: 1.6;
  margin-bottom: var(--spacing-md);
}

.legal-section ul {
  list-style: disc;
  margin-left: var(--spacing-xl);
  color: var(--color-text-secondary);
}

.legal-section li {
  font-size: var(--font-size-md);
  line-height: 1.6;
  margin-bottom: var(--spacing-xs);
}

.legal-section a {
  color: var(--color-primary);
  text-decoration: none;
  transition: opacity 0.2s;
}

.legal-section a:hover {
  opacity: 0.8;
}
</style>
