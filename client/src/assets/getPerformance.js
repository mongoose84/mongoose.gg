import axios from 'axios';
import { getBaseApi } from './getHost.js';

/**
 * Fetches performance timeline data for charts.
 * @param {string|number} userId - The user ID
 * @param {string} period - Time period: '1w', '1m', '3m', '6m', 'all'
 * @returns {Promise<{gamers: Array<{gamerName: string, dataPoints: Array}>}>}
 */
export default async function getPerformance(userId, period = '3m') {
	const base = getBaseApi();
	const { data } = await axios.get(`${base}/performance/${userId}`, {
		params: { period }
	});
	return data;
}
