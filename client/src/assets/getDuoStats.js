import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getDuoStats(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-stats/${userId}`);
  return data;
}

