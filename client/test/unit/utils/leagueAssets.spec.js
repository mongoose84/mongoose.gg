import { describe, it, expect } from 'vitest'
import {
  DATA_DRAGON_VERSION,
  normalizeChampionName,
  getChampionIconUrl,
  getChampionSplashUrl,
  getRoleIconUrl,
  getProfileIconUrl,
  getItemIconUrl,
  getSummonerSpellIconUrl,
  formatRegion,
  REGION_LABELS
} from '@/utils/leagueAssets'

describe('leagueAssets', () => {
  describe('normalizeChampionName', () => {
    it('removes spaces from champion name', () => {
      expect(normalizeChampionName('Lee Sin')).toBe('LeeSin')
    })

    it("removes apostrophes (e.g., Cho'Gath)", () => {
      expect(normalizeChampionName("Cho'Gath")).toBe('ChoGath')
    })

    it('returns empty string for falsy input', () => {
      expect(normalizeChampionName('')).toBe('')
      expect(normalizeChampionName(null)).toBe('')
      expect(normalizeChampionName(undefined)).toBe('')
    })

    it('keeps alphanumeric characters unchanged', () => {
      expect(normalizeChampionName('Ahri')).toBe('Ahri')
    })

    it('removes dots and special chars (e.g., Dr. Mundo)', () => {
      expect(normalizeChampionName('Dr. Mundo')).toBe('DrMundo')
    })
  })

  describe('getChampionIconUrl', () => {
    it('returns a URL containing the normalized champion name', () => {
      const url = getChampionIconUrl('Lee Sin')
      expect(url).toContain('LeeSin')
    })

    it('returns a URL containing the Data Dragon CDN base', () => {
      const url = getChampionIconUrl('Ahri')
      expect(url).toContain('ddragon.leagueoflegends.com')
    })

    it('includes the current Data Dragon version', () => {
      const url = getChampionIconUrl('Ahri')
      expect(url).toContain(DATA_DRAGON_VERSION)
    })

    it('ends with .png', () => {
      const url = getChampionIconUrl('Ahri')
      expect(url).toMatch(/\.png$/)
    })
  })

  describe('getChampionSplashUrl', () => {
    it('returns a splash URL for a valid champion', () => {
      const url = getChampionSplashUrl('Jinx')
      expect(url).toContain('Jinx_0.jpg')
    })

    it('returns empty string for falsy input', () => {
      expect(getChampionSplashUrl('')).toBe('')
      expect(getChampionSplashUrl(null)).toBe('')
    })
  })

  describe('getRoleIconUrl', () => {
    it('returns a URL containing "top" for TOP role', () => {
      const url = getRoleIconUrl('TOP')
      expect(url).toContain('top')
    })

    it('returns a URL containing "jungle" for JUNGLE role', () => {
      const url = getRoleIconUrl('JUNGLE')
      expect(url).toContain('jungle')
    })

    it('returns a URL containing "middle" for MIDDLE role', () => {
      const url = getRoleIconUrl('MIDDLE')
      expect(url).toContain('middle')
    })

    it('returns a URL containing "bottom" for BOTTOM role', () => {
      const url = getRoleIconUrl('BOTTOM')
      expect(url).toContain('bottom')
    })

    it('returns a URL containing "utility" for UTILITY role', () => {
      const url = getRoleIconUrl('UTILITY')
      expect(url).toContain('utility')
    })

    it('falls back to "fill" for unknown role', () => {
      const url = getRoleIconUrl('UNKNOWN')
      expect(url).toContain('fill')
    })

    it('uses Community Dragon CDN', () => {
      const url = getRoleIconUrl('TOP')
      expect(url).toContain('raw.communitydragon.org')
    })
  })

  describe('getProfileIconUrl', () => {
    it('returns a URL with the profile icon ID', () => {
      const url = getProfileIconUrl(29)
      expect(url).toContain('29.png')
    })

    it('uses Data Dragon CDN', () => {
      const url = getProfileIconUrl(1)
      expect(url).toContain('ddragon.leagueoflegends.com')
    })

    it('includes the current version', () => {
      const url = getProfileIconUrl(1)
      expect(url).toContain(DATA_DRAGON_VERSION)
    })
  })

  describe('getItemIconUrl', () => {
    it('returns a URL with the item ID', () => {
      const url = getItemIconUrl(3157)
      expect(url).toContain('3157.png')
    })

    it('uses Data Dragon CDN', () => {
      const url = getItemIconUrl(3157)
      expect(url).toContain('ddragon.leagueoflegends.com')
    })
  })

  describe('getSummonerSpellIconUrl', () => {
    it('returns a URL with the spell name', () => {
      const url = getSummonerSpellIconUrl('Flash')
      expect(url).toContain('SummonerFlash')
    })

    it('ends with .png', () => {
      const url = getSummonerSpellIconUrl('Ignite')
      expect(url).toMatch(/\.png$/)
    })
  })

  describe('formatRegion', () => {
    it('formats known region codes', () => {
      expect(formatRegion('euw1')).toBe('EUW')
      expect(formatRegion('na1')).toBe('NA')
      expect(formatRegion('kr')).toBe('KR')
    })

    it('is case-insensitive for input', () => {
      expect(formatRegion('EUW1')).toBe('EUW')
    })

    it('uppercases unknown region codes as fallback', () => {
      expect(formatRegion('xyz1')).toBe('XYZ1')
    })

    it('returns empty string for null/undefined input', () => {
      expect(formatRegion(null)).toBe('')
      expect(formatRegion(undefined)).toBe('')
      expect(formatRegion('')).toBe('')
    })
  })

  describe('REGION_LABELS', () => {
    it('contains standard region codes', () => {
      expect(REGION_LABELS).toHaveProperty('euw1', 'EUW')
      expect(REGION_LABELS).toHaveProperty('na1', 'NA')
      expect(REGION_LABELS).toHaveProperty('kr', 'KR')
    })
  })
})
