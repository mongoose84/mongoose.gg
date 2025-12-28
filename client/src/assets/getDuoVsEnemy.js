import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getDuoVsEnemy(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-vs-enemy/${userId}`);
  return data;
}

