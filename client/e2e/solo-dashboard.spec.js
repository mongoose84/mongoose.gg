import { test, expect } from '@playwright/test';

/**
 * Solo Dashboard Flow E2E Tests
 *
 * Tests the critical user journey:
 * 1. Login with test credentials
 * 2. Verify redirect to User Page
 * 3. Navigate to Solo Dashboard via sidebar
 * 4. Verify Solo Dashboard loads with data
 */

// Test credentials - pre-verified user with linked Riot account (from environment variables)
const TEST_USER = {
  username: process.env.E2E_TEST_USER || '',
  password: process.env.E2E_TEST_PASSWORD || '',
};

// Skip tests that require login if credentials are not set
const skipIfNoCredentials = !TEST_USER.username || !TEST_USER.password;

/**
 * Helper function to perform login and handle errors
 */
async function performLogin(page, username, password) {
  await page.goto('/auth');

  // Verify we're on the auth page
  await expect(page.locator('h1')).toContainText('Welcome to Mongoose.gg');

  // Fill in login form
  await page.getByLabel('Username').fill(TEST_USER.username);
  await page.getByLabel('Password').fill(TEST_USER.password);

  // Submit login form
  await page.getByRole('button', { name: /sign in/i }).click();

  // Wait for either navigation OR error message
  // After login, the app redirects to /app/overview
  await Promise.race([
    page.waitForURL('/app/overview', { timeout: 15_000 }),
    page.waitForSelector('.auth-error', { timeout: 15_000 }),
  ]);

  // Check if there's an error message displayed
  const errorElement = page.locator('.auth-error');
  if (await errorElement.isVisible()) {
    const errorText = await errorElement.textContent();
    throw new Error(`Login failed with error: ${errorText}`);
  }
}

test.describe('Solo Dashboard Flow', () => {
  test.beforeEach(async ({ page }) => {
    // Clear cookies/storage to ensure clean state
    await page.context().clearCookies();
  });

  test('should complete login → OverviewPage → Solo Dashboard flow', async ({ page }) => {
    test.skip(skipIfNoCredentials, 'E2E_TEST_USER and E2E_TEST_PASSWORD environment variables are required');

    // Step 1-3: Login
    await performLogin(page, TEST_USER.username, TEST_USER.password);

    // Step 4: Verify we're on the Overview Page (login redirects here)
    await expect(page).toHaveURL('/app/overview');

    // Step 5: Verify Overview Page loaded successfully
    // Wait for page to load
    await page.waitForLoadState('networkidle');

    // Step 6: Navigate to Solo Dashboard via the sidebar navigation
    // The sidebar has an "Analysis" section with "Solo" submenu item
    const sidebarSoloLink = page.locator('.app-sidebar .nav-subitem', { hasText: 'Solo' });

    // If sidebar is collapsed, we need to hover over the Analysis section to reveal the popout menu
    const sidebar = page.locator('.app-sidebar');
    const isCollapsed = await sidebar.evaluate((el) => el.classList.contains('collapsed'));

    if (isCollapsed) {
      // Hover over the Analysis section to reveal popout menu
      const analysisSection = page.locator('.app-sidebar .nav-section.has-popout');
      await analysisSection.hover();
      const popoutSoloLink = page.locator('.app-sidebar .popout-item', { hasText: 'Solo' });
      await expect(popoutSoloLink).toBeVisible({ timeout: 5_000 });
      await Promise.all([
        page.waitForURL('/app/solo', { timeout: 10_000 }),
        popoutSoloLink.click(),
      ]);
    } else {
      // Sidebar is expanded, click the Solo submenu item directly
      await expect(sidebarSoloLink).toBeVisible({ timeout: 5_000 });
      await Promise.all([
        page.waitForURL('/app/solo', { timeout: 10_000 }),
        sidebarSoloLink.click(),
      ]);
    }

    // Step 7: Verify we're on the Solo Dashboard
    await expect(page).toHaveURL('/app/solo');

    // Step 8: Verify dashboard content is present
    // Check for profile header card (should show summoner info)
    await expect(page.locator('[class*="profile"]').or(page.locator('[class*="header"]')).first()).toBeVisible({ timeout: 15_000 });
  });

  test('should redirect unauthenticated users to login', async ({ page }) => {
    // Try to access protected route directly
    await page.goto('/app/overview');

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
  // Skip entire describe block if credentials are not set
  test.skip(() => skipIfNoCredentials, 'E2E_TEST_USER and E2E_TEST_PASSWORD environment variables are required');

  // This test requires being logged in first
  test.beforeEach(async ({ page }) => {
    // Login before each test using the helper function
    await performLogin(page, TEST_USER.username, TEST_USER.password);
  });

  test('should display solo dashboard with stats', async ({ page }) => {
    // Navigate to solo dashboard via sidebar
    // After login (in beforeEach), we're on /app/overview
    await page.waitForLoadState('networkidle');

    // Navigate to Solo Dashboard via the sidebar navigation
    const sidebarSoloLink = page.locator('.app-sidebar .nav-subitem', { hasText: 'Solo' });

    // Check if sidebar is collapsed
    const sidebar = page.locator('.app-sidebar');
    const isCollapsed = await sidebar.evaluate((el) => el.classList.contains('collapsed'));

    if (isCollapsed) {
      // Hover over the Analysis section to reveal popout menu
      const analysisSection = page.locator('.app-sidebar .nav-section.has-popout');
      await analysisSection.hover();
      const popoutSoloLink = page.locator('.app-sidebar .popout-item', { hasText: 'Solo' });
      await expect(popoutSoloLink).toBeVisible({ timeout: 5_000 });
      await popoutSoloLink.click();
    } else {
      // Sidebar is expanded, click the Solo submenu item directly
      await expect(sidebarSoloLink).toBeVisible({ timeout: 5_000 });
      await sidebarSoloLink.click();
    }

    await expect(page).toHaveURL('/app/solo');

    // Wait for page to load and check for key dashboard elements
    await page.waitForLoadState('networkidle');

    // Verify the page has rendered something (not blank)
    const bodyContent = await page.locator('body').textContent();
    expect(bodyContent?.length).toBeGreaterThan(100);
  });
});

