import axios from 'axios';
import { getBaseApi } from './shared.js';

// ========================
// Team Stats
// ========================

export async function getTeamStats(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-stats/${userId}`);
  return data;
}

export async function getTeamLatestGame(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-latest-game/${userId}`);
  return data;
}

// ========================
// Team Trend Analysis
// ========================

export async function getTeamWinRateTrend(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-win-rate-trend/${userId}`);
  return data;
}

export async function getTeamDurationAnalysis(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-duration-analysis/${userId}`);
  return data;
}

// ========================
// Team Kill Analysis
// ========================

export async function getTeamMultiKills(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-multi-kills/${userId}`);
  return data;
}

export async function getTeamKillParticipation(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-kill-participation/${userId}`);
  return data;
}

export async function getTeamKillsByPhase(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-kills-by-phase/${userId}`);
  return data;
}

export async function getTeamKillsTrend(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-kills-trend/${userId}`);
  return data;
}

// ========================
// Team Death Analysis
// ========================

export async function getTeamDeathTimerImpact(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-death-timer-impact/${userId}`);
  return data;
}

export async function getTeamDeathShare(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-death-share/${userId}`);
  return data;
}

export async function getTeamDeathsByDuration(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-deaths-by-duration/${userId}`);
  return data;
}

export async function getTeamDeathsTrend(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-deaths-trend/${userId}`);
  return data;
}

// ========================
// Team Champion & Synergy Analysis
// ========================

export async function getTeamSynergy(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-synergy/${userId}`);
  return data;
}

export async function getTeamChampionCombos(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-champion-combos/${userId}`);
  return data;
}

export async function getTeamRolePairEffectiveness(userId) {
  const base = getBaseApi();
  const { data } = await axios.get(`${base}/team-role-pair-effectiveness/${userId}`);
  return data;
}

