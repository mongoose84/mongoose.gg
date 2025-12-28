import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getDuoVsSoloPerformance(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-vs-solo-performance/${userId}`);
  return data;
}

