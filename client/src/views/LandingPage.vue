<template>
	<div class="min-h-screen bg-transparent">
    <NavBar />

    <div class="pt-16">
      <!-- Hero Section -->
      <section class="min-h-[calc(100vh-64px)] flex items-center justify-center p-2xl relative overflow-hidden">
        <div class="max-w-[800px] text-center relative z-10">
        <!-- Logo above hero title -->
          <div class="w-[210px] h-[100px] mx-auto">
            <img src="/mongoose.png" alt="Mongoose.gg logo" class="w-full h-full object-contain" />
          </div>
          <h2 class="text-2xl font-bold text-text -mt-4 ml-sm mb-2xl">Mongoose.gg <span class="beta-tag">Beta</span></h2>

          <div class="inline-flex items-center gap-sm py-sm px-lg bg-background-surface border border-border rounded-full text-sm text-text-secondary mt-2xl mb-2xl backdrop-blur-[10px]">
            <span class="text-lg">🎮</span>
            <span>First 500 users get free Pro tier</span>
            <span class="text-primary font-semibold">{{ freeUsersLeft }} spots left</span>
          </div>

          <h1 class="hero-title">
            The <span class="hero-gradient">Solo Queue</span> Improvement Tracker<br />
            Built for <span class="hero-gradient">Duos & Teams</span>
          </h1>

          <p class="text-lg text-text-secondary leading-relaxed mb-xl max-w-[600px] mx-auto">
            Not just another builds app.
          </p>
          <p class="text-lg text-text-secondary leading-relaxed mb-xl max-w-[600px] mx-auto">
            Better champ select picks, post-game takeaways that stick, and track your progress over time—and climb together with your duo or team.
          </p>

          <div class="flex gap-md justify-center flex-wrap mb-2xl">
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

	          <div class="flex gap-2xl justify-center flex-wrap">
	            <div class="text-center">
	              <div class="text-2xl font-bold text-primary tracking-tight">{{ activePlayersDisplay }}</div>
	              <div class="text-sm text-text-secondary mt-xs">Active Players</div>
	            </div>
	            <div class="text-center">
	              <div class="text-2xl font-bold text-primary tracking-tight">{{ gamesAnalyzedDisplay }}</div>
	              <div class="text-sm text-text-secondary mt-xs">Games Analyzed</div>
	            </div>
	            <div class="text-center">
	              <div class="text-2xl font-bold text-primary tracking-tight">0/5</div>
	              <div class="text-sm text-text-secondary mt-xs">User Rating</div>
	            </div>
	          </div>
        </div>
      </section>

      <!-- Features Section -->
      <section id="features" class="p-2xl">
        <div class="max-w-[1200px] mx-auto">
          <div class="text-center mb-2xl">
            <h2 class="section-title">Everything You Need to Climb</h2>
            <p class="text-lg text-text-secondary max-w-[600px] mx-auto m-0">
              From solo insights to team coordination, we've got your improvement covered.
            </p>
          </div>

          <div class="grid grid-cols-[repeat(auto-fit,minmax(300px,1fr))] gap-lg">
            <div v-for="feature in features" :key="feature.title" class="feature-card">
              <div class="text-[3rem] mb-md" v-html="feature.icon"></div>
              <h3 class="text-xl font-bold tracking-tight mb-sm text-text m-0">{{ feature.title }}</h3>
              <p class="text-md text-text-secondary leading-relaxed m-0">{{ feature.description }}</p>
            </div>
          </div>
        </div>
      </section>

      <!-- How It Works Section -->
      <section id="how-it-works" class="p-2xl">
        <div class="max-w-[1200px] mx-auto">
          <div class="text-center mb-2xl">
            <h2 class="section-title">How It Works</h2>
            <p class="text-lg text-text-secondary max-w-[600px] mx-auto m-0">
              Get started in minutes. See results in days.
            </p>
          </div>

          <div class="grid grid-cols-[repeat(auto-fit,minmax(250px,1fr))] gap-xl">
            <div v-for="(step, index) in steps" :key="index" class="text-center p-xl">
              <div class="step-number">{{ index + 1 }}</div>
              <h3 class="text-lg font-bold tracking-tight mb-sm text-text m-0">{{ step.title }}</h3>
              <p class="text-md text-text-secondary leading-relaxed m-0">{{ step.description }}</p>
            </div>
          </div>
        </div>
      </section>

      <!-- Pricing Section -->
      <section id="pricing" class="p-2xl">
        <div class="max-w-[1200px] mx-auto">
          <div class="text-center mb-2xl">
            <h2 class="section-title">Simple, Transparent Pricing</h2>
            <p class="text-lg text-text-secondary max-w-[600px] mx-auto m-0">
              Choose the plan that fits your playstyle. First 500 users get Pro for free!
            </p>
          </div>

          <div class="grid grid-cols-[repeat(auto-fit,minmax(280px,1fr))] gap-lg mb-xl">
            <div v-for="tier in pricingTiers" :key="tier.name"
                 :class="['pricing-card', { 'pricing-popular': tier.popular }]">
              <div v-if="tier.popular" class="pricing-badge">Most Popular</div>

              <h3 class="text-xl font-bold tracking-tight mb-md text-text m-0">{{ tier.name }}</h3>
              <div class="mb-xl">
                <span class="pricing-amount">{{ tier.price }}</span>
                <span class="text-md text-text-secondary ml-xs">{{ tier.period }}</span>
              </div>

              <ul class="list-none mb-xl p-0">
                <li v-for="feature in tier.features" :key="feature" class="flex items-center gap-sm py-sm text-md text-text-secondary">
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-5 h-5 text-primary flex-shrink-0">
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

          <p class="text-center text-sm text-text-secondary m-0">
            All plans include 30-day money-back guarantee. Cancel anytime.
          </p>
        </div>
      </section>

      <!-- Footer -->
      <footer class="border-t border-border py-2xl px-xl mt-2xl">
        <div class="max-w-[1200px] mx-auto grid grid-cols-[1fr_2fr] gap-2xl mb-xl md:grid-cols-1">
          <div class="flex flex-col gap-md">
            <div class="flex items-center gap-sm">
              <img src="/mongoose.png" alt="Mongoose" class="w-16 h-8" />
              <span class="text-xl font-bold tracking-tight text-text">Mongoose.gg <span class="beta-tag">Beta</span></span>
            </div>
            <p class="text-sm text-text-secondary m-0">Built with ❤️ in the nordics by the Agile Astronaut</p>
          </div>

          <div class="grid grid-cols-[repeat(auto-fit,minmax(150px,1fr))] gap-xl">
            <div class="flex flex-col gap-sm">
              <h4 class="text-sm font-semibold tracking-tight text-text mb-xs">Product</h4>
              <a href="#features" class="text-sm text-text-secondary no-underline hover:text-text transition-colors duration-200">Features</a>
              <a href="#pricing" class="text-sm text-text-secondary no-underline hover:text-text transition-colors duration-200">Pricing</a>
              <a href="#how-it-works" class="text-sm text-text-secondary no-underline hover:text-text transition-colors duration-200">How It Works</a>
            </div>

            <div class="flex flex-col gap-sm">
              <h4 class="text-sm font-semibold tracking-tight text-text mb-xs">Legal</h4>
              <a href="/privacy" class="text-sm text-text-secondary no-underline hover:text-text transition-colors duration-200">Privacy Policy</a>
              <a href="/terms" class="text-sm text-text-secondary no-underline hover:text-text transition-colors duration-200">Terms of Service</a>
            </div>

            <div class="flex flex-col gap-sm">
              <h4 class="text-sm font-semibold tracking-tight text-text mb-xs">Connect</h4>
              <a href="mailto:hello@mongoose.gg" class="text-sm text-text-secondary no-underline hover:text-text transition-colors duration-200">Email Us</a>
            </div>
          </div>
        </div>

        <div class="max-w-[1200px] mx-auto pt-xl border-t border-border text-center">
          <p class="text-xs text-text-secondary m-0">
            © {{ currentYear }} Mongoose.gg <span class="beta-tag">Beta</span>. All rights reserved. Not affiliated with Riot Games.
          </p>
        </div>
      </footer>
    </div>
  </div>
</template>

	<script setup>
	import { ref, computed, onMounted } from 'vue';
	import NavBar from '../components/NavBar.vue';
	import { getPublicStats } from '../services/authApi';

	const freeUsersLeft = ref(493);
	const currentYear = computed(() => new Date().getFullYear());

	const gamesAnalyzed = ref(null);
	const gamesAnalyzedDisplay = computed(() => {
	  if (gamesAnalyzed.value == null) {
	    return '...';
	  }
	  return gamesAnalyzed.value.toLocaleString();
	});

	const activePlayers = ref(null);
	const activePlayersDisplay = computed(() => {
	  if (activePlayers.value == null) {
	    return '...';
	  }
	  return activePlayers.value.toLocaleString();
	});

	onMounted(async () => {
	  try {
	    const stats = await getPublicStats();
	    if (stats && typeof stats.totalMatches === 'number') {
	      gamesAnalyzed.value = stats.totalMatches;
	    }
	    if (stats && typeof stats.activePlayers === 'number') {
	      activePlayers.value = stats.activePlayers;
	    }
	  } catch (error) {
	    console.error('Failed to load public stats', error);
	  }
	});

const features = [
  {
    title: 'Champ Select Matchup Highlights',
    description: 'Know your matchups before you lock in. See personal win rates and tips for the champions you\'re facing.',
    icon: '⚔️'
  },
  {
    title: 'Post-Game Takeaways',
    description: 'After every game, get 2-3 specific things to focus on next time. No walls of stats—just what matters.',
    icon: '📝'
  },
  {
    title: 'Goal Setting & Progress',
    description: 'Set concrete improvement goals and track them over your next 20 games. Watch your metrics trend upward.',
    icon: '📈'
  },
  {
    title: 'Duo & Team Dashboards',
    description: 'Upgrade to Pro and unlock shared views with your duo or team. Set goals together and climb as a unit.',
    icon: '👥'
  },
  {
    title: 'Full Match History',
    description: 'Pro users get unlimited history. Free users see your last 20 games—enough to spot patterns.',
    icon: '📊'
  },
  {
    title: 'Queue Filtering',
    description: 'Analyze Ranked Solo, Flex, Normal, or ARAM separately. Compare your performance across modes.',
    icon: '🎮'
  }
];

const steps = [
  {
    title: 'Link Your Riot Account',
    description: 'Sign up in seconds. We\'ll sync your match history automatically—no manual uploads.'
  },
  {
    title: 'Get Champ Select Tips',
    description: 'Before you lock in, see your personal matchup stats and quick reminders for the lane ahead.'
  },
  {
    title: 'Review Post-Game Takeaways',
    description: 'After each game, get 2-3 actionable things to focus on. Set goals and track them over time.'
  },
  {
    title: 'Climb Together (Pro)',
    description: 'Invite your duo or team. Unlock shared dashboards and set goals you can achieve together.'
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
      'Solo basics',
      'Last 20 games',
      'Champ-select personal matchup highlights',
      'Limited post-game takeaways'
    ]
  },
  {
    name: 'Pro',
    price: '€4.99',
    period: '/month (€3.99/mo annual)',
    cta: 'Get Pro',
    popular: true,
    features: [
      'Full solo history',
      'Goal setting + tracking',
      'Deeper post-game coaching',
      'Duo & Team spaces',
      'Duo & Team dashboards',
      'Shared goals (Guests can join, Pro can collaborate)'
    ]
  }
];
</script>

<style scoped>
/* Beta tag styling (can't easily do 0.5em with Tailwind) */
.beta-tag {
  font-size: 0.5em;
  color: var(--color-text-secondary);
  font-weight: var(--font-weight-normal);
  vertical-align: top;
}

/* Hero title with clamp (responsive font sizing) */
.hero-title {
  font-size: clamp(2.5rem, 5vw, 4rem);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
  line-height: 1.1;
  margin-bottom: var(--spacing-lg);
  color: var(--color-text);
}

/* Gradient text effect (can't be done with Tailwind) */
.hero-gradient {
  background: linear-gradient(135deg, var(--color-primary), #00a8ff);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
}

/* Hero CTA buttons with hover animations */
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

/* Section title with clamp (responsive font sizing) */
.section-title {
  font-size: clamp(2rem, 4vw, 3rem);
  font-weight: var(--font-weight-bold);
  letter-spacing: var(--letter-spacing);
  margin-bottom: var(--spacing-md);
  color: var(--color-text);
}

/* Feature card with hover animation */
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

/* Step number circle */
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

/* Pricing card with hover and popular state */
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

/* Pricing badge positioned above card */
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

/* Pricing amount with clamp (responsive font sizing) */
.pricing-amount {
  font-size: clamp(2rem, 4vw, 3rem);
  font-weight: var(--font-weight-bold);
  color: var(--color-primary);
  letter-spacing: var(--letter-spacing);
}

/* Pricing buttons */
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

/* Responsive footer grid */
@media (max-width: 768px) {
  .md\:grid-cols-1 {
    grid-template-columns: 1fr !important;
  }
}
</style>
