import { test, expect } from '@playwright/test';

/**
 * Solo Dashboard Flow E2E Tests
 * 
 * Tests the critical user journey:
 * 1. Login with test credentials
 * 2. Verify redirect to User Page
 * 3. Navigate to Solo Dashboard
 * 4. Verify Solo Dashboard loads with data
 */

// Test credentials - pre-verified user with linked Riot account
const TEST_USER = {
  username: 'tester',
  password: 'tester1234',
};

test.describe('Solo Dashboard Flow', () => {
  test.beforeEach(async ({ page }) => {
    // Clear cookies/storage to ensure clean state
    await page.context().clearCookies();
  });

  test('should complete login → UserPage → Solo Dashboard flow', async ({ page }) => {
    // Step 1: Navigate to auth page
    await page.goto('/auth');
    
    // Verify we're on the auth page
    await expect(page.locator('h1')).toContainText('Welcome to Pulse.gg');

    // Step 2: Fill in login form
    await page.getByLabel('Username').fill(TEST_USER.username);
    await page.getByLabel('Password').fill(TEST_USER.password);

    // Step 3: Submit login form
    await page.getByRole('button', { name: /sign in/i }).click();

    // Step 4: Wait for navigation to User Page
    await expect(page).toHaveURL('/app/user', { timeout: 15_000 });

    // Step 5: Verify User Page loaded successfully
    // Check for profile header or username display
    await expect(page.getByText(TEST_USER.username, { exact: false })).toBeVisible({ timeout: 10_000 });

    // Step 6: Navigate to Solo Dashboard
    // Look for the solo dashboard navigation link/button
    const soloDashboardLink = page.getByRole('link', { name: /solo/i }).or(
      page.getByRole('button', { name: /solo/i })
    ).or(
      page.locator('[href="/app/solo"]')
    );
    
    await soloDashboardLink.first().click();

    // Step 7: Verify Solo Dashboard loaded
    await expect(page).toHaveURL('/app/solo', { timeout: 10_000 });

    // Step 8: Verify dashboard content is present
    // Check for profile header card (should show summoner info)
    await expect(page.locator('[class*="profile"]').or(page.locator('[class*="header"]')).first()).toBeVisible({ timeout: 15_000 });
  });

  test('should redirect unauthenticated users to login', async ({ page }) => {
    // Try to access protected route directly
    await page.goto('/app/user');

    // Should redirect to auth page
    await expect(page).toHaveURL(/\/auth/);
  });

  test('should redirect unauthenticated users from solo dashboard to login', async ({ page }) => {
    // Try to access solo dashboard directly without auth
    await page.goto('/app/solo');

    // Should redirect to auth page
    await expect(page).toHaveURL(/\/auth/);
  });

  test('should show error for invalid credentials', async ({ page }) => {
    await page.goto('/auth');

    // Fill in invalid credentials
    await page.getByLabel('Username').fill('invaliduser');
    await page.getByLabel('Password').fill('wrongpassword');

    // Submit
    await page.getByRole('button', { name: /sign in/i }).click();

    // Should show error message
    await expect(page.locator('[class*="error"]').or(page.getByText(/invalid|incorrect|failed/i))).toBeVisible({ timeout: 5_000 });

    // Should still be on auth page
    await expect(page).toHaveURL('/auth');
  });
});

test.describe('Solo Dashboard Content', () => {
  // This test requires being logged in first
  test.beforeEach(async ({ page }) => {
    // Login before each test
    await page.goto('/auth');
    await page.getByLabel('Username').fill(TEST_USER.username);
    await page.getByLabel('Password').fill(TEST_USER.password);
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).toHaveURL('/app/user', { timeout: 15_000 });
  });

  test('should display solo dashboard with stats', async ({ page }) => {
    // Navigate to solo dashboard
    await page.goto('/app/solo');
    await expect(page).toHaveURL('/app/solo');

    // Wait for page to load and check for key dashboard elements
    // These selectors are based on typical dashboard components
    await page.waitForLoadState('networkidle');

    // Verify the page has rendered something (not blank)
    const bodyContent = await page.locator('body').textContent();
    expect(bodyContent?.length).toBeGreaterThan(100);
  });
});

