import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import NavBar from '@/components/NavBar.vue';
import { createRouter, createMemoryHistory } from 'vue-router';

const authState = { isAuthenticated: false, isVerified: false };

vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => authState
}));

describe('NavBar.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    authState.isAuthenticated = false;
    authState.isVerified = false;
  });

  const createWrapper = async () => {
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/', component: { template: '<div>Home</div>' } },
        { path: '/auth', component: { template: '<div>Auth</div>' } },
        { path: '/app/user', component: { template: '<div>User</div>' } },
      ]
    });
    router.push('/');
    await router.isReady();

    return mount(NavBar, {
      global: { plugins: [router] }
    });
  };

  it('shows the logo linking home', async () => {
    const wrapper = await createWrapper();
    const logo = wrapper.get('[data-testid="navbar-logo"]');
    expect(logo.text()).toContain('Mongoose.gg');
    expect(logo.attributes('href')).toBe('/');
  });

  it('links the logo to the app for verified users', async () => {
    authState.isAuthenticated = true;
    authState.isVerified = true;
    const wrapper = await createWrapper();
    expect(wrapper.get('[data-testid="navbar-logo"]').attributes('href')).toBe('/app/user');
  });

  it('has the section links, Log in and one filled Create free account button', async () => {
    const wrapper = await createWrapper();
    expect(wrapper.get('[data-testid="navbar-link-features"]').attributes('href')).toBe('/#features');
    expect(wrapper.get('[data-testid="navbar-link-how-it-works"]').text()).toBe('How it works');
    expect(wrapper.get('[data-testid="navbar-login"]').attributes('href')).toBe('/auth?mode=login');
    const signup = wrapper.get('[data-testid="navbar-signup"]');
    expect(signup.text()).toBe('Create free account');
    expect(signup.attributes('href')).toBe('/auth?mode=signup');
    expect(wrapper.findAll('.mp-btn--primary')).toHaveLength(1);
  });

  it('opens and closes the phone menu', async () => {
    const wrapper = await createWrapper();
    const toggle = wrapper.get('[data-testid="navbar-menu-toggle"]');
    expect(toggle.attributes('aria-expanded')).toBe('false');
    expect(toggle.attributes('aria-label')).toBe('Open menu');
    expect(wrapper.find('[data-testid="navbar-mobile-menu"]').exists()).toBe(false);

    await toggle.trigger('click');
    expect(toggle.attributes('aria-expanded')).toBe('true');
    expect(toggle.attributes('aria-label')).toBe('Close menu');
    const menu = wrapper.get('[data-testid="navbar-mobile-menu"]');
    expect(menu.text()).toContain('Features');
    expect(menu.text()).toContain('Create free account');

    await menu.find('a').trigger('click');
    expect(wrapper.find('[data-testid="navbar-mobile-menu"]').exists()).toBe(false);
  });
});
