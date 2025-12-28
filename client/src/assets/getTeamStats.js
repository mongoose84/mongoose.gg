import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getTeamStats(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-stats/${userId}`);
  return data;
}

