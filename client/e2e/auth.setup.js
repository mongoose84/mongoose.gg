import { test as setup, expect } from '@playwright/test';
import path from 'path';

/**
 * Authentication setup for E2E tests.
 * 
 * This file runs once per browser before all tests that depend on it.
 * It performs login and saves the authentication state to a file that
 * other tests can reuse, avoiding repeated login attempts and rate limiting.
 * 
 * @see https://playwright.dev/docs/auth
 */

// Test credentials from environment variables
const TEST_USER = {
  username: process.env.E2E_TEST_USER || '',
  password: process.env.E2E_TEST_PASSWORD || '',
};

// Auth state file path - Playwright will use this automatically
const authFile = path.join(process.cwd(), 'e2e/.auth/user.json');

setup('authenticate', async ({ page }) => {
  // Skip if no credentials provided
  if (!TEST_USER.username || !TEST_USER.password) {
    console.log('⚠️ E2E credentials not provided, skipping authentication setup');
    return;
  }

  console.log('🔐 Performing authentication setup...');

  // Navigate to login page
  await page.goto('/auth');
  await expect(page.locator('h1')).toContainText('Welcome to Mongoose.gg');

  // Fill in credentials
  await page.getByLabel('Username').fill(TEST_USER.username);
  await page.getByLabel('Password').fill(TEST_USER.password);
  
  // Click sign in
  await page.getByRole('button', { name: /sign in/i }).click();

  // Wait for either successful redirect or error
  await Promise.race([
    page.waitForURL('/app/overview', { timeout: 15_000 }),
    page.waitForSelector('.auth-error', { timeout: 15_000 }),
  ]);

  // Check for login errors
  const errorElement = page.locator('.auth-error');
  if (await errorElement.isVisible()) {
    const errorText = await errorElement.textContent();
    throw new Error(`Authentication setup failed: ${errorText}`);
  }

  // Verify we're on the overview page
  await expect(page).toHaveURL('/app/overview');
  console.log('✅ Authentication successful, saving state...');

  // Save authentication state
  await page.context().storageState({ path: authFile });
  console.log(`✅ Auth state saved to ${authFile}`);
});

