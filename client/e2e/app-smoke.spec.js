import { test, expect } from '@playwright/test'
import { gotoAppPage, expectProtectedRouteRedirectsToAuth } from './helpers/app-shell.js'

const coreRoutes = [
  { navTestId: 'nav-overview', path: '/app/overview', locator: '.rank-snapshot' },
  { navTestId: 'nav-solo', path: '/app/solo', locator: '[data-testid="solo-dashboard"]' },
  { navTestId: 'nav-matches', path: '/app/matches', locator: '[data-testid="matches-page"]' },
  { navTestId: 'nav-champion-select', path: '/app/champion-select', locator: '[data-testid="champion-select-page"]' },
  { navTestId: 'nav-team', path: '/app/team', heading: /team analytics/i },
  { navTestId: 'nav-goals', path: '/app/goals', heading: /goals/i },
  { navTestId: 'nav-feedback', path: '/app/feedback', heading: /send feedback/i },
]

test.describe('Smoke - Core app journey', () => {
  test('@smoke redirects unauthenticated visitors away from protected pages', async ({ browser }) => {
    await expectProtectedRouteRedirectsToAuth(browser, '/app/overview')
  })

  test('@smoke loads the authenticated overview shell', async ({ page }) => {
    await gotoAppPage(page, '/app/overview')
    await expect(page.locator('.rank-snapshot')).toBeVisible({ timeout: 10_000 })
    await expect(page.getByRole('heading', { name: /today at a glance/i })).toBeVisible({ timeout: 10_000 })
  })

  test('@smoke navigates across core pages from the sidebar', async ({ page }) => {
    await gotoAppPage(page, '/app/overview')

    for (const route of coreRoutes) {
      const link = page.locator(`[data-testid="${route.navTestId}"]`)
      await expect(link).toBeVisible({ timeout: 5_000 })

      await Promise.all([
        page.waitForURL(`**${route.path}`, { timeout: 10_000 }),
        link.click(),
      ])

      await expect(page).toHaveURL(route.path)

      if (route.locator) {
        await expect(page.locator(route.locator).first()).toBeVisible({ timeout: 10_000 })
      }

      if (route.heading) {
        await expect(page.getByRole('heading', { name: route.heading })).toBeVisible({ timeout: 10_000 })
      }
    }
  })
})
