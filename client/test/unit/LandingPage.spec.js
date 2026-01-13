import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import LandingPage from '@/views/LandingPage.vue';
import { createRouter, createMemoryHistory } from 'vue-router';

describe('LandingPage.vue', () => {
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

  it('renders the landing page', () => {
    const wrapper = createWrapper();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the hero title', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('The Solo Queue Improvement Tracker');
  });

  it('displays features section', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Everything You Need to Climb');
  });

  it('displays pricing section', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Simple, Transparent Pricing');
  });

  it('displays how it works section', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('How It Works');
  });

  it('has CTA buttons', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Start Improving Now');
  });
});
