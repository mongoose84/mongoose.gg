// Centralized environment + host configuration for client API calls

// Edit these values in one place
const development = false; // Set to false for production
const apiVersion = '/api/v1.0';
const host = development
	? 'http://localhost:5000'
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
