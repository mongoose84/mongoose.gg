import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getTeamComposition(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-composition/${userId}`);
  return data;
}

