import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getDuoDeathsByDuration(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-deaths-by-duration/${userId}`);
  return data;
}

