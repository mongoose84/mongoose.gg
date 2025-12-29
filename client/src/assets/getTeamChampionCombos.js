import axios from 'axios';
import { getBaseApi } from './getHost.js';

/**
 * Fetch best team champion combinations for a user.
 * @param {number|string} userId - The user ID.
 * @returns {Promise<Object>} The team champion combos data.
 */
export default async function getTeamChampionCombos(userId) {
  const base = getBaseApi();
  const response = await axios.get(`${base}/team-champion-combos/${userId}`);
  return response.data;
}

