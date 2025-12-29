import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getDuoWinRateTrend(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-win-rate-trend/${userId}`);
  return data;
}

