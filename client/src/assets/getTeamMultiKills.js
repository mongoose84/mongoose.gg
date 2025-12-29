import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getTeamMultiKills(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-multi-kills/${userId}`);
  return data;
}

