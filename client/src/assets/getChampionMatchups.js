import axios from 'axios';
import { getBaseApi } from './getHost.js';

/**
 * Fetches champion matchup data grouped by role.
 * @param {string|number} userId - The user ID
 * @returns {Promise<{matchups: Array<{championName: string, championId: number, role: string, totalGames: number, totalWins: number, winrate: number, opponents: Array<{opponentChampionName: string, opponentChampionId: number, gamesPlayed: number, wins: number, losses: number, winrate: number}>}>}>}
 */
export default async function getChampionMatchups(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/champion-matchups/${userId}`);
  return data;
}

