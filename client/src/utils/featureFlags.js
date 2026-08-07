export const featureFlags = {
  teamAnalytics: import.meta.env.VITE_FEATURE_TEAM_ANALYTICS === 'true',
  goals: import.meta.env.VITE_FEATURE_GOALS === 'true',
  riotSignOn: import.meta.env.VITE_FEATURE_RIOT_SIGNON === 'true',
  googleSignOn: import.meta.env.VITE_FEATURE_GOOGLE_SIGNON === 'true',
}
