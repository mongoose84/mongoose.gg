import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import ResetPasswordPage from '@/views/ResetPasswordPage.vue';
import { createRouter, createMemoryHistory } from 'vue-router';

// Configurable mock — keeps the default as a resolved success so tests that
// don't care about the API response don't need to set it up themselves.
const mockResetPassword = vi.fn().mockResolvedValue({ success: true });

vi.mock('@/services/authApi', () => ({
  resetPassword: (...args) => mockResetPassword(...args)
}));

describe('ResetPasswordPage.vue', () => {
  let router;

  beforeEach(() => {
    setActivePinia(createPinia());
    mockResetPassword.mockReset().mockResolvedValue({ success: true });

    router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/auth/reset-password', component: { template: '<div>Reset</div>' } },
        { path: '/auth', component: { template: '<div>Auth</div>' } },
      ]
    });
  });

  /**
   * Mounts the page after pushing the given query params to the router so that
   * useRoute() sees them inside onMounted.
   */
  const createWrapper = async (query = {}) => {
    await router.push({ path: '/auth/reset-password', query });
    await router.isReady();
    const wrapper = mount(ResetPasswordPage, {
      global: {
        plugins: [router],
        stubs: { NavBar: true }
      }
    });
    await flushPromises();
    return wrapper;
  };

  // ── Rendering ─────────────────────────────────────────────────────────────

  it('renders the page', async () => {
    const wrapper = await createWrapper();
    expect(wrapper.exists()).toBe(true);
  });

  it('displays the page title', async () => {
    const wrapper = await createWrapper();
    expect(wrapper.text()).toContain('Reset Your Password');
  });

  it('renders email, code, and password inputs', async () => {
    const wrapper = await createWrapper();
    expect(wrapper.find('#reset-email').exists()).toBe(true);
    expect(wrapper.find('#reset-code').exists()).toBe(true);
    expect(wrapper.find('#reset-new-password').exists()).toBe(true);
  });

  // ── Query-param pre-fill ───────────────────────────────────────────────────

  it('pre-fills email from route query param', async () => {
    const wrapper = await createWrapper({ email: 'prefilled@example.com' });
    expect(wrapper.find('#reset-email').element.value).toBe('prefilled@example.com');
  });

  it('leaves email empty when query param is absent', async () => {
    const wrapper = await createWrapper();
    expect(wrapper.find('#reset-email').element.value).toBe('');
  });

  // ── Code input filtering ───────────────────────────────────────────────────

  it('strips non-digit characters from the code input', async () => {
    const wrapper = await createWrapper();
    const codeInput = wrapper.find('#reset-code');
    await codeInput.setValue('abc123def');
    await codeInput.trigger('input');
    expect(codeInput.element.value).toMatch(/^[0-9]*$/);
  });

  it('truncates the code input to 6 characters', async () => {
    const wrapper = await createWrapper();
    const codeInput = wrapper.find('#reset-code');
    await codeInput.setValue('1234567890');
    await codeInput.trigger('input');
    expect(codeInput.element.value.length).toBeLessThanOrEqual(6);
  });

  // ── Submit gating (isFormValid) ────────────────────────────────────────────

  it('submit button is disabled when code has fewer than 6 digits', async () => {
    const wrapper = await createWrapper();
    await wrapper.find('#reset-code').setValue('123');
    await wrapper.find('#reset-new-password').setValue('password123');
    expect(wrapper.find('button[type="submit"]').attributes('disabled')).toBeDefined();
  });

  it('submit button is disabled when password is fewer than 8 characters', async () => {
    const wrapper = await createWrapper();
    await wrapper.find('#reset-code').setValue('123456');
    await wrapper.find('#reset-new-password').setValue('short');
    expect(wrapper.find('button[type="submit"]').attributes('disabled')).toBeDefined();
  });

  it('submit button is enabled when code is 6 digits and password is at least 8 characters', async () => {
    const wrapper = await createWrapper();
    await wrapper.find('#reset-code').setValue('123456');
    await wrapper.find('#reset-new-password').setValue('password123');
    expect(wrapper.find('button[type="submit"]').attributes('disabled')).toBeUndefined();
  });

  // ── Happy path ────────────────────────────────────────────────────────────

  it('calls resetPassword with email, code, and new password', async () => {
    const wrapper = await createWrapper({ email: 'user@example.com' });
    await wrapper.find('#reset-code').setValue('123456');
    await wrapper.find('#reset-new-password').setValue('newpassword1');
    await wrapper.find('form').trigger('submit');
    await flushPromises();

    expect(mockResetPassword).toHaveBeenCalledWith({
      email: 'user@example.com',
      code: '123456',
      newPassword: 'newpassword1'
    });
  });

  it('redirects to /auth?mode=login after a successful reset', async () => {
    const wrapper = await createWrapper({ email: 'user@example.com' });
    await wrapper.find('#reset-code').setValue('123456');
    await wrapper.find('#reset-new-password').setValue('newpassword1');
    await wrapper.find('form').trigger('submit');
    await flushPromises();

    expect(router.currentRoute.value.path).toBe('/auth');
    expect(router.currentRoute.value.query.mode).toBe('login');
  });

  // ── Error handling ────────────────────────────────────────────────────────

  describe('error handling', () => {
    /** Fill the form to a valid state and reject with an error carrying the given code. */
    const submitWithError = async (wrapper, errorCode, errorMessage = 'raw message') => {
      await wrapper.find('#reset-email').setValue('user@example.com');
      await wrapper.find('#reset-code').setValue('123456');
      await wrapper.find('#reset-new-password').setValue('newpassword1');
      const err = new Error(errorMessage);
      err.code = errorCode;
      mockResetPassword.mockRejectedValueOnce(err);
      await wrapper.find('form').trigger('submit');
      await flushPromises();
    };

    it('shows the mapped message for INVALID_CODE', async () => {
      const wrapper = await createWrapper();
      await submitWithError(wrapper, 'INVALID_CODE');
      expect(wrapper.find('[role="alert"]').text()).toBe(
        'Invalid or expired code. Please request a new one.'
      );
    });

    it('shows the mapped message for INVALID_EMAIL', async () => {
      const wrapper = await createWrapper();
      await submitWithError(wrapper, 'INVALID_EMAIL');
      expect(wrapper.find('[role="alert"]').text()).toBe(
        'Please enter a valid email address.'
      );
    });

    it('shows the mapped message for PASSWORD_TOO_SHORT', async () => {
      const wrapper = await createWrapper();
      await submitWithError(wrapper, 'PASSWORD_TOO_SHORT');
      expect(wrapper.find('[role="alert"]').text()).toBe(
        'Password must be at least 8 characters.'
      );
    });

    it('falls back to error.message for an unknown error code', async () => {
      const wrapper = await createWrapper();
      await submitWithError(wrapper, 'UNKNOWN_CODE', 'something specific went wrong');
      expect(wrapper.find('[role="alert"]').text()).toBe('something specific went wrong');
    });

    it('falls back to the generic message when the error carries no message', async () => {
      const wrapper = await createWrapper();
      await wrapper.find('#reset-email').setValue('user@example.com');
      await wrapper.find('#reset-code').setValue('123456');
      await wrapper.find('#reset-new-password').setValue('newpassword1');
      mockResetPassword.mockRejectedValueOnce(new Error());
      await wrapper.find('form').trigger('submit');
      await flushPromises();
      expect(wrapper.find('[role="alert"]').text()).toBe(
        'Something went wrong. Please try again.'
      );
    });
  });

  // ── Submitting state ──────────────────────────────────────────────────────

  it('disables the submit button while the request is in flight', async () => {
    let resolveRequest;
    mockResetPassword.mockReturnValueOnce(new Promise(r => { resolveRequest = r; }));

    const wrapper = await createWrapper();
    await wrapper.find('#reset-code').setValue('123456');
    await wrapper.find('#reset-new-password').setValue('password123');

    // Kick off submit but do NOT await it — the promise is still pending
    wrapper.find('form').trigger('submit');
    await wrapper.vm.$nextTick();

    expect(wrapper.find('button[type="submit"]').attributes('disabled')).toBeDefined();

    // Resolve to avoid dangling promise
    resolveRequest({ success: true });
    await flushPromises();
  });
});

