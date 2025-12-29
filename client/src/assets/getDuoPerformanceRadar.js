import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getDuoPerformanceRadar(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-performance-radar/${userId}`);
  return data;
}

