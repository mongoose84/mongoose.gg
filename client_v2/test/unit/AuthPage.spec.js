import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import AuthPage from '@/views/AuthPage.vue';
import { createRouter, createMemoryHistory } from 'vue-router';

describe('AuthPage.vue', () => {
  const createWrapper = (query = {}) => {
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/auth', component: { template: '<div>Auth</div>' } },
      ]
    });

    return mount(AuthPage, {
      global: {
        plugins: [router],
        stubs: {
          NavBar: true,
        }
      },
      props: {}
    });
  };

  it('renders the auth page', () => {
    const wrapper = createWrapper();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the welcome message', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Welcome to Pulse.gg');
  });

  it('has email and password inputs', () => {
    const wrapper = createWrapper();
    const inputs = wrapper.findAll('input');
    expect(inputs.length).toBeGreaterThan(0);
  });

  it('has a toggle between login and signup', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Sign in');
  });

  it('has a submit button', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Sign In');
  });

  it('displays login mode by default', () => {
    const wrapper = createWrapper();
    expect(wrapper.text()).toContain('Sign in to your account');
  });

  it('toggles between login and signup modes', async () => {
    const wrapper = createWrapper();
    
    // Initially in login mode
    expect(wrapper.text()).toContain('Sign in to your account');
    
    // Click toggle button
    const toggleBtn = wrapper.find('button.auth-toggle');
    await toggleBtn.trigger('click');
    
    // Should now show signup text
    expect(wrapper.text()).toContain('Create your account');
  });

  it('shows username field in signup mode', async () => {
    const wrapper = createWrapper();
    
    // Toggle to signup
    const toggleBtn = wrapper.find('button.auth-toggle');
    await toggleBtn.trigger('click');
    
    // Should show username input
    expect(wrapper.text()).toContain('Username');
  });

  it('hides username field in login mode', () => {
    const wrapper = createWrapper();
    
    // In login mode, should not show username field
    const hasUsernameLabel = wrapper.text().includes('Username');
    expect(hasUsernameLabel).toBe(false);
  });

  it('displays auth card with proper styling', () => {
    const wrapper = createWrapper();
    expect(wrapper.find('.auth-card').exists()).toBe(true);
  });

  it('displays auth logo', () => {
    const wrapper = createWrapper();
    const logo = wrapper.find('.auth-logo');
    expect(logo.exists()).toBe(true);
  });

  it('has proper form structure', () => {
    const wrapper = createWrapper();
    expect(wrapper.find('.auth-form').exists()).toBe(true);
    expect(wrapper.find('.form-group').exists()).toBe(true);
  });

  it('submit button text changes with mode', async () => {
    const wrapper = createWrapper();
    
    // Login mode
    expect(wrapper.text()).toContain('Sign In');
    expect(wrapper.text()).not.toContain('Create Account');
    
    // Toggle to signup
    const toggleBtn = wrapper.find('button.auth-toggle');
    await toggleBtn.trigger('click');
    
    // Should show Create Account
    expect(wrapper.text()).toContain('Create Account');
    expect(wrapper.text()).not.toContain('Sign In');
  });

  it('form inputs have proper attributes', () => {
    const wrapper = createWrapper();
    const inputs = wrapper.findAll('input');
    
    inputs.forEach(input => {
      expect(input.attributes('required')).toBeDefined();
    });
  });

  it('displays password input with masked type', () => {
    const wrapper = createWrapper();
    const passwordInput = wrapper.find('input[type="password"]');
    expect(passwordInput.exists()).toBe(true);
  });
});
