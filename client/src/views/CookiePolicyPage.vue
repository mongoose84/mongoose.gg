<template>
  <div class="min-h-screen bg-transparent pt-16">
    <NavBar />

    <div class="max-w-[800px] mx-auto py-2xl px-xl">
      <div class="bg-background-surface border border-border rounded-lg p-2xl backdrop-blur-[10px]" data-testid="cookie-policy-content">
        <h1 class="text-2xl font-bold tracking-tight mb-sm text-text">Cookie Policy</h1>
        <p class="text-sm text-text-secondary mb-2xl">Last updated: {{ currentDate }}</p>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Overview</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">
            Mongoose.gg uses cookies to provide and improve our services. This Cookie Policy explains what cookies are,
            how we use them, and your rights regarding cookies.
          </p>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">What Are Cookies?</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">
            Cookies are small text files stored on your device that allow websites to remember information about your visit.
            They are widely used to make websites work more efficiently and provide a better user experience.
          </p>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Cookies We Use</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">
            Mongoose.gg currently uses one essential authentication cookie:
          </p>

          <div class="cookie-table-container">
            <table class="cookie-table">
              <thead>
                <tr>
                  <th>Cookie Name</th>
                  <th>Purpose</th>
                  <th>Type</th>
                  <th>Duration</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td><code>mongoose-auth</code></td>
                  <td>Authentication — maintains your logged-in session</td>
                  <td>First-party, Strictly Necessary</td>
                  <td>Session or 30 days (based on "Remember Me" preference)</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Strictly Necessary Cookies</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">
            The <code>mongoose-auth</code> cookie is classified as "strictly necessary" because it is essential for the core
            functionality of our service. When you log in, we set this cookie to keep you authenticated as you navigate the site.
          </p>
          <p class="text-base text-text-secondary leading-relaxed mb-md">
            <strong>Legal basis:</strong> Under the ePrivacy Directive (EU) and GDPR, strictly necessary cookies do not require
            affirmative consent. However, we provide a cookie consent banner to inform you about our use of cookies.
          </p>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Your Preferences</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">
            When you visit Mongoose.gg, we ask for your consent about cookies:
          </p>
          <ul class="list-disc ml-xl text-text-secondary">
            <li class="text-base leading-relaxed mb-xs"><strong>Accept Cookies:</strong> You allow us to set the authentication cookie. Login and analytics features will be available.</li>
            <li class="text-base leading-relaxed mb-xs"><strong>Reject Cookies:</strong> No cookies will be set. You will not be able to log in or use authenticated features.</li>
          </ul>
          <p class="text-base text-text-secondary leading-relaxed mb-md mt-md">
            Your consent preference is stored for 6 months. You can change your preference at any time by clicking
            <router-link to="/app/user" class="text-primary">Cookie Preferences</router-link> in your account settings.
          </p>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Cookie Duration</h2>
          <div class="cookie-duration-container">
            <div class="duration-item">
              <h3 class="text-base font-semibold text-text mb-xs">Session Cookie</h3>
              <p class="text-sm text-text-secondary mb-0">
                Cleared when you close your browser or after 30 minutes of inactivity. Set when "Remember Me" is unchecked.
              </p>
            </div>
            <div class="duration-item">
              <h3 class="text-base font-semibold text-text mb-xs">Persistent Cookie (30 days)</h3>
              <p class="text-sm text-text-secondary mb-0">
                Remains on your device for 30 days, allowing you to stay logged in across multiple browser sessions.
                Set when "Remember Me" is checked at login.
              </p>
            </div>
          </div>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Data Controller</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">
            Mongoose.gg is the data controller for the cookies we set. If you have questions about your cookie data,
            please contact us at <a href="mailto:privacy@mongoose.gg" class="text-primary no-underline hover:underline">privacy@mongoose.gg</a>.
          </p>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Your Rights</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">
            Under GDPR and ePrivacy regulations, you have the right to:
          </p>
          <ul class="list-disc ml-xl text-text-secondary">
            <li class="text-base leading-relaxed mb-xs">Know what cookies we use and why</li>
            <li class="text-base leading-relaxed mb-xs">Accept or reject cookies (as much as technically possible)</li>
            <li class="text-base leading-relaxed mb-xs">Withdraw your consent at any time</li>
            <li class="text-base leading-relaxed mb-xs">Request information about how we use your data</li>
          </ul>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Browser Controls</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">
            Most web browsers allow you to control cookies through their settings. You can typically:
          </p>
          <ul class="list-disc ml-xl text-text-secondary">
            <li class="text-base leading-relaxed mb-xs">View cookies stored by websites</li>
            <li class="text-base leading-relaxed mb-xs">Delete cookies from your device</li>
            <li class="text-base leading-relaxed mb-xs">Disable cookies entirely (not recommended)</li>
          </ul>
          <p class="text-base text-text-secondary leading-relaxed mb-md mt-md">
            However, disabling strictly necessary cookies may prevent our services from functioning properly.
          </p>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Changes to This Policy</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">
            We may update this Cookie Policy from time to time to reflect changes in our practices or for other operational,
            legal, or regulatory reasons. We will notify you of any material changes by updating the date at the top of this policy.
          </p>
        </section>

        <section class="mb-xl">
          <h2 class="text-xl font-bold tracking-tight mb-md text-text">Contact Us</h2>
          <p class="text-base text-text-secondary leading-relaxed mb-md">
            If you have questions about this Cookie Policy or our use of cookies, please contact us at
            <a href="mailto:privacy@mongoose.gg" class="text-primary no-underline hover:underline">privacy@mongoose.gg</a>.
          </p>
        </section>

        <div class="mt-2xl pt-xl border-t border-border">
          <router-link to="/" class="text-primary no-underline hover:underline">
            ← Back to Home
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import NavBar from '../components/NavBar.vue'

const currentDate = computed(() => {
  const now = new Date()
  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  }).format(now)
})
</script>

<style scoped>
.cookie-table-container {
  overflow-x: auto;
  margin: var(--spacing-md) 0;
}

.cookie-table {
  width: 100%;
  border-collapse: collapse;
  font-size: var(--font-size-sm);
}

.cookie-table thead {
  background-color: rgba(255, 255, 255, 0.05);
  border-bottom: 1px solid var(--color-border);
}

.cookie-table th {
  padding: var(--spacing-md);
  text-align: left;
  font-weight: 600;
  color: var(--color-text);
}

.cookie-table td {
  padding: var(--spacing-md);
  border-bottom: 1px solid var(--color-border);
  color: var(--color-text-secondary);
}

.cookie-table code {
  background-color: rgba(255, 255, 255, 0.05);
  border: 1px solid var(--color-border);
  border-radius: 4px;
  padding: 2px 6px;
  font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
  font-size: 0.85em;
  color: var(--color-primary);
}

.cookie-duration-container {
  display: grid;
  gap: var(--spacing-lg);
  margin: var(--spacing-md) 0;
}

.duration-item {
  background-color: rgba(255, 255, 255, 0.05);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: var(--spacing-lg);
}

.duration-item h3 {
  margin: 0 0 var(--spacing-xs) 0;
}

.duration-item p {
  margin: 0;
}
</style>
