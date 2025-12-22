import axios from 'axios';
import { getBaseApi } from './getHost';

// POST /api/v1.0/user with JSON body { userName, userType, gamers: [{ gameName, tagLine }, ...] }
export default async function createUser(userName, userType, gamers) {
  const base = getBaseApi();

  try {
    const response = await axios.post(
      `${base}/user`,
      { userName, userType, gamers },
      { headers: { 'Content-Type': 'application/json' } }
    );
    return response.data;
  } catch (e) {
    const errorMsg = e?.response?.data?.error || e.message || 'Request failed';
    throw new Error(errorMsg);
  }
}