import { expect } from '@playwright/test'

function escapeForRegex(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}

export async function gotoAppPage(page, path) {
  await page.goto(path, { waitUntil: 'domcontentloaded' })
  await expect(page).toHaveURL(new RegExp(`${escapeForRegex(path)}(?:$|\\?)`))
  await expect(page.locator('[data-testid="app-sidebar"]')).toBeVisible({ timeout: 15_000 })
}

export async function expectProtectedRouteRedirectsToAuth(browser, path) {
  const context = await browser.newContext({ storageState: undefined })
  const page = await context.newPage()

  try {
    await page.goto(path, { waitUntil: 'domcontentloaded' })
    await expect(page).toHaveURL(/\/auth/)
  } finally {
    await context.close()
  }
}
