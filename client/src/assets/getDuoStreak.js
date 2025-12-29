import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getDuoStreak(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-streak/${userId}`);
  return data;
}

