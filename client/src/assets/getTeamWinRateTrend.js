import axios from 'axios';
import { getBaseApi } from './getHost.js';

/**
 * Fetch team win rate trend data for a user.
 * @param {number|string} userId - The user ID.
 * @returns {Promise<Object>} The team win rate trend data.
 */
export default async function getTeamWinRateTrend(userId) {
  const base = getBaseApi();
  const response = await axios.get(`${base}/team-win-rate-trend/${userId}`);
  return response.data;
}

