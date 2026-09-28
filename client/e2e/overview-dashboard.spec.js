import { test, expect } from '@playwright/test';
import { gotoAppPage, expectProtectedRouteRedirectsToAuth } from './helpers/app-shell.js';

const HEADER_LOCATOR = '[data-testid="champion-hero"], [data-testid="overview-account-cards"]';

async function gotoOverviewPage(page) {
  await gotoAppPage(page, '/app/overview');
  await expect(page.locator(HEADER_LOCATOR)).toBeVisible({ timeout: 10_000 });
}

/**
 * Overview Dashboard E2E Tests
 *
 * The Overview follows the design-system order:
 * - Champion hero (individual mode) or account cards (Overall mode)
 * - Your champions: ranked champion cards and "Also played"
 * - Today's matches: summary, last match as a MatchRow, Sync matches
 * - Insights
 * - Next steps: Champion Select and Solo
 *
 * Authentication is handled by global-setup.js (test user with a linked Riot account).
 * The test account may have no synced matches, so empty states are accepted where data can be missing.
 */

test.describe('Overview Dashboard - Authentication', () => {
  test('should redirect unauthenticated users to login page', async ({ browser }) => {
    await expectProtectedRouteRedirectsToAuth(browser, '/app/overview');
  });

  test('should be authenticated via global setup', async ({ page }) => {
    await page.goto('/app/overview');
    await expect(page).toHaveURL('/app/overview');
  });
});

test.describe('Overview Dashboard - Content', () => {
  test.beforeEach(async ({ page }) => {
    await gotoOverviewPage(page);
  });

  test('should show one page headline', async ({ page }) => {
    const headline = page.locator('h1');
    await expect(headline).toHaveCount(1);
    await expect(headline).not.toBeEmpty();
  });

  test('should show the player line in the hero, or account cards in Overall mode', async ({ page }) => {
    const hero = page.locator('[data-testid="champion-hero"]');

    if (await hero.isVisible()) {
      await expect(page.locator('[data-testid="champion-hero-player"]')).not.toBeEmpty();
      await expect(page.locator('[data-testid="hero-matches-link"]')).toBeVisible();
    } else {
      const gameName = page.locator('[data-testid="overview-account-cards"] .game-name').first();
      await expect(gameName).toBeVisible();
      await expect(gameName).not.toBeEmpty();
    }
  });

  test('should show today\'s matches with the last match or an empty state', async ({ page }) => {
    const card = page.locator('[data-testid="today-matches-card"]');
    await expect(card).toBeVisible({ timeout: 10_000 });
    await expect(card.getByRole('heading', { name: /today's matches/i })).toBeVisible();
    await expect(card.locator('[data-testid="match-row"], [data-testid="today-matches-empty"]').first()).toBeVisible();
  });

  test('should open the matches page from the last match row', async ({ page }) => {
    const row = page.locator('[data-testid="match-row"]');
    test.skip(await row.count() === 0, 'Test account has no synced matches');

    await row.click();
    await expect(page).toHaveURL(/\/app\/matches/);
  });

  test('should show the Sync matches button', async ({ page }) => {
    const button = page.locator('[data-testid="overview-sync-button"]');
    await expect(button).toBeVisible();
    await expect(button).toHaveText(/sync/i);
  });

  test('should show your champions with cards or an empty state', async ({ page }) => {
    const section = page.locator('[data-testid="overview-champion-pool"]');
    await expect(section).toBeVisible();
    await expect(section.getByRole('heading', { name: 'Your champions' })).toBeVisible();
    await expect(section.locator('[data-testid="champion-card"], [data-testid="champion-pool-empty"]').first()).toBeVisible();
  });

  test('should show the insights section with an insight or an empty state', async ({ page }) => {
    const section = page.locator('[data-testid="overview-insights"]');
    await expect(section).toBeVisible();
    await expect(section.locator('[data-testid="insight-card"], [data-testid="overview-insights-empty"]').first()).toBeVisible();
  });

  test('should navigate to Champion Select from next steps', async ({ page }) => {
    await page.locator('[data-testid="next-step-champion-select"]').getByRole('link').click();
    await expect(page).toHaveURL('/app/champion-select');
  });

  test('should navigate to Solo from next steps', async ({ page }) => {
    await page.locator('[data-testid="next-step-solo"]').getByRole('link').click();
    await expect(page).toHaveURL('/app/solo');
  });
});

test.describe('Overview Dashboard - Navigation', () => {
  test.beforeEach(async ({ page }) => {
    await gotoOverviewPage(page);
  });

  test('should have header navigation visible', async ({ page }) => {
    await expect(page.locator('[data-testid="app-header"]')).toBeVisible({ timeout: 10_000 });
  });

  test('should navigate to Solo dashboard from the header nav', async ({ page }) => {
    const soloLink = page.locator('[data-testid="nav-solo"]');
    await expect(soloLink).toBeVisible({ timeout: 5_000 });
    await soloLink.click();

    await expect(page).toHaveURL('/app/solo');
  });

  test('should navigate to Matches page from the header nav', async ({ page }) => {
    const matchesLink = page.locator('[data-testid="nav-matches"]');
    await expect(matchesLink).toBeVisible({ timeout: 5_000 });
    await matchesLink.click();

    await expect(page).toHaveURL(/\/app\/matches(\/[^/?#]+)?$/);
  });
});

test.describe('Overview Dashboard - Responsive', () => {
  for (const viewport of [{ width: 375, height: 667 }, { width: 768, height: 1024 }]) {
    test(`should stack the sections without horizontal scroll at ${viewport.width}px`, async ({ page }) => {
      await page.setViewportSize(viewport);
      await gotoOverviewPage(page);

      await expect(page.locator('[data-testid="today-matches-card"]')).toBeVisible();
      await expect(page.locator('[data-testid="overview-next-steps"]')).toBeAttached();

      const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
      expect(overflow).toBeLessThanOrEqual(0);
    });
  }
});
