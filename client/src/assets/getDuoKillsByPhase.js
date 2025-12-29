import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getDuoKillsByPhase(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-kills-by-phase/${userId}`);
  return data;
}

