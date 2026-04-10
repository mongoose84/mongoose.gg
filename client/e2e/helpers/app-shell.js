import { expect } from '@playwright/test'

const CONSENT_KEY = 'mongoose_cookie_consent'
const CONSENT_DATE_KEY = 'mongoose_cookie_consent_date'

function escapeForRegex(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}

export async function seedAcceptedCookieConsent(page) {
  await page.addInitScript(({ consentKey, consentDateKey }) => {
    localStorage.setItem(consentKey, 'accepted')
    localStorage.setItem(consentDateKey, new Date().toISOString())
  }, { consentKey: CONSENT_KEY, consentDateKey: CONSENT_DATE_KEY })
}

export async function gotoAppPage(page, path) {
  await seedAcceptedCookieConsent(page)
  await page.goto(path, { waitUntil: 'domcontentloaded' })
  await expect(page).toHaveURL(new RegExp(`${escapeForRegex(path)}(?:$|\\?)`))
  await expect(page.locator('[data-testid="app-sidebar"]')).toBeVisible({ timeout: 15_000 })
}

export async function expectProtectedRouteRedirectsToAuth(browser, path) {
  const context = await browser.newContext({ storageState: undefined })
  const page = await context.newPage()

  try {
    await seedAcceptedCookieConsent(page)
    await page.goto(path, { waitUntil: 'domcontentloaded' })
    await expect(page).toHaveURL(/\/auth/)
  } finally {
    await context.close()
  }
}
