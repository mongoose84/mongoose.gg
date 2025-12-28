import axios from 'axios';
import { getBaseApi } from './getHost.js';

/**
 * Fetch team deaths trend data for a user.
 * @param {number|string} userId - The user ID.
 * @returns {Promise<Object>} The deaths trend data.
 */
export default async function getTeamDeathsTrend(userId) {
  const base = getBaseApi();
  const response = await axios.get(`${base}/team-deaths-trend/${userId}`);
  return response.data;
}

