import { test, expect } from '@playwright/test'
import { seedAcceptedCookieConsent } from './helpers/app-shell.js'

// The landing page is for logged-out visitors: drop the shared auth state
test.use({ storageState: { cookies: [], origins: [] } })

test.describe('Smoke - Landing page', () => {
  test('@smoke shows the marketing page to a logged-out visitor', async ({ page }) => {
    await seedAcceptedCookieConsent(page)
    await page.goto('/')

    await expect(page.getByRole('heading', { level: 1, name: 'Climb faster with coaching from your own match history' })).toBeVisible()
    await expect(page.getByTestId('hero-signup')).toBeVisible()
    await expect(page.getByTestId('hero-preview')).toContainText('Example')

    const pageTags = page.getByTestId('feature-page-tag')
    await expect(pageTags).toHaveText(['Overview', 'Champion Select', 'Matches', 'Solo'])
    await expect(page.locator('#how-it-works').getByTestId('landing-step')).toHaveCount(3)
    await expect(page.getByTestId('landing-footer')).toContainText('Not affiliated with Riot Games')
  })

  test('@smoke "Create free account" leads to the sign-up form', async ({ page }) => {
    await seedAcceptedCookieConsent(page)
    await page.goto('/')

    await page.getByTestId('hero-signup').click()

    await expect(page).toHaveURL(/\/auth\?mode=signup/)
    await expect(page.getByRole('heading', { level: 1, name: 'Create your free account' })).toBeVisible()
    await expect(page.getByLabel('Email')).toBeVisible()
  })

  test('@smoke header "Log in" leads to the login form', async ({ page }) => {
    await seedAcceptedCookieConsent(page)
    await page.goto('/')

    await page.getByTestId('navbar-login').click()

    await expect(page).toHaveURL(/\/auth\?mode=login/)
    await expect(page.getByRole('heading', { level: 1, name: 'Log in to Mongoose.gg' })).toBeVisible()
    await expect(page.getByTestId('auth-submit')).toHaveText('Log in')
  })

  test('@smoke asks for cookie consent once', async ({ page }) => {
    await page.goto('/')

    const banner = page.getByRole('dialog', { name: 'Cookie consent' })
    await expect(banner).toBeVisible()
    await banner.getByTestId('accept-cookies').click()
    await expect(banner).toBeHidden()

    await page.reload()
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()
    await expect(page.getByRole('dialog', { name: 'Cookie consent' })).toBeHidden()
  })

  test.describe('on a phone', () => {
    test.use({ viewport: { width: 390, height: 844 } })

    test('@smoke opens the menu and jumps to the features', async ({ page }) => {
      await seedAcceptedCookieConsent(page)
      await page.goto('/')

      // Desktop links are hidden below 900px; the menu holds them
      await expect(page.getByTestId('navbar-link-features')).toBeHidden()

      const toggle = page.getByTestId('navbar-menu-toggle')
      await expect(toggle).toHaveAttribute('aria-expanded', 'false')
      await toggle.click()
      await expect(toggle).toHaveAttribute('aria-expanded', 'true')

      const menu = page.getByTestId('navbar-mobile-menu')
      await menu.getByRole('link', { name: 'Features' }).click()

      await expect(menu).toBeHidden()
      await expect(page).toHaveURL(/#features$/)
      await expect(page.locator('#features')).toBeInViewport()

      // No sideways scrolling on a phone
      const overflows = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth)
      expect(overflows).toBe(false)
    })
  })
})
