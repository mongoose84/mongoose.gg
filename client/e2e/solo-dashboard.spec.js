import { test, expect } from '@playwright/test';

/**
 * Solo Dashboard Flow E2E Tests
 *
 * Tests the critical user journey:
 * 1. Verify authentication (handled by global setup)
 * 2. Navigate to Solo Dashboard via sidebar
 * 3. Verify Solo Dashboard loads with data
 *
 * Authentication is handled by global-setup.js which:
 * 1. Creates a fresh test user (auto-verified in non-production)
 * 2. Links a Riot account
 * 3. Saves auth state for all tests to reuse
 *
 * @see https://playwright.dev/docs/test-global-setup-teardown
 */

test.describe('Solo Dashboard Flow', () => {
  test('should complete Overview → Solo Dashboard navigation flow', async ({ page }) => {
    // Auth is handled by global-setup.js - go directly to overview
    await page.goto('/app/overview');
    await expect(page).toHaveURL('/app/overview');
    await page.waitForLoadState('networkidle');

    // Navigate to Solo Dashboard via the sidebar navigation
    const sidebar = page.locator('[data-testid="app-sidebar"]');
    const isCollapsed = await sidebar.getAttribute('data-collapsed') === 'true';

    if (isCollapsed) {
      const analysisSection = page.locator('[data-testid="nav-section-analysis"]');
      await analysisSection.hover();
      const popoutSoloLink = page.locator('[data-testid="popout-item-solo"]');
      await expect(popoutSoloLink).toBeVisible({ timeout: 5_000 });
      await Promise.all([
        page.waitForURL('/app/solo', { timeout: 10_000 }),
        popoutSoloLink.click(),
      ]);
    } else {
      const sidebarSoloLink = page.locator('[data-testid="nav-subitem-solo"]');
      await expect(sidebarSoloLink).toBeVisible({ timeout: 5_000 });
      await Promise.all([
        page.waitForURL('/app/solo', { timeout: 10_000 }),
        sidebarSoloLink.click(),
      ]);
    }

    // Verify we're on the Solo Dashboard
    await expect(page).toHaveURL('/app/solo');

    // Verify dashboard content is present
    await expect(page.locator('[data-testid="solo-dashboard"]')).toBeVisible({ timeout: 15_000 });
  });

  test('should redirect unauthenticated users to login', async ({ browser }) => {
    // Create a fresh context WITHOUT storage state (override project default)
    const context = await browser.newContext({ storageState: undefined });
    const page = await context.newPage();

    await page.goto('/app/overview');
    await expect(page).toHaveURL(/\/auth/);

    await context.close();
  });

  test('should redirect unauthenticated users from solo dashboard to login', async ({ browser }) => {
    // Create a fresh context WITHOUT storage state (override project default)
    const context = await browser.newContext({ storageState: undefined });
    const page = await context.newPage();

    await page.goto('/app/solo');
    await expect(page).toHaveURL(/\/auth/);

    await context.close();
  });

  test('should show error for invalid credentials', async ({ browser }) => {
    // Create a fresh context WITHOUT storage state (override project default)
    const context = await browser.newContext({ storageState: undefined });
    const page = await context.newPage();

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

    await context.close();
  });
});

test.describe('Solo Dashboard Content', () => {
  test.beforeEach(async ({ page }) => {
    // Auth state is automatically loaded from global setup
    await page.goto('/app/solo');
    await page.waitForLoadState('networkidle');
  });

  test('should display solo dashboard with stats', async ({ page }) => {
    // Verify we're on the Solo Dashboard
    await expect(page).toHaveURL('/app/solo');

    // Verify the page has rendered something (not blank)
    const bodyContent = await page.locator('body').textContent();
    expect(bodyContent?.length).toBeGreaterThan(100);
  });

  test('should have solo dashboard section visible', async ({ page }) => {
    await expect(page.locator('[data-testid="solo-dashboard"]')).toBeVisible({ timeout: 15_000 });
  });
});

