import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getComparison(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/comparison/${userId}`);
  return data;
}
