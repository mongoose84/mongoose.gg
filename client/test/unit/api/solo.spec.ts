import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import axios from 'axios'

// Mock axios
vi.mock('axios')

describe('Solo API', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  describe('getOverallStats', () => {
    it('fetches overall stats for a user', async () => {
      const mockStats = {
        totalGames: 100,
        wins: 55,
        losses: 45,
        winRate: 55.0,
        avgKDA: 3.2
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockStats })

      const { getOverallStats } = await import('@/api/solo.js')
      const result = await getOverallStats(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/stats/1'))
      expect(result).toEqual(mockStats)
    })

    it('throws error on API failure', async () => {
      vi.mocked(axios.get).mockRejectedValueOnce(new Error('Server Error'))

      const { getOverallStats } = await import('@/api/solo.js')
      await expect(getOverallStats(1)).rejects.toThrow('Server Error')
    })
  })

  describe('getPerformance', () => {
    it('fetches performance data without limit', async () => {
      const mockPerformance = {
        gamers: [
          { gamerName: 'Player1#EUW', dataPoints: [] }
        ]
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockPerformance })

      const { getPerformance } = await import('@/api/solo.js')
      const result = await getPerformance(1)

      expect(axios.get).toHaveBeenCalledWith(
        expect.stringContaining('/performance/1'),
        { params: {} }
      )
      expect(result).toEqual(mockPerformance)
    })

    it('fetches performance data with limit', async () => {
      const mockPerformance = { gamers: [] }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockPerformance })

      const { getPerformance } = await import('@/api/solo.js')
      await getPerformance(1, 50)

      expect(axios.get).toHaveBeenCalledWith(
        expect.stringContaining('/performance/1'),
        { params: { limit: 50 } }
      )
    })
  })

  describe('getComparison', () => {
    it('fetches comparison data for a user', async () => {
      const mockComparison = {
        winrate: [{ value: 55, gamerName: 'Player1#EUW' }],
        kda: [{ value: 3.2, gamerName: 'Player1#EUW' }]
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockComparison })

      const { getComparison } = await import('@/api/solo.js')
      const result = await getComparison(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/comparison/1'))
      expect(result).toEqual(mockComparison)
    })
  })

  describe('getMatchDuration', () => {
    it('fetches match duration data', async () => {
      const mockDuration = {
        gamers: [{
          gamerName: 'Player1#EUW',
          buckets: [
            { durationRange: '20-25', gamesPlayed: 10, winrate: 60 }
          ]
        }]
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockDuration })

      const { getMatchDuration } = await import('@/api/solo.js')
      const result = await getMatchDuration(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/match-duration/1'))
      expect(result).toEqual(mockDuration)
    })
  })

  describe('getChampionPerformance', () => {
    it('fetches champion performance data', async () => {
      const mockChampions = {
        champions: [
          { championName: 'Jinx', servers: [{ gamesPlayed: 20 }] }
        ]
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockChampions })

      const { getChampionPerformance } = await import('@/api/solo.js')
      const result = await getChampionPerformance(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/champion-performance/1'))
      expect(result).toEqual(mockChampions)
    })
  })

  describe('getChampionMatchups', () => {
    it('fetches champion matchups data', async () => {
      const mockMatchups = {
        matchups: [
          {
            championName: 'Jinx',
            role: 'BOTTOM',
            opponents: [{ opponentChampionName: 'Caitlyn', winrate: 55 }]
          }
        ]
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockMatchups })

      const { getChampionMatchups } = await import('@/api/solo.js')
      const result = await getChampionMatchups(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/champion-matchups/1'))
      expect(result).toEqual(mockMatchups)
    })
  })
})

