import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getDuoDeathsTrend(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-deaths-trend/${userId}`);
  return data;
}

