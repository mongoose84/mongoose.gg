import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getDuoDeathTimerImpact(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-death-timer-impact/${userId}`);
  return data;
}

