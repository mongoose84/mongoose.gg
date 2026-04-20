export const featureFlags = {
  teamAnalytics: import.meta.env.VITE_FEATURE_TEAM_ANALYTICS === 'true',
  goals: import.meta.env.VITE_FEATURE_GOALS === 'true',
}
