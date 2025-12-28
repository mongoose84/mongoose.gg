import axios from 'axios';
import { getBaseApi } from './getHost.js';

export default async function getDuoRoleConsistency(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-role-consistency/${userId}`);
  return data;
}

