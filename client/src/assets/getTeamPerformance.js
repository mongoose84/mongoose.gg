import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getTeamPerformance(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-performance/${userId}`);
  return data;
}

