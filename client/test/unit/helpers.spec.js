/**
 * Tests for test helper utilities
 * 
 * These tests verify that our testing utilities work correctly
 * and serve as documentation for how to use them.
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import {
  createMockUser,
  createMockRiotAccount,
  createMockMatch,
  createAuthApiMock,
  createAnalyticsApiMock,
  createApiConfigMock,
  createMockAuthStore,
  setupPinia,
  createDeferredPromise,
  waitFor,
  cleanupMocks
} from '@test/helpers';

describe('API Mock Helpers', () => {
  describe('createMockUser', () => {
    it('creates a user with default values', () => {
      const user = createMockUser();
      
      expect(user.userId).toBe(1);
      expect(user.username).toBe('testuser');
      expect(user.email).toBe('test@example.com');
      expect(user.emailVerified).toBe(true);
      expect(user.tier).toBe('free');
      expect(user.riotAccounts).toEqual([]);
    });

    it('allows overriding specific properties', () => {
      const user = createMockUser({
        userId: 42,
        tier: 'pro',
        emailVerified: false
      });

      expect(user.userId).toBe(42);
      expect(user.tier).toBe('pro');
      expect(user.emailVerified).toBe(false);
      expect(user.username).toBe('testuser'); // Default preserved
    });
  });

  describe('createMockRiotAccount', () => {
    it('creates a Riot account with default values', () => {
      const account = createMockRiotAccount();

      expect(account.puuid).toBe('mock-puuid-123');
      expect(account.gameName).toBe('TestPlayer');
      expect(account.tagLine).toBe('EUW');
      expect(account.tier).toBe('GOLD');
      expect(account.isPrimary).toBe(true);
    });

    it('allows overriding properties', () => {
      const account = createMockRiotAccount({
        gameName: 'Faker',
        tagLine: 'KR1',
        tier: 'CHALLENGER'
      });

      expect(account.gameName).toBe('Faker');
      expect(account.tagLine).toBe('KR1');
      expect(account.tier).toBe('CHALLENGER');
    });
  });

  describe('createMockMatch', () => {
    it('creates a match with default values', () => {
      const match = createMockMatch();

      expect(match.matchId).toBe('EUW1_1234567890');
      expect(match.queueId).toBe(420);
      expect(match.championName).toBe('Darius');
      expect(match.win).toBe(true);
      expect(match.kills).toBe(5);
      expect(match.deaths).toBe(2);
      expect(match.assists).toBe(8);
    });
  });

  describe('createAuthApiMock', () => {
    it('creates mock with all auth API functions', () => {
      const mock = createAuthApiMock();

      expect(mock.login).toBeDefined();
      expect(mock.register).toBeDefined();
      expect(mock.logout).toBeDefined();
      expect(mock.verify).toBeDefined();
      expect(mock.getCurrentUser).toBeDefined();
      expect(mock.linkRiotAccount).toBeDefined();
      expect(mock.unlinkRiotAccount).toBeDefined();
    });

    it('mocks are callable and can be configured', () => {
      const mock = createAuthApiMock();
      mock.login.mockResolvedValue({ success: true });

      expect(mock.login()).resolves.toEqual({ success: true });
    });
  });

  describe('createAnalyticsApiMock', () => {
    it('creates mock with all analytics functions', () => {
      const mock = createAnalyticsApiMock();

      expect(mock.track).toBeDefined();
      expect(mock.trackPageView).toBeDefined();
      expect(mock.trackAuth).toBeDefined();
      expect(mock.trackMatchSelect).toBeDefined();
      expect(mock.getSessionId()).toBe('mock-session-id');
    });
  });

  describe('createMockAuthStore', () => {
    it('creates a store-like object with default values', () => {
      const store = createMockAuthStore();

      expect(store.user).toBeNull();
      expect(store.isLoading).toBe(false);
      expect(store.isAuthenticated).toBe(false);
      expect(store.login).toBeDefined();
      expect(store.logout).toBeDefined();
    });

    it('allows overriding state and getters', () => {
      const mockUser = createMockUser();
      const store = createMockAuthStore({
        user: mockUser,
        isAuthenticated: true
      });

      expect(store.user).toEqual(mockUser);
      expect(store.isAuthenticated).toBe(true);
    });
  });
});

describe('Test Utilities', () => {
  describe('setupPinia', () => {
    it('creates and activates a Pinia instance', () => {
      const pinia = setupPinia();
      expect(pinia).toBeDefined();
    });
  });

  describe('createDeferredPromise', () => {
    it('creates a controllable promise', async () => {
      const { promise, resolve } = createDeferredPromise();
      
      let resolved = false;
      promise.then(() => { resolved = true; });
      
      expect(resolved).toBe(false);
      resolve('done');
      await promise;
      expect(resolved).toBe(true);
    });

    it('can reject the promise', async () => {
      const { promise, reject } = createDeferredPromise();
      
      reject(new Error('test error'));
      
      await expect(promise).rejects.toThrow('test error');
    });
  });

  describe('waitFor', () => {
    it('resolves when condition becomes true', async () => {
      let counter = 0;
      setTimeout(() => { counter = 5; }, 50);

      await waitFor(() => counter === 5, 200);
      expect(counter).toBe(5);
    });

    it('throws if condition never becomes true', async () => {
      await expect(
        waitFor(() => false, 100, 20)
      ).rejects.toThrow('condition not met');
    });
  });
});

