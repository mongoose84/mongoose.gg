/**
 * Shared active-account context helpers for analytics endpoints
 */

const ACTIVE_ACCOUNT_STORAGE_KEY = 'mongoose_active_account'

/**
 * Resolve the current account query parameter.
 * Maps overall mode to `all` and clears stale raw PUUID values.
 * @returns {string}
 */
export function getAccountParam() {
  const activeAccount = localStorage.getItem(ACTIVE_ACCOUNT_STORAGE_KEY) || 'overall'
  if (activeAccount === 'overall' || activeAccount === 'all') {
    return 'all'
  }

  if (!activeAccount.startsWith('acc_')) {
    localStorage.removeItem(ACTIVE_ACCOUNT_STORAGE_KEY)
    return 'all'
  }

  return activeAccount
}

/**
 * Append the active account context to a URLSearchParams instance.
 * @param {URLSearchParams} params
 */
export function appendAccountParam(params) {
  params.append('accountId', getAccountParam())
}
