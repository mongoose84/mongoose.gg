import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import axios from 'axios'

// Mock axios
vi.mock('axios')

describe('Duo API', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  describe('getDuoStats', () => {
    it('fetches duo stats for a user', async () => {
      const mockStats = {
        gamesPlayed: 50,
        winRate: 58.0,
        queueType: 'Ranked Solo/Duo'
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockStats })

      const { getDuoStats } = await import('@/api/duo.js')
      const result = await getDuoStats(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/duo-stats/1'))
      expect(result).toEqual(mockStats)
    })
  })

  describe('getDuoWinRateTrend', () => {
    it('fetches duo win rate trend', async () => {
      const mockTrend = {
        dataPoints: [
          { date: '2024-01-01', winRate: 55 },
          { date: '2024-01-02', winRate: 58 }
        ]
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockTrend })

      const { getDuoWinRateTrend } = await import('@/api/duo.js')
      const result = await getDuoWinRateTrend(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/duo-win-rate-trend/1'))
      expect(result).toEqual(mockTrend)
    })
  })

  describe('getDuoStreak', () => {
    it('fetches duo streak data', async () => {
      const mockStreak = {
        currentStreak: 3,
        streakType: 'win',
        longestWinStreak: 7,
        longestLossStreak: 4
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockStreak })

      const { getDuoStreak } = await import('@/api/duo.js')
      const result = await getDuoStreak(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/duo-streak/1'))
      expect(result).toEqual(mockStreak)
    })
  })

  describe('getDuoPerformanceRadar', () => {
    it('fetches duo performance radar data', async () => {
      const mockRadar = {
        metrics: [
          { name: 'Kills', value: 75 },
          { name: 'Deaths', value: 40 },
          { name: 'Assists', value: 85 }
        ]
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockRadar })

      const { getDuoPerformanceRadar } = await import('@/api/duo.js')
      const result = await getDuoPerformanceRadar(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/duo-performance-radar/1'))
      expect(result).toEqual(mockRadar)
    })
  })

  describe('getDuoMultiKills', () => {
    it('fetches duo multi-kill data', async () => {
      const mockMultiKills = {
        doubleKills: 25,
        tripleKills: 8,
        quadraKills: 2,
        pentaKills: 0
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockMultiKills })

      const { getDuoMultiKills } = await import('@/api/duo.js')
      const result = await getDuoMultiKills(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/duo-multi-kills/1'))
      expect(result).toEqual(mockMultiKills)
    })
  })

  describe('getDuoKillParticipation', () => {
    it('fetches duo kill participation', async () => {
      const mockParticipation = {
        avgKillParticipation: 65.5,
        player1Participation: 68.0,
        player2Participation: 63.0
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockParticipation })

      const { getDuoKillParticipation } = await import('@/api/duo.js')
      const result = await getDuoKillParticipation(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/duo-kill-participation/1'))
      expect(result).toEqual(mockParticipation)
    })
  })

  describe('getDuoMatchDuration', () => {
    it('fetches duo match duration data', async () => {
      const mockDuration = {
        avgDuration: 28.5,
        buckets: [
          { range: '20-25', games: 15, winRate: 60 }
        ]
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockDuration })

      const { getDuoMatchDuration } = await import('@/api/duo.js')
      const result = await getDuoMatchDuration(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/duo-match-duration/1'))
      expect(result).toEqual(mockDuration)
    })
  })

  describe('getDuoImprovementSummary', () => {
    it('fetches duo improvement summary', async () => {
      const mockSummary = {
        insights: [
          { type: 'strength', message: 'Strong early game' },
          { type: 'weakness', message: 'Late game struggles' }
        ]
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockSummary })

      const { getDuoImprovementSummary } = await import('@/api/duo.js')
      const result = await getDuoImprovementSummary(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/duo-improvement-summary/1'))
      expect(result).toEqual(mockSummary)
    })
  })
})

