/**
 * Test Utilities
 * 
 * Common utilities for setting up and running tests.
 * Provides consistent patterns for mounting components, setting up stores,
 * and handling async operations.
 */

import { mount, shallowMount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { vi } from 'vitest';

// ============ Store Setup ============

/**
 * Creates and activates a fresh Pinia instance for testing
 * Call in beforeEach to ensure test isolation
 */
export function setupPinia() {
  const pinia = createPinia();
  setActivePinia(pinia);
  return pinia;
}

// ============ Component Mounting Helpers ============

/**
 * Creates default HeadlessUI stubs for modal/dialog testing
 * Use in global.stubs when mounting components that use HeadlessUI
 */
export const headlessUIStubs = {
  TransitionRoot: {
    template: '<div v-if="show"><slot /></div>',
    props: ['show']
  },
  TransitionChild: {
    template: '<div><slot /></div>'
  },
  Dialog: {
    template: '<div class="modal-root" data-testid="modal-overlay"><slot /></div>',
    props: ['as']
  },
  DialogPanel: {
    template: '<div class="modal-panel" data-testid="modal-content"><slot /></div>',
    props: ['class']
  },
  DialogTitle: {
    template: '<h2><slot /></h2>',
    props: ['as']
  },
  Menu: {
    template: '<div class="menu"><slot /></div>',
    props: ['as']
  },
  MenuButton: {
    template: '<button class="menu-button"><slot /></button>'
  },
  MenuItems: {
    template: '<div class="menu-items"><slot /></div>',
    props: ['as']
  },
  MenuItem: {
    template: '<div class="menu-item"><slot /></div>',
    props: ['as']
  },
  Listbox: {
    template: '<div class="listbox"><slot /></div>',
    props: ['modelValue']
  },
  ListboxButton: {
    template: '<button class="listbox-button"><slot /></button>'
  },
  ListboxOptions: {
    template: '<ul class="listbox-options"><slot /></ul>'
  },
  ListboxOption: {
    template: '<li class="listbox-option"><slot /></li>',
    props: ['value']
  }
};

/**
 * Creates a wrapper with common test configuration
 * @param {Object} component - Vue component to mount
 * @param {Object} options - Mount options (props, stubs, etc.)
 */
export function createWrapper(component, options = {}) {
  const { 
    props = {}, 
    stubs = {}, 
    attachToBody = false,
    shallow = false,
    ...rest 
  } = options;

  const mountFn = shallow ? shallowMount : mount;
  const mountOptions = {
    props,
    global: {
      stubs: {
        ...headlessUIStubs,
        ...stubs
      }
    },
    ...rest
  };

  // Optionally attach to document body (needed for modals/portals)
  if (attachToBody) {
    const container = document.createElement('div');
    document.body.appendChild(container);
    mountOptions.attachTo = container;
  }

  return mountFn(component, mountOptions);
}

// ============ Async Helpers ============

/**
 * Wait for a specific condition to be true
 * @param {Function} condition - Function that returns true when condition is met
 * @param {number} timeout - Maximum time to wait in ms (default: 1000)
 * @param {number} interval - Check interval in ms (default: 50)
 */
export async function waitFor(condition, timeout = 1000, interval = 50) {
  const startTime = Date.now();
  
  while (Date.now() - startTime < timeout) {
    if (condition()) return true;
    await new Promise(resolve => setTimeout(resolve, interval));
  }
  
  throw new Error(`waitFor condition not met within ${timeout}ms`);
}

/**
 * Creates a deferred promise for testing async flows
 * Returns { promise, resolve, reject } to control resolution timing
 */
export function createDeferredPromise() {
  let resolve, reject;
  const promise = new Promise((res, rej) => {
    resolve = res;
    reject = rej;
  });
  return { promise, resolve, reject };
}

// ============ Mock Helpers ============

/**
 * Creates a mock fetch function with configurable responses
 * @param {Object} defaultResponse - Default response for unmocked URLs
 */
export function createMockFetch(defaultResponse = { ok: true }) {
  const mockFetch = vi.fn().mockResolvedValue(defaultResponse);
  global.fetch = mockFetch;
  return mockFetch;
}

/**
 * Clears all mocks and restores original implementations
 * Call in afterEach for clean test isolation
 */
export function cleanupMocks() {
  vi.clearAllMocks();
  vi.restoreAllMocks();
}

// ============ DOM Helpers ============

/**
 * Finds an element by data-testid attribute
 * @param {Wrapper} wrapper - Vue Test Utils wrapper
 * @param {string} testId - The data-testid value
 */
export function findByTestId(wrapper, testId) {
  return wrapper.find(`[data-testid="${testId}"]`);
}

/**
 * Finds all elements by data-testid attribute
 * @param {Wrapper} wrapper - Vue Test Utils wrapper
 * @param {string} testId - The data-testid value
 */
export function findAllByTestId(wrapper, testId) {
  return wrapper.findAll(`[data-testid="${testId}"]`);
}

