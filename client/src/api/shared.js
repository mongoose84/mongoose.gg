import axios from 'axios';

// Centralized environment + host configuration for client API calls
const development = import.meta.env.DEV;
const apiVersion = '/api/v1.0';
const host = development
  ? 'http://localhost:5164'
  : 'https://lol-api.agileastronaut.com';

export function getHost() {
  return host;
}

export function getBaseApi() {
  return host + apiVersion;
}

export const isDevelopment = development;
export const apiVersionPath = apiVersion;

// ========================
// User API Functions
// ========================

export async function getUsers() {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/users`);
  return data;
}

export async function createUser(userName, userType, gamers) {
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

export async function getGamers(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/gamers/${userId}`);
  return data;
}

// ========================
// Shared Stats Functions (used by multiple dashboards)
// ========================

export async function getSideStats(userId, mode = 'solo') {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/side-stats/${userId}`, {
    params: { mode }
  });
  return data;
}

export async function getRoleDistribution(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/role-distribution/${userId}`);
  return data;
}

export async function getChampionSynergy(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/champion-synergy/${userId}`);
  return data;
}

