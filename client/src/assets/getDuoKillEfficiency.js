import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getDuoKillEfficiency(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-kill-efficiency/${userId}`);
  return data;
}

