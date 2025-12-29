import axios from 'axios';
import { getBaseApi } from './getHost.js';

/**
 * Fetch team role pair effectiveness data for a user.
 * @param {number|string} userId - The user ID.
 * @returns {Promise<Object>} The role pair effectiveness data.
 */
export default async function getTeamRolePairEffectiveness(userId) {
  const base = getBaseApi();
  const response = await axios.get(`${base}/team-role-pair-effectiveness/${userId}`);
  return response.data;
}

