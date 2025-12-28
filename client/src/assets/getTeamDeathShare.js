import axios from 'axios';
import { getBaseApi } from './getHost.js';

/**
 * Fetch team death share distribution data for a user.
 * @param {number|string} userId - The user ID.
 * @returns {Promise<Object>} The death share data.
 */
export default async function getTeamDeathShare(userId) {
  const base = getBaseApi();
  const response = await axios.get(`${base}/team-death-share/${userId}`);
  return response.data;
}

