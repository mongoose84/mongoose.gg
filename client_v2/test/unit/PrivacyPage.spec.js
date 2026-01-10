import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import PrivacyPage from '@/views/PrivacyPage.vue';
import { createRouter, createMemoryHistory } from 'vue-router';

describe('PrivacyPage.vue', () => {
  const createWrapper = () => {
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/privacy', component: { template: '<div>Privacy</div>' } },
      ]
    });

    return mount(PrivacyPage, {
      global: {
        plugins: [router],
        stubs: {
          NavBar: true,
        }
      }
    });
  };

  it('renders the privacy page', () => {
    const wrapper = createWrapper();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the privacy policy title', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Privacy Policy');
  });

  it('has a last updated date', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Last updated:');
  });

  it('contains information collection section', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Information We Collect');
  });

  it('contains data usage section', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('How We Use Your Information');
  });

  it('contains data security section', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Data Security');
  });

  it('contains user rights section', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Your Rights');
  });

  it('contains contact information', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Contact Us');
    expect(wrapper.text()).toContain('privacy@pulse.gg');
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
});
