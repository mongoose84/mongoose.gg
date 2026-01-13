import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import TermsPage from '@/views/TermsPage.vue';
import { createRouter, createMemoryHistory } from 'vue-router';

describe('TermsPage.vue', () => {
  const createWrapper = () => {
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/terms', component: { template: '<div>Terms</div>' } },
      ]
    });

    return mount(TermsPage, {
      global: {
        plugins: [router],
        stubs: {
          NavBar: true,
        }
      }
    });
  };

  it('renders the terms page', () => {
    const wrapper = createWrapper();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the terms of service title', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Terms of Service');
  });

  it('has a last updated date', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Last updated:');
  });

  it('contains agreement section', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Agreement to Terms');
  });

  it('contains user responsibilities section', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Use License');
  });

  it('contains limitation of liability section', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Limitation of Liability');
  });

  it('contains disclaimer section', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Disclaimer');
  });

  it('contains contact information', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Contact Us');
    expect(wrapper.text()).toContain('legal@pulse.gg');
  });

  it('has properly structured legal content container', () => {
    const wrapper = createWrapper();
    expect(wrapper.find('.legal-content').exists()).toBe(true);
  });

  it('displays multiple legal sections', () => {
    const wrapper = createWrapper();
    const sections = wrapper.findAll('.legal-section');
    expect(sections.length).toBeGreaterThan(0);
  });

  it('mentions Riot Games disclaimer', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Riot Games');
  });
});
