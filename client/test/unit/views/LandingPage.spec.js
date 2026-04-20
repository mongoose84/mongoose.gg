import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount } from '@vue/test-utils';
import LandingPage from '@/views/LandingPage.vue';
import { createRouter, createMemoryHistory } from 'vue-router';

const createWrapper = () => {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div>Home</div>' } },
      { path: '/auth', component: { template: '<div>Auth</div>' } },
    ]
  });

  return mount(LandingPage, {
    global: {
      plugins: [router],
      stubs: {
        NavBar: true,
      }
    }
  });
};

describe('LandingPage.vue', () => {
  it('renders the landing page', () => {
    const wrapper = createWrapper();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the hero title', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('The Solo Queue Improvement Tracker');
  });

  it('hero title no longer contains "Built for Teams"', () => {
    const wrapper = createWrapper();
    const h1 = wrapper.find('h1');
    expect(h1.text()).not.toContain('Built for Teams');
    expect(h1.text()).toContain('Built to Help You Climb');
  });

  it('does not render the promo banner pill', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).not.toContain('spots left');
    expect(wrapper.text()).not.toContain('First 500 users get free Pro tier');
  });

  it('does not render the 0/5 User Rating counter', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).not.toContain('User Rating');
    expect(wrapper.text()).not.toContain('0/5');
  });

  it('renders Active Players and Games Analyzed counters', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Active Players');
    expect(wrapper.text()).toContain('Games Analyzed');
  });

  it('displays features section', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Everything You Need to Climb');
  });

  it('displays how it works section', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('How It Works');
  });

  it('has CTA buttons', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Start Improving Now');
  });

  it('renders feature icons as plain text', () => {
    const wrapper = createWrapper();
    const featureIcons = wrapper.findAll('[data-testid="feature-icon"]');

    expect(featureIcons.length).toBeGreaterThan(0);
    expect(featureIcons[0].text()).toBe('⚔️');
    expect(featureIcons[0].find('img').exists()).toBe(false);
    expect(featureIcons[0].find('script').exists()).toBe(false);
  });

  it('Post-Game Takeaways card uses updated description copy', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('lane-by-lane breakdown');
    expect(wrapper.text()).not.toContain('get 2-3 specific things to focus on next time');
  });
});

describe('LandingPage.vue — flag=false (default)', () => {
  beforeEach(() => {
    vi.stubEnv('VITE_ENABLE_UPCOMING_FEATURES', 'false');
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it('hides the Pricing section', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).not.toContain('Simple, Transparent Pricing');
  });

  it('hides Goal Setting feature card', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).not.toContain('Goal Setting & Progress');
  });

  it('hides Team Dashboards feature card', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).not.toContain('Team Dashboards');
  });

  it('renders 3 How It Works steps', () => {
    const wrapper = createWrapper();
    const steps = wrapper.findAll('#how-it-works .step-number');
    expect(steps).toHaveLength(3);
  });

  it('hides the footer Pricing link', () => {
    const wrapper = createWrapper();
    const pricingLinks = wrapper.findAll('a[href="#pricing"]');
    expect(pricingLinks).toHaveLength(0);
  });
});

describe('LandingPage.vue — flag=true', () => {
  beforeEach(() => {
    vi.stubEnv('VITE_ENABLE_UPCOMING_FEATURES', 'true');
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it('shows the Pricing section', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Simple, Transparent Pricing');
  });

  it('shows Goal Setting feature card', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Goal Setting & Progress');
  });

  it('shows Team Dashboards feature card', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Team Dashboards');
  });

  it('renders 4 How It Works steps', () => {
    const wrapper = createWrapper();
    const steps = wrapper.findAll('#how-it-works .step-number');
    expect(steps).toHaveLength(4);
  });

  it('shows the footer Pricing link', () => {
    const wrapper = createWrapper();
    const pricingLinks = wrapper.findAll('a[href="#pricing"]');
    expect(pricingLinks.length).toBeGreaterThan(0);
  });
});
