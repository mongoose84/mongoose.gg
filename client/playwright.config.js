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
const smokeTestPattern = /.*smoke\.spec\.js/;

/**
 * Playwright configuration for mongoose.gg E2E tests
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  // Global setup/teardown for user creation and cleanup
  // @see https://playwright.dev/docs/test-global-setup-teardown
  globalSetup: './e2e/global-setup.js',
  globalTeardown: './e2e/global-teardown.js',

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

  // Split E2E into a quick PR smoke suite and a more comprehensive post-merge regression suite.
  // Authentication is handled by global setup/teardown.
  // @see https://playwright.dev/docs/test-global-setup-teardown
  projects: [
    {
      name: 'smoke-chromium',
      testMatch: smokeTestPattern,
      use: {
        ...devices['Desktop Chrome'],
        storageState: authFile,
      },
    },
    {
      name: 'full-chromium',
      testIgnore: smokeTestPattern,
      use: {
        ...devices['Desktop Chrome'],
        storageState: authFile,
      },
    },
    {
      name: 'full-firefox',
      testIgnore: smokeTestPattern,
      use: {
        ...devices['Desktop Firefox'],
        storageState: authFile,
      },
    },
  ],

  // Web server configuration - start the Vue dev server
  // NOTE: The .NET backend must be started separately with E2E flags:
  //   Auth__AutoVerifyEmail=true RateLimiting__Enabled=false Email__DevMode=true dotnet run --project server/Mongoose.Api
  // In CI, this is handled by the ci-e2e.yml workflow which generates
  // appsettings.Production.json with AutoVerifyEmail: true and RateLimiting.Enabled: false
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

