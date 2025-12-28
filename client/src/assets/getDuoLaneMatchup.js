import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getDuoLaneMatchup(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-lane-matchup/${userId}`);
  return data;
}

