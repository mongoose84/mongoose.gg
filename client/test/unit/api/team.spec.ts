import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import axios from 'axios'

// Mock axios
vi.mock('axios')

describe('Team API', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  describe('getTeamStats', () => {
    it('fetches team stats for a user', async () => {
      const mockStats = {
        gamesPlayed: 40,
        winRate: 52.5,
        queueType: 'Flex Queue',
        playerCount: 5
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockStats })

      const { getTeamStats } = await import('@/api/team.js')
      const result = await getTeamStats(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/team-stats/1'))
      expect(result).toEqual(mockStats)
    })
  })

  describe('getTeamWinRateTrend', () => {
    it('fetches team win rate trend', async () => {
      const mockTrend = {
        dataPoints: [
          { date: '2024-01-01', winRate: 50 },
          { date: '2024-01-02', winRate: 55 }
        ]
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockTrend })

      const { getTeamWinRateTrend } = await import('@/api/team.js')
      const result = await getTeamWinRateTrend(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/team-win-rate-trend/1'))
      expect(result).toEqual(mockTrend)
    })
  })

  describe('getTeamDurationAnalysis', () => {
    it('fetches team duration analysis', async () => {
      const mockDuration = {
        avgDuration: 30.2,
        shortGameWinRate: 55,
        longGameWinRate: 48
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockDuration })

      const { getTeamDurationAnalysis } = await import('@/api/team.js')
      const result = await getTeamDurationAnalysis(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/team-duration-analysis/1'))
      expect(result).toEqual(mockDuration)
    })
  })

  describe('getTeamMultiKills', () => {
    it('fetches team multi-kill data', async () => {
      const mockMultiKills = {
        totalDoubleKills: 45,
        totalTripleKills: 12,
        totalQuadraKills: 3,
        totalPentaKills: 1
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockMultiKills })

      const { getTeamMultiKills } = await import('@/api/team.js')
      const result = await getTeamMultiKills(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/team-multi-kills/1'))
      expect(result).toEqual(mockMultiKills)
    })
  })

  describe('getTeamSynergy', () => {
    it('fetches team synergy data', async () => {
      const mockSynergy = {
        pairs: [
          { player1: 'Player1', player2: 'Player2', synergyScore: 85 }
        ]
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockSynergy })

      const { getTeamSynergy } = await import('@/api/team.js')
      const result = await getTeamSynergy(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/team-synergy/1'))
      expect(result).toEqual(mockSynergy)
    })
  })

  describe('getTeamChampionCombos', () => {
    it('fetches team champion combos', async () => {
      const mockCombos = {
        combos: [
          { champions: ['Jinx', 'Lulu'], games: 15, winRate: 73 }
        ]
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockCombos })

      const { getTeamChampionCombos } = await import('@/api/team.js')
      const result = await getTeamChampionCombos(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/team-champion-combos/1'))
      expect(result).toEqual(mockCombos)
    })
  })

  describe('getTeamRolePairEffectiveness', () => {
    it('fetches team role pair effectiveness', async () => {
      const mockEffectiveness = {
        pairs: [
          { roles: ['BOTTOM', 'SUPPORT'], effectiveness: 78 }
        ]
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockEffectiveness })

      const { getTeamRolePairEffectiveness } = await import('@/api/team.js')
      const result = await getTeamRolePairEffectiveness(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/team-role-pair-effectiveness/1'))
      expect(result).toEqual(mockEffectiveness)
    })
  })

  describe('getTeamKillParticipation', () => {
    it('fetches team kill participation', async () => {
      const mockParticipation = {
        avgTeamKillParticipation: 72.5,
        playerParticipation: [
          { player: 'Player1', participation: 75 }
        ]
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockParticipation })

      const { getTeamKillParticipation } = await import('@/api/team.js')
      const result = await getTeamKillParticipation(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/team-kill-participation/1'))
      expect(result).toEqual(mockParticipation)
    })
  })
})

