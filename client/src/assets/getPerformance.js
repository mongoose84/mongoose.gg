import axios from 'axios';
import { getBaseApi } from './getHost.js';

/**
 * Fetches performance timeline data for charts.
 * @param {string|number} userId - The user ID
 * @param {string} period - Number of matches to return: '20', '50', '100', 'all'
 * @returns {Promise<{gamers: Array<{gamerName: string, dataPoints: Array}>}>}
 */
export default async function getPerformance(userId, period = '50') {
	const base = getBaseApi();
	const { data } = await axios.get(`${base}/performance/${userId}`, {
		params: { period }
	});
	return data;
}
