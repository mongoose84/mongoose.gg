// Centralized environment + host configuration for client API calls

// Automatically detect development mode using Vite's environment variables
const development = import.meta.env.DEV; // true when running 'npm run dev', false in production build
const apiVersion = '/api/v1.0';
const host = development
	? 'http://localhost:5164'
	: 'https://lol-api.agileastronaut.com';

// Returns the base API host (no version)
export function getHost() {
	return host;
}

// Returns host + version, e.g., http://localhost:5000/api/v1.0
export function getBaseApi() {
	return host + apiVersion;
}

// Expose raw values if needed
export const isDevelopment = development;
export const apiVersionPath = apiVersion;

export default {
	isDevelopment,
	apiVersionPath,
	getHost,
	getBaseApi,
};
