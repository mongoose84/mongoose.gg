import axios from 'axios';
import { getBaseApi } from './getHost.js';

// GET /api/v1.0/users => [{ UserId, UserName }, ...]
export default async function getUsers() {
	const base = getBaseApi();
	const { data } = await axios.get(`${base}/users`);
	console.log('Raw API response:', JSON.stringify(data, null, 2));
	return data;
}
