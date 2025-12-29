import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getDuoDeathShare(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-death-share/${userId}`);
  return data;
}

