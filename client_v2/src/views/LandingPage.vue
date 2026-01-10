<template>
  <div class="landing-page">
    <NavBar />
    
    <div class="landing-wrapper">
      <!-- Hero Section -->
      <section class="hero-section">
        <div class="hero-container">
          <div class="hero-badge">
            <span class="hero-badge-icon">🎮</span>
            <span>First 500 users get free Pro tier</span>
            <span class="hero-badge-count">{{ freeUsersLeft }} spots left</span>
          </div>
          
          <h1 class="hero-title">
            The Only LoL Tracker<br />
            Built for <span class="hero-gradient">Duos & Teams</span>
          </h1>
          
          <p class="hero-description">
            Powered by AI coaching that turns your stats into actionable goals you can actually achieve. 
            Stop guessing what to improve. Start climbing with purpose.
          </p>
          
          <div class="hero-cta">
            <router-link to="/auth?mode=signup" class="hero-btn-primary">
              Start Improving Now
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="hero-arrow">
                <path fill-rule="evenodd" d="M3 10a.75.75 0 01.75-.75h10.638L10.23 5.29a.75.75 0 111.04-1.08l5.5 5.25a.75.75 0 010 1.08l-5.5 5.25a.75.75 0 11-1.04-1.08l4.158-3.96H3.75A.75.75 0 013 10z" clip-rule="evenodd" />
              </svg>
            </router-link>
            <a href="#how-it-works" class="hero-btn-secondary">
              See How It Works
            </a>
          </div>
          
          <div class="hero-stats">
            <div class="hero-stat">
              <div class="hero-stat-value">2.5k+</div>
              <div class="hero-stat-label">Active Players</div>
            </div>
            <div class="hero-stat">
              <div class="hero-stat-value">100k+</div>
              <div class="hero-stat-label">Games Analyzed</div>
            </div>
            <div class="hero-stat">
              <div class="hero-stat-value">4.9/5</div>
              <div class="hero-stat-label">User Rating</div>
            </div>
          </div>
        </div>
      </section>

      <!-- Features Section -->
      <section id="features" class="features-section">
        <div class="section-container">
          <div class="section-header">
            <h2 class="section-title">Everything You Need to Climb</h2>
            <p class="section-description">
              From solo insights to team coordination, we've got your improvement covered.
            </p>
          </div>
          
          <div class="features-grid">
            <div v-for="feature in features" :key="feature.title" class="feature-card">
              <div class="feature-icon" v-html="feature.icon"></div>
              <h3 class="feature-title">{{ feature.title }}</h3>
              <p class="feature-description">{{ feature.description }}</p>
            </div>
          </div>
        </div>
      </section>

      <!-- How It Works Section -->
      <section id="how-it-works" class="how-it-works-section">
        <div class="section-container">
          <div class="section-header">
            <h2 class="section-title">How It Works</h2>
            <p class="section-description">
              Get started in minutes. See results in days.
            </p>
          </div>
          
          <div class="steps-grid">
            <div v-for="(step, index) in steps" :key="index" class="step-card">
              <div class="step-number">{{ index + 1 }}</div>
              <h3 class="step-title">{{ step.title }}</h3>
              <p class="step-description">{{ step.description }}</p>
            </div>
          </div>
        </div>
      </section>

      <!-- Pricing Section -->
      <section id="pricing" class="pricing-section">
        <div class="section-container">
          <div class="section-header">
            <h2 class="section-title">Simple, Transparent Pricing</h2>
            <p class="section-description">
              Choose the plan that fits your playstyle. First 500 users get Pro for free!
            </p>
          </div>
          
          <div class="pricing-grid">
            <div v-for="tier in pricingTiers" :key="tier.name" 
                 :class="['pricing-card', { 'pricing-popular': tier.popular }]">
              <div v-if="tier.popular" class="pricing-badge">Most Popular</div>
              
              <h3 class="pricing-tier-name">{{ tier.name }}</h3>
              <div class="pricing-price">
                <span class="pricing-amount">{{ tier.price }}</span>
                <span class="pricing-period">{{ tier.period }}</span>
              </div>
              
              <ul class="pricing-features">
                <li v-for="feature in tier.features" :key="feature" class="pricing-feature">
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="pricing-check">
                    <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd" />
                  </svg>
                  {{ feature }}
                </li>
              </ul>
              
              <router-link to="/auth?mode=signup" :class="['pricing-btn', { 'pricing-btn-primary': tier.popular }]">
                {{ tier.cta }}
              </router-link>
            </div>
          </div>
          
          <p class="pricing-note">
            All plans include 30-day money-back guarantee. Cancel anytime.
          </p>
        </div>
      </section>

      <!-- Footer -->
      <footer class="footer">
        <div class="footer-container">
          <div class="footer-brand">
            <div class="footer-logo">
              <img src="/pulse-icon.svg" alt="Pulse.gg" class="footer-logo-icon" />
              <span class="footer-logo-text">Pulse<span class="footer-logo-tld">.gg</span></span>
            </div>
            <p class="footer-tagline">Built with ❤️ in the nordics by the Agile Astronaut</p>
          </div>
          
          <div class="footer-links">
            <div class="footer-column">
              <h4 class="footer-heading">Product</h4>
              <a href="#features" class="footer-link">Features</a>
              <a href="#pricing" class="footer-link">Pricing</a>
              <a href="#how-it-works" class="footer-link">How It Works</a>
            </div>
            
            <div class="footer-column">
              <h4 class="footer-heading">Legal</h4>
              <a href="/privacy" class="footer-link">Privacy Policy</a>
              <a href="/terms" class="footer-link">Terms of Service</a>
            </div>
            
            <div class="footer-column">
              <h4 class="footer-heading">Connect</h4>
              <a href="mailto:hello@pulse.gg" class="footer-link">Contact Us</a>
              <a href="https://twitter.com/pulsegg" class="footer-link" target="_blank">Twitter</a>
              <a href="https://discord.gg/pulse" class="footer-link" target="_blank">Discord</a>
            </div>
          </div>
        </div>
        
        <div class="footer-bottom">
          <p class="footer-copyright">
            © {{ currentYear }} Pulse.gg. All rights reserved. Not affiliated with Riot Games.
          </p>
        </div>
      </footer>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue';
import NavBar from '../components/NavBar.vue';

const freeUsersLeft = ref(487);
const currentYear = computed(() => new Date().getFullYear());

const features = [
  {
    title: 'Solo Performance Analytics',
    description: 'Deep dive into your individual stats with AI-powered insights. Track CS, KDA, and game-phase performance.',
    icon: '📊'
  },
  {
    title: 'Duo Synergy Dashboard',
    description: 'Discover your duo\'s strengths and weaknesses. Optimize champion picks and lane matchups together.',
    icon: '👥'
  },
  {
    title: 'Team Coordination Tools',
    description: 'Full 5-player analytics. Track objectives, role distribution, and macro decision-making as a unit.',
    icon: '🎯'
  },
  {
    title: 'AI Goal Recommendations',
    description: 'Get personalized improvement goals based on your recent performance. No more guesswork.',
    icon: '🤖'
  },
  {
    title: 'Progress Tracking',
    description: 'Watch your metrics improve over time. Celebrate milestones and stay motivated.',
    icon: '📈'
  },
  {
    title: 'Queue Filtering',
    description: 'Analyze Ranked Solo, Flex, Normal, or ARAM separately. Compare your performance across modes.',
    icon: '🎮'
  }
];

const steps = [
  {
    title: 'Create Your Account',
    description: 'Sign up in seconds. Link your Riot account and we\'ll sync your match history automatically.'
  },
  {
    title: 'Get AI Insights',
    description: 'Our AI analyzes your stats and gives you 3-5 personalized improvement goals each week.'
  },
  {
    title: 'Track Your Progress',
    description: 'Set goals, play games, and watch your metrics improve in real-time dashboards.'
  },
  {
    title: 'Climb Together',
    description: 'Invite your duo or team. Share goals, coordinate strategies, and climb the ladder together.'
  }
];

const pricingTiers = [
  {
    name: 'Free',
    price: '€0',
    period: 'forever',
    cta: 'Start Free',
    popular: false,
    features: [
      'Solo stats only',
      'Last 20 games',
      'Basic analytics',
      'Ranked queue only'
    ]
  },
  {
    name: 'Pro',
    price: '€4.99',
    period: '/month',
    cta: 'Get Pro',
    popular: true,
    features: [
      'Full solo & duo stats',
      'Unlimited game history',
      '5 AI recommendations/week',
      'Goal tracking',
      'All queue types',
      'Advanced analytics'
    ]
  },
  {
    name: 'Team',
    price: '€8.99',
    period: '/month',
    cta: 'Get Team',
    popular: false,
    features: [
      'Everything in Pro',
      'Team dashboard (5 players)',
      'Shared goals',
      'Unlimited AI recommendations',
      'Role optimization',
      'Macro tracking'
    ]
  }
];
</script>

<style scoped>
.landing-page {
  min-height: 100vh;
  background: transparent; /* Reset: no page-level background */
}

.landing-wrapper {
  padding-top: 64px; /* Account for fixed nav */
}

/* Hero Section */
.hero-section {
  min-height: calc(100vh - 64px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-2xl) var(--spacing-xl);
  position: relative;
  overflow: hidden;
}

/* Accent gradient overlay (purple glow) */
/* Removed hero overlay to simplify background */

.hero-container {
  max-width: 800px;
  text-align: center;
  position: relative;
  z-index: 1;
}

.hero-badge {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-sm) var(--spacing-lg);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 9999px;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  margin-bottom: var(--spacing-xl);
  backdrop-filter: blur(10px);
}

.hero-badge-icon {
  font-size: var(--font-size-lg);
}

.hero-badge-count {
  color: var(--color-primary);
  font-weight: var(--font-weight-semibold);
}

.hero-title {
  font-size: clamp(2.5rem, 5vw, 4rem);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
  line-height: 1.1;
  margin-bottom: var(--spacing-lg);
  color: var(--color-text);
}

.hero-gradient {
  background: linear-gradient(135deg, var(--color-primary), #00a8ff);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
}

.hero-description {
  font-size: var(--font-size-lg);
  color: var(--color-text-secondary);
  line-height: 1.6;
  margin-bottom: var(--spacing-2xl);
  max-width: 600px;
  margin-left: auto;
  margin-right: auto;
}

.hero-cta {
  display: flex;
  gap: var(--spacing-md);
  justify-content: center;
  flex-wrap: wrap;
  margin-bottom: var(--spacing-2xl);
}

.hero-btn-primary {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-md) var(--spacing-xl);
  background: var(--color-primary);
  color: white;
  font-weight: var(--font-weight-semibold);
  font-size: var(--font-size-md);
  text-decoration: none;
  border-radius: var(--radius-md);
  transition: all 0.2s;
  box-shadow: var(--shadow-md);
  letter-spacing: var(--letter-spacing);
}

.hero-btn-primary:hover {
  box-shadow: var(--shadow-lg);
  transform: translateY(-2px);
}

.hero-arrow {
  width: 20px;
  height: 20px;
  transition: transform 0.2s;
}

.hero-btn-primary:hover .hero-arrow {
  transform: translateX(3px);
}

.hero-btn-secondary {
  display: inline-flex;
  align-items: center;
  padding: var(--spacing-md) var(--spacing-xl);
  background: var(--color-surface);
  color: var(--color-text);
  font-weight: var(--font-weight-semibold);
  font-size: var(--font-size-md);
  text-decoration: none;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  transition: all 0.2s;
  backdrop-filter: blur(10px);
  letter-spacing: var(--letter-spacing);
}

.hero-btn-secondary:hover {
  border-color: var(--color-primary);
  background: var(--color-primary-soft);
}

.hero-stats {
  display: flex;
  gap: var(--spacing-2xl);
  justify-content: center;
  flex-wrap: wrap;
}

.hero-stat {
  text-align: center;
}

.hero-stat-value {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-primary);
  letter-spacing: var(--letter-spacing);
}

.hero-stat-label {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  margin-top: var(--spacing-xs);
}

/* Section Styles */
.features-section,
.how-it-works-section,
.pricing-section {
  padding: var(--spacing-2xl) var(--spacing-xl);
}

.section-container {
  max-width: 1200px;
  margin: 0 auto;
}

.section-header {
  text-align: center;
  margin-bottom: var(--spacing-2xl);
}

.section-title {
  font-size: clamp(2rem, 4vw, 3rem);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
  margin-bottom: var(--spacing-md);
  color: var(--color-text);
}

.section-description {
  font-size: var(--font-size-lg);
  color: var(--color-text-secondary);
  max-width: 600px;
  margin: 0 auto;
}

/* Features Grid */
.features-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: var(--spacing-lg);
}

.feature-card {
  padding: var(--spacing-xl);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  transition: all 0.3s;
  backdrop-filter: blur(10px);
}

.feature-card:hover {
  border-color: var(--color-primary);
  transform: translateY(-4px);
  box-shadow: var(--shadow-lg);
}

.feature-icon {
  font-size: 3rem;
  margin-bottom: var(--spacing-md);
}

.feature-title {
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
  margin-bottom: var(--spacing-sm);
  color: var(--color-text);
}

.feature-description {
  font-size: var(--font-size-md);
  color: var(--color-text-secondary);
  line-height: 1.6;
}

/* Steps Grid */
.steps-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: var(--spacing-xl);
}

.step-card {
  text-align: center;
  padding: var(--spacing-xl);
}

.step-number {
  width: 60px;
  height: 60px;
  margin: 0 auto var(--spacing-lg);
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--color-primary);
  color: white;
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  border-radius: 50%;
  box-shadow: var(--shadow-md);
}

.step-title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
  margin-bottom: var(--spacing-sm);
  color: var(--color-text);
}

.step-description {
  font-size: var(--font-size-md);
  color: var(--color-text-secondary);
  line-height: 1.6;
}

/* Pricing Grid */
.pricing-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: var(--spacing-lg);
  margin-bottom: var(--spacing-xl);
}

.pricing-card {
  position: relative;
  padding: var(--spacing-xl);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  transition: all 0.3s;
  backdrop-filter: blur(10px);
}

.pricing-card:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-md);
}

.pricing-popular {
  border-color: var(--color-primary);
  box-shadow: var(--shadow-lg);
  transform: scale(1.05);
}

.pricing-popular:hover {
  transform: scale(1.05) translateY(-4px);
}

.pricing-badge {
  position: absolute;
  top: -12px;
  left: 50%;
  transform: translateX(-50%);
  padding: var(--spacing-xs) var(--spacing-md);
  background: var(--color-primary);
  color: white;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  border-radius: 9999px;
  letter-spacing: var(--letter-spacing);
}

.pricing-tier-name {
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
  margin-bottom: var(--spacing-md);
  color: var(--color-text);
}

.pricing-price {
  margin-bottom: var(--spacing-xl);
}

.pricing-amount {
  font-size: clamp(2rem, 4vw, 3rem);
  font-weight: var(--font-weight-bold);
  color: var(--color-primary);
  letter-spacing: var(--letter-spacing);
}

.pricing-period {
  font-size: var(--font-size-md);
  color: var(--color-text-secondary);
  margin-left: var(--spacing-xs);
}

.pricing-features {
  list-style: none;
  margin-bottom: var(--spacing-xl);
}

.pricing-feature {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-sm) 0;
  font-size: var(--font-size-md);
  color: var(--color-text-secondary);
}

.pricing-check {
  width: 20px;
  height: 20px;
  color: var(--color-primary);
  flex-shrink: 0;
}

.pricing-btn {
  display: block;
  width: 100%;
  padding: var(--spacing-md);
  text-align: center;
  text-decoration: none;
  font-weight: var(--font-weight-semibold);
  font-size: var(--font-size-md);
  border-radius: var(--radius-md);
  transition: all 0.2s;
  letter-spacing: var(--letter-spacing);
  background: var(--color-surface);
  color: var(--color-text);
  border: 1px solid var(--color-border);
}

.pricing-btn:hover {
  border-color: var(--color-primary);
  background: var(--color-primary-soft);
}

.pricing-btn-primary {
  background: var(--color-primary);
  color: white;
  border-color: var(--color-primary);
  box-shadow: var(--shadow-sm);
}

.pricing-btn-primary:hover {
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
}

.pricing-note {
  text-align: center;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

/* Footer */
.footer {
  border-top: 1px solid var(--color-border);
  padding: var(--spacing-2xl) var(--spacing-xl) var(--spacing-lg);
  margin-top: var(--spacing-2xl);
}

.footer-container {
  max-width: 1200px;
  margin: 0 auto;
  display: grid;
  grid-template-columns: 1fr 2fr;
  gap: var(--spacing-2xl);
  margin-bottom: var(--spacing-xl);
}

@media (max-width: 768px) {
  .footer-container {
    grid-template-columns: 1fr;
  }
}

.footer-brand {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.footer-logo {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
}

.footer-logo-icon {
  width: 32px;
  height: 32px;
}

.footer-logo-text {
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
  color: var(--color-text);
}

.footer-logo-tld {
  color: var(--color-primary);
}

.footer-tagline {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.footer-links {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
  gap: var(--spacing-xl);
}

.footer-column {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.footer-heading {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  letter-spacing: var(--letter-spacing);
  color: var(--color-text);
  margin-bottom: var(--spacing-xs);
}

.footer-link {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  text-decoration: none;
  transition: color 0.2s;
}

.footer-link:hover {
  color: var(--color-text);
}

.footer-bottom {
  max-width: 1200px;
  margin: 0 auto;
  padding-top: var(--spacing-xl);
  border-top: 1px solid var(--color-border);
  text-align: center;
}

.footer-copyright {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}
</style>
