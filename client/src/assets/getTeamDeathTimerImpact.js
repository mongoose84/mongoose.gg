import axios from 'axios';
import { getBaseApi } from './getHost.js';

/**
 * Fetch team death timer impact data for a user.
 * @param {number|string} userId - The user ID.
 * @returns {Promise<Object>} The death timer impact data.
 */
export default async function getTeamDeathTimerImpact(userId) {
  const base = getBaseApi();
  const response = await axios.get(`${base}/team-death-timer-impact/${userId}`);
  return response.data;
}

