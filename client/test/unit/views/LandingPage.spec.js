import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createRouter, createMemoryHistory } from 'vue-router';
import LandingPage from '@/views/LandingPage.vue';
import { getPublicStats } from '@/services/publicApi';

vi.mock('@/services/publicApi', () => ({
  getPublicStats: vi.fn()
}));

const createWrapper = async () => {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/', component: { template: '<div>Home</div>' } },
      { path: '/auth', component: { template: '<div>Auth</div>' } },
      { path: '/privacy', component: { template: '<div>Privacy</div>' } },
      { path: '/cookies', component: { template: '<div>Cookies</div>' } },
      { path: '/terms', component: { template: '<div>Terms</div>' } },
    ]
  });

  const wrapper = mount(LandingPage, {
    global: {
      plugins: [router],
      stubs: { NavBar: true }
    }
  });
  await flushPromises();
  return wrapper;
};

describe('LandingPage.vue', () => {
  beforeEach(() => {
    getPublicStats.mockReset();
    getPublicStats.mockResolvedValue({ totalMatches: 120, activePlayers: 4 });
  });

  it('shows one headline about the player outcome', async () => {
    const wrapper = await createWrapper();
    const headlines = wrapper.findAll('h1');
    expect(headlines).toHaveLength(1);
    expect(headlines[0].text()).toBe('Climb faster with coaching from your own match history');
  });

  it('sends the main button to sign-up and the second one to log in', async () => {
    const wrapper = await createWrapper();
    expect(wrapper.get('[data-testid="hero-signup"]').attributes('href')).toBe('/auth?mode=signup');
    expect(wrapper.get('[data-testid="hero-signup"]').text()).toBe('Create free account');
    expect(wrapper.get('[data-testid="hero-login"]').attributes('href')).toBe('/auth?mode=login');
  });

  it('shows the trust line', async () => {
    const wrapper = await createWrapper();
    const trust = wrapper.get('[data-testid="hero-trust"]').text();
    expect(trust).toContain('Free');
    expect(trust).toContain("Built on Riot's official API");
  });

  it('labels the product preview as an example', async () => {
    const wrapper = await createWrapper();
    expect(wrapper.get('[data-testid="hero-preview"]').text()).toContain('Example');
    expect(wrapper.get('[data-testid="hero-preview"]').findAll('[data-testid="score-ring"]')).toHaveLength(3);
  });

  it('tags each feature with the page it lives on', async () => {
    const wrapper = await createWrapper();
    const tags = wrapper.findAll('[data-testid="feature-page-tag"]').map((tag) => tag.text());
    expect(tags).toEqual(['Overview', 'Champion Select', 'Matches', 'Solo']);
  });

  it('explains the three steps after sign-up', async () => {
    const wrapper = await createWrapper();
    const steps = wrapper.findAll('#how-it-works [data-testid="landing-step"]');
    expect(steps).toHaveLength(3);
    expect(steps[0].text()).toContain('Create your account');
    expect(steps[1].text()).toContain('Link your Riot ID');
    expect(steps[2].text()).toContain('We sync your matches');
  });

  it('has no pricing section or emoji', async () => {
    const wrapper = await createWrapper();
    expect(wrapper.find('#pricing').exists()).toBe(false);
    expect(wrapper.text()).not.toMatch(/\p{Emoji_Presentation}/u);
  });

  it('shows the Riot disclaimer and legal links in the footer', async () => {
    const wrapper = await createWrapper();
    const footer = wrapper.get('[data-testid="landing-footer"]');
    expect(footer.text()).toContain('Not affiliated with Riot Games');
    expect(footer.find('a[href="/privacy"]').exists()).toBe(true);
    expect(footer.find('a[href="/terms"]').exists()).toBe(true);
  });

  it('exposes the example readiness meter to screen readers', async () => {
    const wrapper = await createWrapper();
    const meter = wrapper.get('[data-testid="readiness-meter"]');
    expect(meter.attributes('role')).toBe('meter');
    expect(meter.attributes('aria-valuenow')).toBe('71');
    expect(meter.attributes('aria-valuetext')).toBe('71 out of 100, good to go');
    // 20 segments, round(71 / 5) = 14 switched on
    const segments = meter.findAll('span');
    expect(segments).toHaveLength(20);
    expect(segments.filter((s) => s.classes().includes('bg-primary-accent'))).toHaveLength(14);
  });

  it('shows signed LP with a real minus sign in the match example', async () => {
    const wrapper = await createWrapper();
    const matches = wrapper.get('[data-testid="feature-matches"]').text();
    expect(matches).toContain('+19 LP');
    expect(matches).toContain('−17 LP');
  });

  describe('Solo trend preview', () => {
    it('switches the example range', async () => {
      const wrapper = await createWrapper();
      expect(wrapper.get('[data-testid="trend-title"]').text()).toBe('Deaths down from 6.1 to 5.2');
      expect(wrapper.get('[data-testid="trend-range-last20"]').attributes('aria-pressed')).toBe('true');

      await wrapper.get('[data-testid="trend-range-last50"]').trigger('click');

      expect(wrapper.get('[data-testid="trend-title"]').text()).toBe('Deaths down from 6.6 to 5.2');
      expect(wrapper.get('[data-testid="trend-range-last50"]').attributes('aria-pressed')).toBe('true');
      expect(wrapper.get('[data-testid="trend-range-last20"]').attributes('aria-pressed')).toBe('false');
    });
  });

  describe('proof count', () => {
    it('is hidden while the count is small', async () => {
      const wrapper = await createWrapper();
      expect(wrapper.get('[data-testid="hero-trust"]').text()).not.toContain('matches analysed');
    });

    it('shows the rounded-down count once it is large enough', async () => {
      getPublicStats.mockResolvedValue({ totalMatches: 3_249_000, activePlayers: 900 });
      const wrapper = await createWrapper();
      expect(wrapper.get('[data-testid="hero-trust"]').text()).toContain('3.2M+ matches analysed');
    });

    it('stays hidden just below 10,000 matches', async () => {
      getPublicStats.mockResolvedValue({ totalMatches: 9_999, activePlayers: 50 });
      const wrapper = await createWrapper();
      expect(wrapper.get('[data-testid="hero-trust"]').text()).not.toContain('matches analysed');
    });

    it('appears at exactly 10,000 matches', async () => {
      getPublicStats.mockResolvedValue({ totalMatches: 10_000, activePlayers: 50 });
      const wrapper = await createWrapper();
      expect(wrapper.get('[data-testid="hero-trust"]').text()).toContain('10K+ matches analysed');
    });

    it('keeps the page intact when the stats request fails', async () => {
      const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
      getPublicStats.mockRejectedValue(new Error('offline'));
      const wrapper = await createWrapper();
      expect(wrapper.get('[data-testid="landing-headline"]').exists()).toBe(true);
      expect(wrapper.get('[data-testid="hero-trust"]').text()).not.toContain('matches analysed');
      expect(consoleError).toHaveBeenCalled();
      consoleError.mockRestore();
    });
  });
});
