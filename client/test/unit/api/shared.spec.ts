import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import axios from 'axios'

// Mock axios
vi.mock('axios')

// Mock import.meta.env
vi.stubGlobal('import', { meta: { env: { DEV: true } } })

describe('Shared API', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  describe('getBaseApi', () => {
    it('returns the correct base API URL in development', async () => {
      const { getBaseApi } = await import('@/api/shared.js')
      const baseApi = getBaseApi()
      expect(baseApi).toContain('/api/v1.0')
    })

    it('returns the host URL', async () => {
      const { getHost } = await import('@/api/shared.js')
      const host = getHost()
      expect(host).toBeTruthy()
      expect(typeof host).toBe('string')
    })
  })

  describe('getUsers', () => {
    it('fetches users from the API', async () => {
      const mockUsers = [
        { id: 1, userName: 'Player1', userType: 'solo' },
        { id: 2, userName: 'DuoPartners', userType: 'duo' }
      ]
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockUsers })

      const { getUsers } = await import('@/api/shared.js')
      const result = await getUsers()

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/users'))
      expect(result).toEqual(mockUsers)
    })

    it('throws error on API failure', async () => {
      vi.mocked(axios.get).mockRejectedValueOnce(new Error('Network Error'))

      const { getUsers } = await import('@/api/shared.js')
      await expect(getUsers()).rejects.toThrow('Network Error')
    })
  })

  describe('createUser', () => {
    it('creates a new user with correct payload', async () => {
      const mockResponse = { id: 3, userName: 'NewPlayer', userType: 'solo' }
      vi.mocked(axios.post).mockResolvedValueOnce({ data: mockResponse })

      const { createUser } = await import('@/api/shared.js')
      const result = await createUser('NewPlayer', 'solo', ['Player1#EUW'])

      expect(axios.post).toHaveBeenCalledWith(
        expect.stringContaining('/user'),
        { userName: 'NewPlayer', userType: 'solo', gamers: ['Player1#EUW'] },
        { headers: { 'Content-Type': 'application/json' } }
      )
      expect(result).toEqual(mockResponse)
    })

    it('throws formatted error on API failure', async () => {
      vi.mocked(axios.post).mockRejectedValueOnce({
        response: { data: { error: 'User already exists' } }
      })

      const { createUser } = await import('@/api/shared.js')
      await expect(createUser('ExistingPlayer', 'solo', [])).rejects.toThrow('User already exists')
    })

    it('throws generic error when no specific error message', async () => {
      vi.mocked(axios.post).mockRejectedValueOnce(new Error('Connection failed'))

      const { createUser } = await import('@/api/shared.js')
      await expect(createUser('Player', 'solo', [])).rejects.toThrow('Connection failed')
    })
  })

  describe('getGamers', () => {
    it('fetches gamers for a user', async () => {
      const mockGamers = [
        { puuid: 'abc123', gamerName: 'Player1#EUW', server: 'euw1' },
        { puuid: 'def456', gamerName: 'Player1#EUNE', server: 'eun1' }
      ]
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockGamers })

      const { getGamers } = await import('@/api/shared.js')
      const result = await getGamers(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/gamers/1'))
      expect(result).toEqual(mockGamers)
    })
  })

  describe('getSideStats', () => {
    it('fetches side stats with default mode', async () => {
      const mockStats = {
        blueGames: 30, blueWins: 18, blueWinRate: 60.0,
        redGames: 25, redWins: 12, redWinRate: 48.0
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockStats })

      const { getSideStats } = await import('@/api/shared.js')
      const result = await getSideStats(1)

      expect(axios.get).toHaveBeenCalledWith(
        expect.stringContaining('/side-stats/1'),
        { params: { mode: 'solo' } }
      )
      expect(result).toEqual(mockStats)
    })

    it('fetches side stats with specified mode', async () => {
      const mockStats = { blueGames: 20, redGames: 20 }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockStats })

      const { getSideStats } = await import('@/api/shared.js')
      await getSideStats(1, 'duo')

      expect(axios.get).toHaveBeenCalledWith(
        expect.stringContaining('/side-stats/1'),
        { params: { mode: 'duo' } }
      )
    })
  })

  describe('getRoleDistribution', () => {
    it('fetches role distribution for a user', async () => {
      const mockRoles = {
        roles: [
          { role: 'BOTTOM', games: 50, winRate: 55 },
          { role: 'MIDDLE', games: 30, winRate: 48 }
        ]
      }
      vi.mocked(axios.get).mockResolvedValueOnce({ data: mockRoles })

      const { getRoleDistribution } = await import('@/api/shared.js')
      const result = await getRoleDistribution(1)

      expect(axios.get).toHaveBeenCalledWith(expect.stringContaining('/role-distribution/1'))
      expect(result).toEqual(mockRoles)
    })
  })
})

