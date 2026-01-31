/**
 * Test Helpers - Central Export
 * 
 * Import all test utilities from this single entry point:
 * 
 *   import { 
 *     createMockUser, 
 *     createAuthApiMock, 
 *     setupPinia 
 *   } from '@test/helpers';
 * 
 * Note: To use the @test alias, add to vitest.config.js:
 *   resolve: {
 *     alias: {
 *       '@test': path.resolve(__dirname, 'test')
 *     }
 *   }
 */

// API Mocks
export {
  createMockUser,
  createMockRiotAccount,
  createMockMatch,
  createAuthApiMock,
  createAnalyticsApiMock,
  createApiConfigMock,
  createMockAuthStore
} from './apiMocks.js';

// Test Utilities
export {
  setupPinia,
  headlessUIStubs,
  createWrapper,
  waitFor,
  createDeferredPromise,
  createMockFetch,
  restoreFetch,
  cleanupMocks,
  findByTestId,
  findAllByTestId
} from './testUtils.js';

