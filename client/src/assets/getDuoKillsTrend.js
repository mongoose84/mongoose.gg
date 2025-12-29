import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getDuoKillsTrend(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-kills-trend/${userId}`);
  return data;
}

