import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getDuoKillParticipation(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-kill-participation/${userId}`);
  return data;
}

