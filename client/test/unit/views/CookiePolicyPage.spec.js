import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import CookiePolicyPage from '@/views/CookiePolicyPage.vue';
import { createRouter, createMemoryHistory } from 'vue-router';

describe('CookiePolicyPage.vue', () => {
  const createWrapper = () => {
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/cookie-policy', component: { template: '<div>Cookie Policy</div>' } },
      ]
    });

    return mount(CookiePolicyPage, {
      global: {
        plugins: [router],
        stubs: {
          NavBar: true,
        }
      }
    });
  };

  it('renders the cookie policy page', () => {
    const wrapper = createWrapper();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the Cookie Policy title', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Cookie Policy');
  });

  it('has a last updated date', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Last updated:');
  });

  it('contains overview section', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Overview');
  });

  it('contains cookies we use section', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Cookies We Use');
  });

  it('contains strictly necessary cookies section', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Strictly Necessary Cookies');
  });

  it('contains your preferences section', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Your Preferences');
  });

  it('has the cookie-policy-content testid', () => {
    const wrapper = createWrapper();
    expect(wrapper.find('[data-testid="cookie-policy-content"]').exists()).toBe(true);
  });

  it('contains the authentication cookie name "mongoose-auth"', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('mongoose-auth');
  });

  it('contains contact information (privacy@mongoose.gg)', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('privacy@mongoose.gg');
  });
});
