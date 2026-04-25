/**
 * Centralized environment + host configuration for client_v2 API calls
 */

const development = import.meta.env.DEV
const apiVersion = '/api/v2'
// In development, use a relative path so requests go through the Vite proxy
// (configured in vite.config.js → server.proxy). In production, use the full origin.
const host = development
  ? ''
  : 'https://api.mongoose.gg'

export function getHost() {
  return host
}

export function getBaseApi() {
  return host + apiVersion
}

export const isDevelopment = development
export const apiVersionPath = apiVersion

