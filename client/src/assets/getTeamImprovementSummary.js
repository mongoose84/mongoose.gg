import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getTeamImprovementSummary(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-improvement-summary/${userId}`);
  return data;
}

