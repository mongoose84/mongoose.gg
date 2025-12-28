import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getDuoImprovementSummary(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-improvement-summary/${userId}`);
  return data;
}

