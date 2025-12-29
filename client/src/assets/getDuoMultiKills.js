import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getDuoMultiKills(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-multi-kills/${userId}`);
  return data;
}

