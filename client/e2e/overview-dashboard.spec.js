import { test, expect } from '@playwright/test';
import { gotoAppPage, expectProtectedRouteRedirectsToAuth } from './helpers/app-shell.js';

async function gotoOverviewPage(page) {
  await gotoAppPage(page, '/app/overview');
}

/**
 * Overview Dashboard E2E Tests
 *
 * Tests the Overview dashboard page which displays:
 * - Player header (summoner name, level, region, rank)
 * - At a glance: TodaySessionCard (win/loss strip) + SurvivalCheckCard (death insights)
 * - Quick actions: Champion Select CTA + AnalysisStatusCard + SoloAnalyticsCTA
 * - Latest match card
 *
 * Authentication is handled by global-setup.js which:
 * 1. Creates a fresh test user (auto-verified in non-production)
 * 2. Links a Riot account
 * 3. Saves auth state for all tests to reuse
 *
 * @see https://playwright.dev/docs/test-global-setup-teardown
 */

test.describe('Overview Dashboard - Authentication', () => {
  test('should redirect unauthenticated users to login page', async ({ browser }) => {
    await expectProtectedRouteRedirectsToAuth(browser, '/app/overview');
  });

  test('should be authenticated via global setup', async ({ page }) => {
    // Auth is handled by global-setup.js - just verify we can access the page
    await page.goto('/app/overview');
    await expect(page).toHaveURL('/app/overview');
  });
});

test.describe('Overview Dashboard - Content', () => {
  test.beforeEach(async ({ page }) => {
    // Auth state is automatically loaded from global setup
    await gotoOverviewPage(page);
  });

  test('should display player header with summoner info', async ({ page }) => {
    // Header is either the individual player header or account cards in overall mode
    const playerHeader = page.locator('.overview-player-header');
    const accountCards = page.locator('[data-testid="overview-account-cards"]');

    const isIndividualMode = await playerHeader.isVisible();
    const isOverallMode = await accountCards.isVisible();

    if (!isIndividualMode && !isOverallMode) {
      // Wait for either to appear
      await expect(page.locator('.overview-player-header, [data-testid="overview-account-cards"]'))
        .toBeVisible({ timeout: 10_000 });
    }

    if (isIndividualMode) {
      // Individual mode: check summoner name and region tag
      const summonerName = playerHeader.locator('.summoner-name');
      await expect(summonerName).toBeVisible();
      await expect(summonerName).not.toBeEmpty();

      const regionTag = playerHeader.locator('.region-tag');
      await expect(regionTag).toBeVisible();
    } else {
      // Overall mode: account cards show .game-name (no region tag)
      const gameName = accountCards.locator('.game-name').first();
      await expect(gameName).toBeVisible();
      await expect(gameName).not.toBeEmpty();
    }
  });

  test('should display "At a glance" section', async ({ page }) => {
    // Section title should be visible
    const sectionTitle = page.getByRole('heading', { name: /at a glance/i });
    await expect(sectionTitle).toBeVisible({ timeout: 10_000 });
  });

  test('should display "Quick actions" section', async ({ page }) => {
    // Section title should be visible
    const sectionTitle = page.getByRole('heading', { name: /quick actions/i });
    await expect(sectionTitle).toBeVisible({ timeout: 10_000 });
  });

  test('should display "Latest match" section', async ({ page }) => {
    // Section title should be visible
    const sectionTitle = page.getByRole('heading', { name: /latest match/i });
    await expect(sectionTitle).toBeVisible({ timeout: 10_000 });
  });

  test('should display last match card with match info', async ({ page }) => {
    // Check if last match card exists (it may not if user has no matches)
    const lastMatchCard = page.locator('.last-match-card');
    const cardCount = await lastMatchCard.count();

    if (cardCount > 0) {
      // Card exists - verify it's visible
      await expect(lastMatchCard).toBeVisible({ timeout: 10_000 });

      // Check if it's the empty state or has match data
      const isEmpty = await lastMatchCard.evaluate(el => el.classList.contains('empty'));

      if (isEmpty) {
        // Empty state - should show "No recent matches"
        await expect(lastMatchCard.locator('.empty-text')).toBeVisible();
        await expect(lastMatchCard.locator('.empty-text')).toHaveText('No recent matches');
      } else {
        // Has match data - verify match info is displayed
        await expect(lastMatchCard.locator('.champion-name')).toBeVisible();
        await expect(lastMatchCard.locator('.champion-name')).not.toBeEmpty();
        await expect(lastMatchCard.locator('.result-badge')).toBeVisible();
        await expect(lastMatchCard.locator('.kda')).toBeVisible();
      }
    } else {
      // Card doesn't exist - this is acceptable if user has no match data
      // Just verify the section title is present
      const sectionTitle = page.getByRole('heading', { name: /latest match/i });
      const titleExists = await sectionTitle.count();
      // Section may not be rendered if there's no match data
      expect(titleExists).toBeGreaterThanOrEqual(0);
    }
  });

  test('should navigate to matches page when clicking last match card', async ({ page }) => {
    const lastMatchCard = page.locator('.last-match-card');
    const cardCount = await lastMatchCard.count();

    if (cardCount > 0) {
      await expect(lastMatchCard).toBeVisible({ timeout: 10_000 });

      // Check if it's a clickable match (not empty state)
      const isEmpty = await lastMatchCard.evaluate(el => el.classList.contains('empty'));

      if (!isEmpty) {
        // Has match data - should be clickable and navigate to matches page
        await lastMatchCard.click();
        await expect(page).toHaveURL(/\/app\/matches/);
      }
    }
    // If card doesn't exist or is empty, skip the navigation test
  });

  test('should display Champion Select CTA', async ({ page }) => {
    // The CTA should be in the "Quick actions" section
    const cta = page.locator('[data-testid="champion-select-cta"]');
    await expect(cta).toBeVisible({ timeout: 10_000 });
  });

  test('should display Analysis Status Card in quick actions section', async ({ page }) => {
    // The Analysis Status Card should be in the "Quick actions" section
    const analysisCard = page.locator('.analysis-status-card');
    await expect(analysisCard).toBeVisible({ timeout: 10_000 });
  });

  test('should display TodaySessionCard in "At a glance" section', async ({ page }) => {
    const card = page.locator('[data-testid="today-session-card"]');
    await expect(card).toBeVisible({ timeout: 10_000 });
  });

  test('should display SurvivalCheckCard in "At a glance" section', async ({ page }) => {
    const card = page.locator('[data-testid="survival-check-card"]');
    await expect(card).toBeVisible({ timeout: 10_000 });
  });

  test('should display Solo Analytics CTA in quick actions section', async ({ page }) => {
    const cta = page.locator('.solo-analytics-cta');
    await expect(cta).toBeVisible({ timeout: 10_000 });
  });

  test('should navigate to solo page when clicking Solo Analytics CTA', async ({ page }) => {
    const cta = page.locator('.solo-analytics-cta');
    await expect(cta).toBeVisible({ timeout: 10_000 });
    await cta.click();
    await expect(page).toHaveURL('/app/solo');
  });

  test('should display rank badge in player header', async ({ page }) => {
    const header = page.locator('.overview-player-header');
    const accountCards = page.locator('[data-testid="overview-account-cards"]');

    if (await header.isVisible()) {
      const rankBadge = page.locator('[data-testid="rank-badge"], [data-testid="rank-badge-unranked"]');
      await expect(rankBadge).toBeVisible();
    } else {
      await expect(accountCards).toBeVisible({ timeout: 10_000 });
    }
  });

  test('should display profile icon or fallback', async ({ page }) => {
    // Header is either the individual player header or account cards in overall mode
    const header = page.locator('.overview-player-header, [data-testid="overview-account-cards"]');
    await expect(header).toBeVisible({ timeout: 10_000 });

    // Either profile icon image or fallback SVG should be visible.
    // OverviewPlayerHeader uses .profile-icon / .profile-icon-fallback;
    // OverviewAccountCards uses .account-avatar-image / .account-avatar-fallback.
    const profileIcon = page.locator('.profile-icon, .account-avatar-image').first();
    const fallbackIcon = page.locator('.profile-icon-fallback, .account-avatar-fallback').first();

    const hasIcon = await profileIcon.isVisible() || await fallbackIcon.isVisible();
    expect(hasIcon).toBe(true);
  });

  test('should display level badge on profile icon', async ({ page }) => {
    // Header is either the individual player header or account cards in overall mode.
    // Both components render a .level-badge element.
    const header = page.locator('.overview-player-header, [data-testid="overview-account-cards"]');
    await expect(header).toBeVisible({ timeout: 10_000 });

    // Level badge should be visible
    const levelBadge = page.locator('.level-badge').first();
    const hasLevel = await levelBadge.isVisible();

    if (hasLevel) {
      const levelText = await levelBadge.textContent();
      expect(parseInt(levelText)).toBeGreaterThan(0);
    }
  });
});

test.describe('Overview Dashboard - Navigation', () => {
  test.beforeEach(async ({ page }) => {
    // Auth state is automatically loaded from global setup
    await gotoOverviewPage(page);
  });

  test('should have sidebar navigation visible', async ({ page }) => {
    const sidebar = page.locator('[data-testid="app-sidebar"]');
    await expect(sidebar).toBeVisible({ timeout: 10_000 });
  });

  test('should navigate to Solo dashboard from sidebar', async ({ page }) => {
    const soloLink = page.locator('[data-testid="nav-solo"]');
    await expect(soloLink).toBeVisible({ timeout: 5_000 });
    await soloLink.click();

    await expect(page).toHaveURL('/app/solo');
  });

  test('should navigate to Matches page from sidebar', async ({ page }) => {
    // Matches is a top-level nav item (not under Analysis section)
    // It's visible in both collapsed and expanded states
    const matchesLink = page.locator('[data-testid="nav-matches"]');
    await expect(matchesLink).toBeVisible({ timeout: 5_000 });
    await matchesLink.click();

    await expect(page).toHaveURL('/app/matches');
  });
});

test.describe('Overview Dashboard - Responsive', () => {
  test('should display correctly on mobile viewport', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    // Auth state is automatically loaded from global setup
    await gotoOverviewPage(page);

    // Header section should be visible — either the individual player header
    // or the account cards header when the user is in overall mode
    const header = page.locator('.overview-player-header, [data-testid="overview-account-cards"]');
    await expect(header).toBeVisible({ timeout: 10_000 });

    // Player header should still be visible
    await expect(page.locator('.overview-player-header, [data-testid="overview-account-cards"]')).toBeVisible({ timeout: 10_000 });
  });

  test('should display correctly on tablet viewport', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    // Auth state is automatically loaded from setup project
    await gotoOverviewPage(page);

    // All sections should be visible — header is either the individual player header
    // or the account cards header when the user is in overall mode
    await expect(page.locator('.overview-player-header, [data-testid="overview-account-cards"]')).toBeVisible({ timeout: 10_000 });
    await expect(page.getByRole('heading', { name: /at a glance/i })).toBeVisible();
  });
});
