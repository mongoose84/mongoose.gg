import axios from 'axios';
import { getBaseApi } from './getHost.js';

/**
 * Fetch team game duration analysis data for a user.
 * @param {number|string} userId - The user ID.
 * @returns {Promise<Object>} The team duration analysis data.
 */
export default async function getTeamDurationAnalysis(userId) {
  const base = getBaseApi();
  const response = await axios.get(`${base}/team-duration-analysis/${userId}`);
  return response.data;
}

