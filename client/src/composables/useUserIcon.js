import { ref, computed } from 'vue'
import { getProfileIconUrl } from '@/utils/leagueAssets'

const USER_ICON_KEY = 'mongoose_user_icon'

/**
 * Curated set of League profile icon IDs for the user icon picker.
 * Grouped by theme for display purposes.
 */
export const ICON_OPTIONS = [
  // Classic / Popular
  29, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
  11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
  21, 22, 23, 24, 25, 26, 27, 28,
  // Extended
  503, 504, 505, 506, 507, 508, 509, 510,
  3150, 3151, 3152, 3153, 3154, 3155,
  4025, 4026, 4027, 4028
]

// Module-level ref so all consumers share the same reactive state
const selectedIconId = ref(parseStoredIcon())

function parseStoredIcon() {
  const stored = localStorage.getItem(USER_ICON_KEY)
  if (!stored) return null
  const parsed = parseInt(stored, 10)
  return isNaN(parsed) ? null : parsed
}

export function useUserIcon() {
  const userIconUrl = computed(() => {
    if (selectedIconId.value === null) return null
    return getProfileIconUrl(selectedIconId.value)
  })

  function setUserIcon(iconId) {
    if (iconId === null) {
      selectedIconId.value = null
      localStorage.removeItem(USER_ICON_KEY)
    } else {
      selectedIconId.value = iconId
      localStorage.setItem(USER_ICON_KEY, String(iconId))
    }
  }

  return { selectedIconId, userIconUrl, setUserIcon }
}
