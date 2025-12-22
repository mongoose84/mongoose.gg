import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getOverallStats(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/stats/${userId}`);
  return data; // { gamesPlayed, wins }
}
