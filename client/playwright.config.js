import { defineConfig, devices } from '@playwright/test';
import dotenv from 'dotenv';
import { fileURLToPath } from 'url';
import { dirname, resolve } from 'path';

// Load environment variables from .env file
// Using fileURLToPath for compatibility with Node.js < 20.11
const __dirname = dirname(fileURLToPath(import.meta.url));
dotenv.config({ path: resolve(__dirname, '.env') });

// Path to store authentication state (shared across browsers)
const authFile = 'e2e/.auth/user.json';

/**
 * Playwright configuration for mongoose.gg E2E tests
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  // Test directory
  testDir: './e2e',

  // Run tests in parallel
  fullyParallel: true,

  // Fail the build on CI if you accidentally left test.only in the source code
  forbidOnly: !!process.env.CI,

  // Retry on CI only
  retries: process.env.CI ? 2 : 0,

  // Limit parallel workers on CI to avoid resource issues
  workers: process.env.CI ? 1 : undefined,

  // Reporter configuration
  reporter: process.env.CI 
    ? [['html', { open: 'never' }], ['github']] 
    : [['html', { open: 'on-failure' }]],

  // Shared settings for all projects
  use: {
    // Base URL for the app - use environment variable or default to local dev
    baseURL: process.env.E2E_BASE_URL || 'http://localhost:5174',

    // Collect trace on first retry
    trace: 'on-first-retry',

    // Screenshot on failure
    screenshot: 'only-on-failure',

    // Video on failure (useful for debugging CI issues)
    video: process.env.CI ? 'on-first-retry' : 'off',
  },

  // Test timeout
  timeout: 30_000,

  // Expect timeout
  expect: {
    timeout: 10_000,
  },

  // Configure projects for Chromium and Firefox
  // Uses setup project pattern for authentication to avoid rate limiting
  // @see https://playwright.dev/docs/auth
  projects: [
    // Setup project - runs once to authenticate and save state
    {
      name: 'setup',
      testMatch: /auth\.setup\.js/,
    },
    // Browser projects - depend on setup and use saved auth state
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        storageState: authFile,
      },
      dependencies: ['setup'],
    },
    {
      name: 'firefox',
      use: {
        ...devices['Desktop Firefox'],
        storageState: authFile,
      },
      dependencies: ['setup'],
    },
  ],

  // Web server configuration - start the Vue dev server
  webServer: [
    {
      command: 'npm run dev',
      url: 'http://localhost:5174',
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
      cwd: '.',
    },
  ],
});

