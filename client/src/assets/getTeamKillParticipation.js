import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getTeamKillParticipation(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-kill-participation/${userId}`);
  return data;
}

