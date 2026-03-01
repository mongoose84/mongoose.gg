import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { useAuthStore } from '@/stores/authStore';

// Mock the authApi module
vi.mock('@/services/authApi', () => ({
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn(),
  verify: vi.fn(),
  getCurrentUser: vi.fn(),
  changePassword: vi.fn(),
  linkRiotAccount: vi.fn(),
  unlinkRiotAccount: vi.fn(),
  setPrimaryRiotAccount: vi.fn(),
  triggerRiotAccountSync: vi.fn(),
}));

// Mock the apiClient module
vi.mock('@/services/apiClient', () => ({
  setSessionExpiredCallback: vi.fn(),
}));

// Import the mocked modules
import * as authApi from '@/services/authApi';
import { setSessionExpiredCallback } from '@/services/apiClient';

describe('authStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.resetAllMocks();
  });

  describe('initial state', () => {
    it('initializes with default values', () => {
      const store = useAuthStore();
      
      expect(store.user).toBeNull();
      expect(store.isLoading).toBe(false);
      expect(store.isInitialized).toBe(false);
      expect(store.error).toBeNull();
      expect(store.isLinkingAccount).toBe(false);
    });

    it('isAuthenticated is false when user is null', () => {
      const store = useAuthStore();
      expect(store.isAuthenticated).toBe(false);
    });

    it('isVerified is false when user is null', () => {
      const store = useAuthStore();
      expect(store.isVerified).toBe(false);
    });

    it('riotAccounts is empty array when user is null', () => {
      const store = useAuthStore();
      expect(store.riotAccounts).toEqual([]);
    });

    it('hasLinkedAccount is false when no riot accounts', () => {
      const store = useAuthStore();
      expect(store.hasLinkedAccount).toBe(false);
    });
  });

  describe('initialize', () => {
    it('fetches current user on initialize', async () => {
      const mockUser = { userId: 1, username: 'testuser', email: 'test@test.com', emailVerified: true };
      authApi.getCurrentUser.mockResolvedValue(mockUser);

      const store = useAuthStore();
      await store.initialize();

      expect(authApi.getCurrentUser).toHaveBeenCalledOnce();
      expect(store.user).toEqual(mockUser);
      expect(store.isInitialized).toBe(true);
      expect(store.isLoading).toBe(false);
    });

    it('sets user to null if not authenticated', async () => {
      authApi.getCurrentUser.mockRejectedValue(new Error('Unauthorized'));

      const store = useAuthStore();
      await store.initialize();

      expect(store.user).toBeNull();
      expect(store.isInitialized).toBe(true);
    });

    it('only initializes once', async () => {
      authApi.getCurrentUser.mockResolvedValue({ userId: 1 });

      const store = useAuthStore();
      await store.initialize();
      await store.initialize();

      expect(authApi.getCurrentUser).toHaveBeenCalledOnce();
    });

    it('does not clear an already authenticated user with stale initialize result', async () => {
      let resolveCurrentUser;
      authApi.getCurrentUser.mockImplementationOnce(() => new Promise(resolve => {
        resolveCurrentUser = resolve;
      }));

      const store = useAuthStore();
      const initializePromise = store.initialize();

      store.user = { userId: 7, username: 'fresh-login', emailVerified: true };

      resolveCurrentUser(null);
      await initializePromise;

      expect(store.user).toEqual({ userId: 7, username: 'fresh-login', emailVerified: true });
      expect(store.isAuthenticated).toBe(true);
    });

    it('does not replace authenticated user with stale initialize user from different account', async () => {
      let resolveCurrentUser;
      authApi.getCurrentUser.mockImplementationOnce(() => new Promise(resolve => {
        resolveCurrentUser = resolve;
      }));

      const store = useAuthStore();
      const initializePromise = store.initialize();

      store.user = { userId: 7, username: 'fresh-login', emailVerified: true };

      resolveCurrentUser({ userId: 99, username: 'stale-user', emailVerified: true });
      await initializePromise;

      expect(store.user).toEqual({ userId: 7, username: 'fresh-login', emailVerified: true });
      expect(store.isAuthenticated).toBe(true);
    });
  });

  describe('login', () => {
    it('calls login API and fetches user data', async () => {
      const mockUser = { userId: 1, username: 'testuser', emailVerified: true };
      authApi.login.mockResolvedValue({ success: true });
      authApi.getCurrentUser.mockResolvedValue(mockUser);

      const store = useAuthStore();
      const result = await store.login({ username: 'testuser', password: 'password123' });

      expect(authApi.login).toHaveBeenCalledWith({ username: 'testuser', password: 'password123', rememberMe: false });
      expect(authApi.getCurrentUser).toHaveBeenCalled();
      expect(store.user).toEqual(mockUser);
      expect(store.isAuthenticated).toBe(true);
      expect(result).toEqual({ success: true, emailVerified: true });
    });

    it('passes rememberMe option to API', async () => {
      authApi.login.mockResolvedValue({ success: true });
      authApi.getCurrentUser.mockResolvedValue({ userId: 1, emailVerified: true });

      const store = useAuthStore();
      await store.login({ username: 'testuser', password: 'password123', rememberMe: true });

      expect(authApi.login).toHaveBeenCalledWith({ username: 'testuser', password: 'password123', rememberMe: true });
    });

    it('sets error on login failure', async () => {
      const error = new Error('Invalid credentials');
      authApi.login.mockRejectedValue(error);

      const store = useAuthStore();
      
      await expect(store.login({ username: 'testuser', password: 'wrong' })).rejects.toThrow('Invalid credentials');
      expect(store.error).toBe('Invalid credentials');
      expect(store.user).toBeNull();
    });

    it('sets isLoading during login', async () => {
      let resolveLogin;
      authApi.login.mockReturnValue(new Promise(resolve => { resolveLogin = resolve; }));

      const store = useAuthStore();
      const loginPromise = store.login({ username: 'test', password: 'test' });

      expect(store.isLoading).toBe(true);
      
      resolveLogin({ success: true });
      authApi.getCurrentUser.mockResolvedValue({ userId: 1, emailVerified: true });
      await loginPromise;

      expect(store.isLoading).toBe(false);
    });
  });

  describe('register', () => {
    it('calls register API and fetches user data', async () => {
      const mockUser = { userId: 1, username: 'newuser', email: 'new@test.com', emailVerified: false };
      authApi.register.mockResolvedValue({ success: true });
      authApi.getCurrentUser.mockResolvedValue(mockUser);

      const store = useAuthStore();
      const result = await store.register({ username: 'newuser', email: 'new@test.com', password: 'password123' });

      expect(authApi.register).toHaveBeenCalledWith({ username: 'newuser', email: 'new@test.com', password: 'password123' });
      expect(store.user).toEqual(mockUser);
      expect(result).toEqual({ success: true, needsVerification: true });
    });

    it('sets error on registration failure', async () => {
      const error = new Error('Email already exists');
      authApi.register.mockRejectedValue(error);

      const store = useAuthStore();

      await expect(store.register({ username: 'test', email: 'test@test.com', password: 'pass' })).rejects.toThrow();
      expect(store.error).toBe('Email already exists');
    });
  });

  describe('logout', () => {
    it('calls logout API and clears user', async () => {
      authApi.logout.mockResolvedValue();
      authApi.getCurrentUser.mockResolvedValue({ userId: 1, emailVerified: true });

      const store = useAuthStore();
      // First login
      authApi.login.mockResolvedValue({ success: true });
      await store.login({ username: 'test', password: 'test' });
      expect(store.isAuthenticated).toBe(true);

      // Then logout
      await store.logout();

      expect(authApi.logout).toHaveBeenCalled();
      expect(store.user).toBeNull();
      expect(store.isAuthenticated).toBe(false);
    });

    it('clears user even if logout API fails', async () => {
      authApi.logout.mockRejectedValue(new Error('Network error'));

      const store = useAuthStore();
      store.user = { userId: 1 }; // Manually set user

      await expect(store.logout()).rejects.toThrow('Network error');
      expect(store.user).toBeNull();
    });
  });

  describe('verify', () => {
    it('calls verify API and refreshes user data', async () => {
      const verifiedUser = { userId: 1, emailVerified: true };
      authApi.verify.mockResolvedValue({ success: true });
      authApi.getCurrentUser.mockResolvedValue(verifiedUser);

      const store = useAuthStore();
      const result = await store.verify('123456');

      expect(authApi.verify).toHaveBeenCalledWith('123456');
      expect(authApi.getCurrentUser).toHaveBeenCalled();
      expect(store.user).toEqual(verifiedUser);
      expect(store.isVerified).toBe(true);
      expect(result).toEqual({ success: true });
    });

    it('sets error on verification failure', async () => {
      authApi.verify.mockRejectedValue(new Error('Invalid code'));

      const store = useAuthStore();

      await expect(store.verify('000000')).rejects.toThrow('Invalid code');
      expect(store.error).toBe('Invalid code');
    });
  });

  describe('computed getters', () => {
    it('returns correct username from user', () => {
      const store = useAuthStore();
      store.user = { userId: 1, username: 'testuser', email: 'test@test.com' };

      expect(store.username).toBe('testuser');
    });

    it('returns correct email from user', () => {
      const store = useAuthStore();
      store.user = { userId: 1, username: 'testuser', email: 'test@test.com' };

      expect(store.email).toBe('test@test.com');
    });

    it('returns correct tier from user', () => {
      const store = useAuthStore();
      store.user = { userId: 1, tier: 'pro' };

      expect(store.tier).toBe('pro');
    });

    it('returns free tier by default', () => {
      const store = useAuthStore();
      store.user = { userId: 1 };

      expect(store.tier).toBe('free');
    });

    it('returns userId from user', () => {
      const store = useAuthStore();
      store.user = { userId: 42 };

      expect(store.userId).toBe(42);
    });
  });

  describe('riot account management', () => {
    it('returns riot accounts from user', () => {
      const store = useAuthStore();
      const accounts = [{ puuid: 'abc', gameName: 'Player1', tagLine: 'NA1' }];
      store.user = { userId: 1, riotAccounts: accounts };

      expect(store.riotAccounts).toEqual(accounts);
    });

    it('hasLinkedAccount is true when accounts exist', () => {
      const store = useAuthStore();
      store.user = { userId: 1, riotAccounts: [{ puuid: 'abc' }] };

      expect(store.hasLinkedAccount).toBe(true);
    });

    it('returns primary riot account', () => {
      const store = useAuthStore();
      const accounts = [
        { puuid: 'abc', isPrimary: false },
        { puuid: 'def', isPrimary: true },
      ];
      store.user = { userId: 1, riotAccounts: accounts };

      expect(store.primaryRiotAccount).toEqual({ puuid: 'def', isPrimary: true });
    });

    it('returns first account if no primary set', () => {
      const store = useAuthStore();
      const accounts = [
        { puuid: 'abc', isPrimary: false },
        { puuid: 'def', isPrimary: false },
      ];
      store.user = { userId: 1, riotAccounts: accounts };

      expect(store.primaryRiotAccount).toEqual({ puuid: 'abc', isPrimary: false });
    });

    it('linkRiotAccount calls API and refreshes user', async () => {
      const linkedAccount = { puuid: 'new-puuid', gameName: 'NewPlayer', tagLine: 'EUW' };
      authApi.linkRiotAccount.mockResolvedValue(linkedAccount);
      authApi.getCurrentUser.mockResolvedValue({ userId: 1, riotAccounts: [linkedAccount] });

      const store = useAuthStore();
      const result = await store.linkRiotAccount({ gameName: 'NewPlayer', tagLine: 'EUW', region: 'euw1' });

      expect(authApi.linkRiotAccount).toHaveBeenCalledWith({ gameName: 'NewPlayer', tagLine: 'EUW', region: 'euw1' });
      expect(result).toEqual({ success: true, account: linkedAccount });
      expect(store.isLinkingAccount).toBe(false);
    });

    it('unlinkRiotAccount calls API and refreshes user', async () => {
      authApi.unlinkRiotAccount.mockResolvedValue();
      authApi.getCurrentUser.mockResolvedValue({ userId: 1, riotAccounts: [] });

      const store = useAuthStore();
      store.user = { userId: 1, riotAccounts: [{ puuid: 'abc' }] };

      const result = await store.unlinkRiotAccount('abc');

      expect(authApi.unlinkRiotAccount).toHaveBeenCalledWith('abc');
      expect(result).toEqual({ success: true });
    });

    it('setPrimary calls API and refreshes user', async () => {
      authApi.setPrimaryRiotAccount.mockResolvedValue({ success: true });
      authApi.getCurrentUser.mockResolvedValue({
        userId: 1,
        riotAccounts: [
          { puuid: 'abc', isPrimary: true },
          { puuid: 'def', isPrimary: false }
        ]
      });

      const store = useAuthStore();
      const result = await store.setPrimary('abc');

      expect(authApi.setPrimaryRiotAccount).toHaveBeenCalledWith('abc');
      expect(result).toEqual({ success: true });
    });
  });

  describe('changePassword', () => {
    it('calls authApi.changePassword with currentPassword and newPassword', async () => {
      authApi.changePassword.mockResolvedValue();

      const store = useAuthStore();
      await store.changePassword({ currentPassword: 'oldPass1', newPassword: 'newPass1' });

      expect(authApi.changePassword).toHaveBeenCalledWith({
        currentPassword: 'oldPass1',
        newPassword: 'newPass1',
      });
    });

    it('clears user state on success', async () => {
      authApi.changePassword.mockResolvedValue();

      const store = useAuthStore();
      store.user = { userId: 1, username: 'testuser' };

      await store.changePassword({ currentPassword: 'oldPass1', newPassword: 'newPass1' });

      expect(store.user).toBeNull();
    });

    it('returns { success: true } on success', async () => {
      authApi.changePassword.mockResolvedValue();

      const store = useAuthStore();
      const result = await store.changePassword({ currentPassword: 'oldPass1', newPassword: 'newPass1' });

      expect(result).toEqual({ success: true });
    });

    it('resets isLoading to false after success', async () => {
      authApi.changePassword.mockResolvedValue();

      const store = useAuthStore();
      await store.changePassword({ currentPassword: 'oldPass1', newPassword: 'newPass1' });

      expect(store.isLoading).toBe(false);
    });

    it('clears any pre-existing error before calling the API', async () => {
      authApi.changePassword.mockResolvedValue();

      const store = useAuthStore();
      store.error = 'stale error';

      await store.changePassword({ currentPassword: 'oldPass1', newPassword: 'newPass1' });

      expect(store.error).toBeNull();
    });

    it('sets store.error to e.message on failure', async () => {
      const err = new Error('WRONG_PASSWORD');
      authApi.changePassword.mockRejectedValue(err);

      const store = useAuthStore();
      await expect(
        store.changePassword({ currentPassword: 'wrong', newPassword: 'newPass1' })
      ).rejects.toThrow('WRONG_PASSWORD');

      expect(store.error).toBe('WRONG_PASSWORD');
    });

    it('re-throws the error with e.code intact so callers can map it to UI messages', async () => {
      const err = new Error('raw message');
      err.code = 'WRONG_PASSWORD';
      authApi.changePassword.mockRejectedValue(err);

      const store = useAuthStore();
      let caught;
      try {
        await store.changePassword({ currentPassword: 'wrong', newPassword: 'newPass1' });
      } catch (e) {
        caught = e;
      }

      expect(caught).toBeDefined();
      expect(caught.code).toBe('WRONG_PASSWORD');
    });

    it('resets isLoading to false after failure', async () => {
      authApi.changePassword.mockRejectedValue(new Error('WRONG_PASSWORD'));

      const store = useAuthStore();
      await expect(
        store.changePassword({ currentPassword: 'wrong', newPassword: 'newPass1' })
      ).rejects.toThrow();

      expect(store.isLoading).toBe(false);
    });

    it('preserves user state on failure', async () => {
      authApi.changePassword.mockRejectedValue(new Error('WRONG_PASSWORD'));

      const store = useAuthStore();
      store.user = { userId: 1, username: 'testuser' };

      await expect(
        store.changePassword({ currentPassword: 'wrong', newPassword: 'newPass1' })
      ).rejects.toThrow();

      expect(store.user).toEqual({ userId: 1, username: 'testuser' });
    });
  });

  describe('clearError', () => {
    it('clears the error state', () => {
      const store = useAuthStore();
      store.error = 'Some error';

      store.clearError();

      expect(store.error).toBeNull();
    });
  });

  describe('session expiry', () => {
    describe('initial session state', () => {
      it('initializes sessionExpired as false', () => {
        const store = useAuthStore();
        expect(store.sessionExpired).toBe(false);
      });

      it('initializes sessionExpiredMessage as empty', () => {
        const store = useAuthStore();
        expect(store.sessionExpiredMessage).toBe('');
      });
    });

    describe('handleSessionExpired', () => {
      it('sets sessionExpired to true when wasAuthenticated is true', async () => {
        // First login to set wasAuthenticated
        authApi.login.mockResolvedValue({ success: true });
        authApi.getCurrentUser.mockResolvedValue({ userId: 1, emailVerified: true });

        const store = useAuthStore();
        await store.login({ email: 'test@test.com', password: 'pass' });

        // Simulate internal handleSessionExpired call
        // We need to test the behavior, so we call it via initializeSessionHandler
        store.initializeSessionHandler();
        const callback = setSessionExpiredCallback.mock.calls[0][0];
        callback({ error: 'Session expired' });

        expect(store.sessionExpired).toBe(true);
        expect(store.sessionExpiredMessage).toBe('Session expired');
      });

      it('does NOT set sessionExpired when wasAuthenticated is false', () => {
        const store = useAuthStore();

        // Initialize handler and get callback
        store.initializeSessionHandler();
        const callback = setSessionExpiredCallback.mock.calls[0][0];
        callback({ error: 'Session expired' });

        expect(store.sessionExpired).toBe(false);
      });

      it('clears user data when session expires', async () => {
        authApi.login.mockResolvedValue({ success: true });
        authApi.getCurrentUser.mockResolvedValue({ userId: 1, username: 'test', emailVerified: true });

        const store = useAuthStore();
        await store.login({ email: 'test@test.com', password: 'pass' });
        expect(store.user).not.toBeNull();

        store.initializeSessionHandler();
        const callback = setSessionExpiredCallback.mock.calls[0][0];
        callback({});

        expect(store.user).toBeNull();
      });

      it('uses default message when error message not provided', async () => {
        authApi.login.mockResolvedValue({ success: true });
        authApi.getCurrentUser.mockResolvedValue({ userId: 1, emailVerified: true });

        const store = useAuthStore();
        await store.login({ email: 'test@test.com', password: 'pass' });

        store.initializeSessionHandler();
        const callback = setSessionExpiredCallback.mock.calls[0][0];
        callback({});

        expect(store.sessionExpiredMessage).toBe('Your session has expired. Please log in again.');
      });
    });

    describe('clearSessionExpired', () => {
      it('clears sessionExpired state', async () => {
        authApi.login.mockResolvedValue({ success: true });
        authApi.getCurrentUser.mockResolvedValue({ userId: 1, emailVerified: true });

        const store = useAuthStore();
        await store.login({ email: 'test@test.com', password: 'pass' });

        // Trigger session expired
        store.initializeSessionHandler();
        const callback = setSessionExpiredCallback.mock.calls[0][0];
        callback({ error: 'Expired' });

        expect(store.sessionExpired).toBe(true);

        store.clearSessionExpired();

        expect(store.sessionExpired).toBe(false);
        expect(store.sessionExpiredMessage).toBe('');
      });
    });

    describe('initializeSessionHandler', () => {
      it('registers callback with apiClient', () => {
        const store = useAuthStore();
        store.initializeSessionHandler();

        expect(setSessionExpiredCallback).toHaveBeenCalledOnce();
        expect(setSessionExpiredCallback).toHaveBeenCalledWith(expect.any(Function));
      });
    });

    describe('login clears session expired state', () => {
      it('clears sessionExpired on successful login', async () => {
        authApi.login.mockResolvedValue({ success: true });
        authApi.getCurrentUser.mockResolvedValue({ userId: 1, emailVerified: true });

        const store = useAuthStore();

        // Manually set session expired state
        // (simulating a previous session expiry before logging in again)
        await store.login({ email: 'first@test.com', password: 'pass' });
        store.initializeSessionHandler();
        const callback = setSessionExpiredCallback.mock.calls[0][0];
        callback({ error: 'Expired' });

        expect(store.sessionExpired).toBe(true);

        // Login again
        await store.login({ email: 'new@test.com', password: 'newpass' });

        expect(store.sessionExpired).toBe(false);
        expect(store.sessionExpiredMessage).toBe('');
      });
    });
  });
});

