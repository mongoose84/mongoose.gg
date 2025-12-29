import axios from 'axios';
import { getBaseApi } from './getHost.js';

/**
 * Fetch team deaths by game duration data for a user.
 * @param {number|string} userId - The user ID.
 * @returns {Promise<Object>} The deaths by duration data.
 */
export default async function getTeamDeathsByDuration(userId) {
  const base = getBaseApi();
  const response = await axios.get(`${base}/team-deaths-by-duration/${userId}`);
  return response.data;
}

