import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import NavBar from '@/components/NavBar.vue';
import { createRouter, createMemoryHistory } from 'vue-router';

// Mock the authStore
vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => ({
    isAuthenticated: false,
    isVerified: false
  })
}));

describe('NavBar.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  const createWrapper = () => {
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/', component: { template: '<div>Home</div>' } },
        { path: '/auth', component: { template: '<div>Auth</div>' } },
        { path: '/app/user', component: { template: '<div>User</div>' } },
      ]
    });

    return mount(NavBar, {
      global: {
        plugins: [router],
      }
    });
  };

  it('renders the NavBar component', () => {
    const wrapper = createWrapper();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the Pulse.gg logo', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Pulse');
  });

  it('has Get Started and Login buttons', () => {
    const wrapper = createWrapper();
    const text = wrapper.text();
    expect(text).toContain('Get Started');
    expect(text).toContain('Login');
  });

  it('has navigation links to features', () => {
    const wrapper = createWrapper();
    const text = wrapper.text();
    expect(text).toContain('Features');
  });
});
