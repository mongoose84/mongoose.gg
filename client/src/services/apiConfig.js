/**
 * Centralized environment + host configuration for client_v2 API calls
 */

const development = import.meta.env.DEV
const apiVersion = '/api/v2'
const host = development
  ? 'http://localhost:5164'
  : 'https://lol-api.agileastronaut.com'

export function getHost() {
  return host
}

export function getBaseApi() {
  return host + apiVersion
}

export const isDevelopment = development
export const apiVersionPath = apiVersion

