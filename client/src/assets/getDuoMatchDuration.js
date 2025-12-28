import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getDuoMatchDuration(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-match-duration/${userId}`);
  return data;
}

