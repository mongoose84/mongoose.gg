import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getTeamKillsTrend(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-kills-trend/${userId}`);
  return data;
}

