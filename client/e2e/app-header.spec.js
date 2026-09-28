import { test, expect } from '@playwright/test'
import { gotoAppPage } from './helpers/app-shell.js'

/**
 * App header E2E tests
 *
 * Covers the design-system app shell that replaced the sidebar:
 * desktop PillNav, the avatar menu (Settings, Feedback, Log out)
 * and the phone bottom tab bar.
 */

test.describe('App header - Desktop', () => {
  test.beforeEach(async ({ page }) => {
    await gotoAppPage(page, '/app/overview')
  })

  test('marks the current page in the pill nav', async ({ page }) => {
    await expect(page.locator('[data-testid="nav-overview"]')).toHaveAttribute('aria-current', 'page')
    await expect(page.locator('[data-testid="nav-matches"]')).not.toHaveAttribute('aria-current', 'page')

    await page.locator('[data-testid="nav-matches"]').click()
    await expect(page).toHaveURL(/\/app\/matches(\/[^/?#]+)?$/)
    await expect(page.locator('[data-testid="nav-matches"]')).toHaveAttribute('aria-current', 'page')
  })

  test('hides the phone tab bar on desktop', async ({ page }) => {
    await expect(page.locator('[data-testid="app-tabbar"]')).toBeHidden()
  })

  test('opens the avatar menu and closes it with Escape', async ({ page }) => {
    const avatar = page.locator('[data-testid="app-header-avatar"]')
    const menu = page.locator('[data-testid="app-header-menu"]')

    await expect(avatar).toHaveAttribute('aria-expanded', 'false')
    await avatar.click()

    await expect(menu).toBeVisible()
    await expect(avatar).toHaveAttribute('aria-expanded', 'true')
    await expect(page.locator('[data-testid="app-header-settings"]')).toBeVisible()
    await expect(page.locator('[data-testid="nav-feedback"]')).toBeVisible()
    await expect(page.locator('[data-testid="app-header-logout"]')).toBeVisible()

    await page.locator('[data-testid="app-header-settings"]').focus()
    await page.keyboard.press('Escape')

    await expect(menu).toBeHidden()
    await expect(avatar).toBeFocused()
  })

  test('closes the avatar menu on an outside click', async ({ page }) => {
    await page.locator('[data-testid="app-header-avatar"]').click()
    await expect(page.locator('[data-testid="app-header-menu"]')).toBeVisible()

    // Click the page content well below the fixed header
    await page.mouse.click(10, 400)

    await expect(page.locator('[data-testid="app-header-menu"]')).toBeHidden()
  })

  test('navigates to Settings from the avatar menu', async ({ page }) => {
    await page.locator('[data-testid="app-header-avatar"]').click()
    await page.locator('[data-testid="app-header-settings"]').click()

    await expect(page).toHaveURL('/app/user')
    await expect(page.locator('[data-testid="app-header-menu"]')).toBeHidden()
  })
})

test.describe('App header - Log out', () => {
  // Log out clears the auth cookie of this browser context only, so it does
  // not affect the shared session other tests reuse.
  test('logs out from the avatar menu', async ({ page }) => {
    await gotoAppPage(page, '/app/overview')

    await page.locator('[data-testid="app-header-avatar"]').click()
    await page.locator('[data-testid="app-header-logout"]').click()

    await expect(page).toHaveURL('/')

    await page.goto('/app/overview', { waitUntil: 'domcontentloaded' })
    await expect(page).toHaveURL(/\/auth/)
  })
})

test.describe('App header - Phone', () => {
  test.use({ viewport: { width: 375, height: 667 } })

  test.beforeEach(async ({ page }) => {
    await gotoAppPage(page, '/app/overview')
  })

  test('shows the tab bar instead of the pill nav', async ({ page }) => {
    await expect(page.locator('[data-testid="app-tabbar"]')).toBeVisible()
    await expect(page.locator('[data-testid="nav-overview"]')).toBeHidden()
    await expect(page.locator('[data-testid="app-header-avatar"]')).toBeVisible()
  })

  test('navigates between pages with the tab bar', async ({ page }) => {
    await expect(page.locator('[data-testid="tab-overview"]')).toHaveAttribute('aria-current', 'page')

    await page.locator('[data-testid="tab-matches"]').click()
    await expect(page).toHaveURL(/\/app\/matches(\/[^/?#]+)?$/)
    await expect(page.locator('[data-testid="tab-matches"]')).toHaveAttribute('aria-current', 'page')

    await page.locator('[data-testid="tab-champion-select"]').click()
    await expect(page).toHaveURL('/app/champion-select')
    await expect(page.locator('[data-testid="tab-champion-select"]')).toContainText('Champ Select')
  })
})
