import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getTeamKillsByPhase(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-kills-by-phase/${userId}`);
  return data;
}

