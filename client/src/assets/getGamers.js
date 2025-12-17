import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getGamers(userId) {
	const base = getBaseApi();
	const { data } = await axios.get(`${base}/gamers/${userId}`);
	return data;
}