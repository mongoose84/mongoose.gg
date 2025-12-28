import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getChampionSynergy(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/champion-synergy/${userId}`);
  return data;
}

