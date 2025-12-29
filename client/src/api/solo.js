import axios from 'axios';
import { getBaseApi } from './shared.js';

// ========================
// Solo Stats & Performance
// ========================

export async function getOverallStats(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/stats/${userId}`);
  return data;
}

export async function getPerformance(userId, limit) {
  const base = getBaseApi();
  const params = limit ? { limit } : {};
  const { data } = await axios.get(`${base}/performance/${userId}`, { params });
  return data;
}

export async function getComparison(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/comparison/${userId}`);
  return data;
}

export async function getMatchDuration(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/match-duration/${userId}`);
  return data;
}

export async function getChampionPerformance(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/champion-performance/${userId}`);
  return data;
}

export async function getChampionMatchups(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/champion-matchups/${userId}`);
  return data;
}

// ========================
// Dev Tools - Refresh Games
// ========================

export async function refreshGames(userId) {
  const base = getBaseApi();
  const { data } = await axios.post(`${base}/refresh-games/${userId}`);
  return data;
}
