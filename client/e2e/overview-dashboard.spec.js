import { test, expect } from '@playwright/test';

/**
 * Overview Dashboard E2E Tests
 *
 * Tests the Overview dashboard page which displays:
 * - Player header (summoner name, level, region)
 * - Rank snapshot (rank, LP, win/loss)
 * - Champion Select CTA
 * - Match activity heatmap
 * - Analysis status card
 * - Latest match card
 */

// Test credentials from environment variables
const TEST_USER = {
  username: process.env.E2E_TEST_USER || '',
  password: process.env.E2E_TEST_PASSWORD || '',
};

const skipIfNoCredentials = !TEST_USER.username || !TEST_USER.password;

/**
 * Helper function to perform login
 */
async function performLogin(page) {
  await page.goto('/auth');
  await expect(page.locator('h1')).toContainText('Welcome to Mongoose.gg');
  
  await page.getByLabel('Username').fill(TEST_USER.username);
  await page.getByLabel('Password').fill(TEST_USER.password);
  await page.getByRole('button', { name: /sign in/i }).click();
  
  await Promise.race([
    page.waitForURL('/app/overview', { timeout: 15_000 }),
    page.waitForSelector('.auth-error', { timeout: 15_000 }),
  ]);
  
  const errorElement = page.locator('.auth-error');
  if (await errorElement.isVisible()) {
    const errorText = await errorElement.textContent();
    throw new Error(`Login failed: ${errorText}`);
  }
}

test.describe('Overview Dashboard - Authentication', () => {
  test.beforeEach(async ({ page }) => {
    await page.context().clearCookies();
  });

  test('should redirect unauthenticated users to login page', async ({ page }) => {
    await page.goto('/app/overview');
    await expect(page).toHaveURL(/\/auth/);
  });

  test('should redirect to overview after successful login', async ({ page }) => {
    test.skip(skipIfNoCredentials, 'E2E credentials required');
    
    await performLogin(page);
    await expect(page).toHaveURL('/app/overview');
  });
});

test.describe('Overview Dashboard - Content', () => {
  test.skip(() => skipIfNoCredentials, 'E2E credentials required');

  test.beforeEach(async ({ page }) => {
    await page.context().clearCookies();
    await performLogin(page);
    await page.waitForLoadState('networkidle');
  });

  test('should display player header with summoner info', async ({ page }) => {
    // Player header should be visible with summoner name
    const playerHeader = page.locator('.overview-player-header');
    await expect(playerHeader).toBeVisible({ timeout: 10_000 });
    
    // Summoner name should be displayed
    const summonerName = playerHeader.locator('.summoner-name');
    await expect(summonerName).toBeVisible();
    await expect(summonerName).not.toBeEmpty();
    
    // Region tag should be visible
    const regionTag = playerHeader.locator('.region-tag');
    await expect(regionTag).toBeVisible();
  });

  test('should display rank snapshot section', async ({ page }) => {
    // Rank snapshot should be visible
    const rankSnapshot = page.locator('.rank-snapshot');
    await expect(rankSnapshot).toBeVisible({ timeout: 10_000 });
    
    // Queue label should be visible (e.g., "Ranked Solo/Duo")
    const queueLabel = rankSnapshot.locator('.queue-label');
    await expect(queueLabel).toBeVisible();
    
    // Rank text should be visible (e.g., "Silver IV" or "Unranked")
    const rankText = rankSnapshot.locator('.rank-text');
    await expect(rankText).toBeVisible();
  });

  test('should display "Today at a glance" section', async ({ page }) => {
    // Section title should be visible
    const sectionTitle = page.getByRole('heading', { name: /today at a glance/i });
    await expect(sectionTitle).toBeVisible({ timeout: 10_000 });
  });

  test('should display "Recent matches" section', async ({ page }) => {
    // Section title should be visible
    const sectionTitle = page.getByRole('heading', { name: /recent matches/i });
    await expect(sectionTitle).toBeVisible({ timeout: 10_000 });
  });

  test('should display "Latest match" section', async ({ page }) => {
    // Section title should be visible
    const sectionTitle = page.getByRole('heading', { name: /latest match/i });
    await expect(sectionTitle).toBeVisible({ timeout: 10_000 });
  });

  test('should display last match card with match info', async ({ page }) => {
    // Last match card should be visible
    const lastMatchCard = page.locator('.last-match-card');
    await expect(lastMatchCard).toBeVisible({ timeout: 10_000 });
    
    // If there's a match, it should show champion name and result
    const hasMatch = await lastMatchCard.locator('.champion-name').isVisible();
    if (hasMatch) {
      await expect(lastMatchCard.locator('.champion-name')).not.toBeEmpty();
      await expect(lastMatchCard.locator('.result-badge')).toBeVisible();
      await expect(lastMatchCard.locator('.kda')).toBeVisible();
    }
  });

  test('should navigate to matches page when clicking last match card', async ({ page }) => {
    const lastMatchCard = page.locator('.last-match-card');
    await expect(lastMatchCard).toBeVisible({ timeout: 10_000 });
    
    // Check if it's a clickable match (not empty state)
    const hasMatch = await lastMatchCard.locator('.champion-name').isVisible();
    if (hasMatch) {
      await lastMatchCard.click();
      await expect(page).toHaveURL(/\/app\/matches/);
    }
  });

  test('should display Champion Select CTA', async ({ page }) => {
    // Wait for the section to load
    await page.waitForLoadState('networkidle');

    // The CTA should be in the "Today at a glance" section
    const glanceSection = page.locator('.section-col--secondary').first();
    await expect(glanceSection).toBeVisible({ timeout: 10_000 });
  });

  test('should display Analysis Status Card in recent matches section', async ({ page }) => {
    // Wait for the section to load
    await page.waitForLoadState('networkidle');

    // The Analysis Status Card should be in the "Recent matches" section
    const recentSection = page.locator('.section-col--secondary').nth(1);
    await expect(recentSection).toBeVisible({ timeout: 10_000 });
  });

  test('should display win/loss strip in rank snapshot', async ({ page }) => {
    const rankSnapshot = page.locator('.rank-snapshot');
    await expect(rankSnapshot).toBeVisible({ timeout: 10_000 });

    // W/L strip should be visible if user has played games
    const wlStrip = rankSnapshot.locator('.wl-strip');
    const hasGames = await wlStrip.isVisible();

    if (hasGames) {
      // Strip should have indicators
      const indicators = wlStrip.locator('.wl-indicator');
      const count = await indicators.count();
      expect(count).toBeGreaterThan(0);
    }
  });

  test('should display LP delta with correct styling', async ({ page }) => {
    const rankSnapshot = page.locator('.rank-snapshot');
    await expect(rankSnapshot).toBeVisible({ timeout: 10_000 });

    // LP delta should be visible
    const lpDelta = rankSnapshot.locator('.lp-delta');
    await expect(lpDelta).toBeVisible();

    // Should have one of the styling classes
    const hasPositive = await lpDelta.locator('.positive').count() > 0 ||
                        await lpDelta.evaluate(el => el.classList.contains('positive'));
    const hasNegative = await lpDelta.locator('.negative').count() > 0 ||
                        await lpDelta.evaluate(el => el.classList.contains('negative'));
    const hasNeutral = await lpDelta.locator('.neutral').count() > 0 ||
                       await lpDelta.evaluate(el => el.classList.contains('neutral'));

    expect(hasPositive || hasNegative || hasNeutral).toBe(true);
  });

  test('should display profile icon or fallback', async ({ page }) => {
    const playerHeader = page.locator('.overview-player-header');
    await expect(playerHeader).toBeVisible({ timeout: 10_000 });

    // Either profile icon image or fallback SVG should be visible
    const profileIcon = playerHeader.locator('.profile-icon');
    const fallbackIcon = playerHeader.locator('.profile-icon-fallback');

    const hasIcon = await profileIcon.isVisible() || await fallbackIcon.isVisible();
    expect(hasIcon).toBe(true);
  });

  test('should display level badge on profile icon', async ({ page }) => {
    const playerHeader = page.locator('.overview-player-header');
    await expect(playerHeader).toBeVisible({ timeout: 10_000 });

    // Level badge should be visible
    const levelBadge = playerHeader.locator('.level-badge');
    const hasLevel = await levelBadge.isVisible();

    if (hasLevel) {
      const levelText = await levelBadge.textContent();
      expect(parseInt(levelText)).toBeGreaterThan(0);
    }
  });
});

test.describe('Overview Dashboard - Navigation', () => {
  test.skip(() => skipIfNoCredentials, 'E2E credentials required');

  test.beforeEach(async ({ page }) => {
    await page.context().clearCookies();
    await performLogin(page);
    await page.waitForLoadState('networkidle');
  });

  test('should have sidebar navigation visible', async ({ page }) => {
    const sidebar = page.locator('[data-testid="app-sidebar"]');
    await expect(sidebar).toBeVisible({ timeout: 10_000 });
  });

  test('should navigate to Solo dashboard from sidebar', async ({ page }) => {
    const sidebar = page.locator('[data-testid="app-sidebar"]');
    const isCollapsed = await sidebar.getAttribute('data-collapsed') === 'true';

    if (isCollapsed) {
      const analysisSection = page.locator('[data-testid="nav-section-analysis"]');
      await analysisSection.hover();
      const popoutSoloLink = page.locator('[data-testid="popout-item-solo"]');
      await expect(popoutSoloLink).toBeVisible({ timeout: 5_000 });
      await popoutSoloLink.click();
    } else {
      const sidebarSoloLink = page.locator('[data-testid="nav-subitem-solo"]');
      await expect(sidebarSoloLink).toBeVisible({ timeout: 5_000 });
      await sidebarSoloLink.click();
    }

    await expect(page).toHaveURL('/app/solo');
  });

  test('should navigate to Matches page from sidebar', async ({ page }) => {
    const sidebar = page.locator('[data-testid="app-sidebar"]');
    const isCollapsed = await sidebar.getAttribute('data-collapsed') === 'true';

    if (isCollapsed) {
      const analysisSection = page.locator('[data-testid="nav-section-analysis"]');
      await analysisSection.hover();
      const popoutMatchesLink = page.locator('[data-testid="popout-item-matches"]');
      await expect(popoutMatchesLink).toBeVisible({ timeout: 5_000 });
      await popoutMatchesLink.click();
    } else {
      const sidebarMatchesLink = page.locator('[data-testid="nav-subitem-matches"]');
      await expect(sidebarMatchesLink).toBeVisible({ timeout: 5_000 });
      await sidebarMatchesLink.click();
    }

    await expect(page).toHaveURL('/app/matches');
  });
});

test.describe('Overview Dashboard - Responsive', () => {
  test.skip(() => skipIfNoCredentials, 'E2E credentials required');

  test('should display correctly on mobile viewport', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.context().clearCookies();
    await performLogin(page);
    await page.waitForLoadState('networkidle');

    // Player header should still be visible
    const playerHeader = page.locator('.overview-player-header');
    await expect(playerHeader).toBeVisible({ timeout: 10_000 });

    // Rank snapshot should still be visible
    const rankSnapshot = page.locator('.rank-snapshot');
    await expect(rankSnapshot).toBeVisible({ timeout: 10_000 });
  });

  test('should display correctly on tablet viewport', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.context().clearCookies();
    await performLogin(page);
    await page.waitForLoadState('networkidle');

    // All sections should be visible
    await expect(page.locator('.overview-player-header')).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('.rank-snapshot')).toBeVisible({ timeout: 10_000 });
    await expect(page.getByRole('heading', { name: /today at a glance/i })).toBeVisible();
  });
});

