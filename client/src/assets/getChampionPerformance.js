import axios from 'axios';
import { getBaseApi } from './getHost.js';

/**
 * Fetches champion performance data split by server (EUNE vs EUW).
 * @param {string|number} userId - The user ID
 * @returns {Promise<{champions: Array<{championName: string, championId: number, servers: Array<{serverName: string, gamesPlayed: number, wins: number, winrate: number}>}>}>}
 */
export default async function getChampionPerformance(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/champion-performance/${userId}`);
  return data;
}

