import axios from 'axios';
import { getBaseApi } from './getHost.js';

/**
 * Fetches side (blue/red) statistics for a user.
 * @param {string|number} userId - The user ID
 * @param {string} [mode] - Optional mode: 'solo', 'duo', or 'team'. Auto-detected if not provided.
 * @returns {Promise<{
 *   blueGames: number,
 *   blueWins: number,
 *   blueWinRate: number,
 *   redGames: number,
 *   redWins: number,
 *   redWinRate: number,
 *   totalGames: number,
 *   bluePercentage: number,
 *   redPercentage: number
 * }>}
 */
export default async function getSideStats(userId, mode) {
  const base = getBaseApi();
  const params = mode ? { mode } : {};
  const { data } = await axios.get(`${base}/side-stats/${userId}`, { params });
  return data;
}

