<template>
  <div class="legal-page">
    <NavBar />
    
    <div class="legal-container">
      <div class="legal-content">
        <h1 class="legal-title">Privacy Policy</h1>
        <p class="legal-updated">Last updated: {{ currentDate }}</p>
        
        <section class="legal-section">
          <h2>Overview</h2>
          <p>Pulse.gg ("we", "our", or "us") is committed to protecting your privacy. This Privacy Policy explains how we collect, use, and share information about you when you use our service.</p>
        </section>
        
        <section class="legal-section">
          <h2>Information We Collect</h2>
          <p>We collect the following types of information:</p>
          <ul>
            <li>Account information (email, username)</li>
            <li>Riot Games account data (summoner name, match history)</li>
            <li>Usage data and analytics</li>
            <li>Payment information (processed securely through Mollie)</li>
          </ul>
        </section>
        
        <section class="legal-section">
          <h2>How We Use Your Information</h2>
          <p>We use your information to:</p>
          <ul>
            <li>Provide and improve our services</li>
            <li>Generate personalized insights and recommendations</li>
            <li>Process payments and manage subscriptions</li>
            <li>Communicate with you about updates and features</li>
          </ul>
        </section>
        
        <section class="legal-section">
          <h2>Data Security</h2>
          <p>We implement industry-standard security measures to protect your data. However, no method of transmission over the internet is 100% secure.</p>
        </section>
        
        <section class="legal-section">
          <h2>Your Rights</h2>
          <p>You have the right to access, update, or delete your personal data. Contact us at privacy@pulse.gg for any requests.</p>
        </section>
        
        <section class="legal-section">
          <h2>Contact Us</h2>
          <p>If you have questions about this Privacy Policy, please contact us at <a href="mailto:privacy@pulse.gg">privacy@pulse.gg</a>.</p>
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
