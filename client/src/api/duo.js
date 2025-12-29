import axios from 'axios';
import { getBaseApi } from './shared.js';

// ========================
// Duo Stats
// ========================

export async function getDuoStats(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-stats/${userId}`);
  return data;
}

export async function getDuoLatestGame(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-latest-game/${userId}`);
  return data;
}

// ========================
// Duo Trend Analysis
// ========================

export async function getDuoWinRateTrend(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-win-rate-trend/${userId}`);
  return data;
}

export async function getDuoStreak(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-streak/${userId}`);
  return data;
}

export async function getDuoPerformanceRadar(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-performance-radar/${userId}`);
  return data;
}

// ========================
// Duo Kill Analysis
// ========================

export async function getDuoMultiKills(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-multi-kills/${userId}`);
  return data;
}

export async function getDuoKillParticipation(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-kill-participation/${userId}`);
  return data;
}

export async function getDuoKillsByPhase(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-kills-by-phase/${userId}`);
  return data;
}

export async function getDuoKillsTrend(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-kills-trend/${userId}`);
  return data;
}

// ========================
// Duo Death Analysis
// ========================

export async function getDuoDeathTimerImpact(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-death-timer-impact/${userId}`);
  return data;
}

export async function getDuoDeathShare(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-death-share/${userId}`);
  return data;
}

export async function getDuoDeathsByDuration(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-deaths-by-duration/${userId}`);
  return data;
}

export async function getDuoDeathsTrend(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-deaths-trend/${userId}`);
  return data;
}

// ========================
// Duo Champion & Match Analysis
// ========================

export async function getDuoMatchDuration(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-match-duration/${userId}`);
  return data;
}

export async function getDuoVsEnemy(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-vs-enemy/${userId}`);
  return data;
}

export async function getDuoImprovementSummary(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/duo-improvement-summary/${userId}`);
  return data;
}

