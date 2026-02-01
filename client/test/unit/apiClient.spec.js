import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

// Mock apiConfig before importing apiClient
vi.mock('@/services/apiConfig', () => ({
  getBaseApi: () => 'http://localhost:5000/api/v2'
}));

// Store original fetch
const originalFetch = global.fetch;

describe('apiClient', () => {
  let apiClient;
  let mockFetch;

  beforeEach(async () => {
    // Clear module cache to reset the onSessionExpired callback
    vi.resetModules();
    
    // Create mock fetch
    mockFetch = vi.fn();
    global.fetch = mockFetch;
    
    // Import fresh module
    apiClient = await import('@/services/apiClient');
  });

  afterEach(() => {
    global.fetch = originalFetch;
    vi.clearAllMocks();
  });

  describe('AUTH_ERROR_CODES', () => {
    it('exports correct error codes', () => {
      expect(apiClient.AUTH_ERROR_CODES).toEqual({
        SESSION_EXPIRED: 'SESSION_EXPIRED',
        NOT_AUTHENTICATED: 'NOT_AUTHENTICATED',
        FORBIDDEN: 'FORBIDDEN',
        INVALID_CREDENTIALS: 'INVALID_CREDENTIALS',
        ACCOUNT_DEACTIVATED: 'ACCOUNT_DEACTIVATED'
      });
    });
  });

  describe('isSessionExpiredError', () => {
    it('returns true for SESSION_EXPIRED code', () => {
      expect(apiClient.isSessionExpiredError('SESSION_EXPIRED')).toBe(true);
    });

    it('returns false for other codes', () => {
      expect(apiClient.isSessionExpiredError('NOT_AUTHENTICATED')).toBe(false);
      expect(apiClient.isSessionExpiredError('FORBIDDEN')).toBe(false);
      expect(apiClient.isSessionExpiredError(null)).toBe(false);
    });
  });

  describe('isNotAuthenticatedError', () => {
    it('returns true for NOT_AUTHENTICATED code', () => {
      expect(apiClient.isNotAuthenticatedError('NOT_AUTHENTICATED')).toBe(true);
    });

    it('returns false for other codes', () => {
      expect(apiClient.isNotAuthenticatedError('SESSION_EXPIRED')).toBe(false);
      expect(apiClient.isNotAuthenticatedError('FORBIDDEN')).toBe(false);
      expect(apiClient.isNotAuthenticatedError(null)).toBe(false);
    });
  });

  describe('setSessionExpiredCallback', () => {
    it('registers callback that is called on session expiry', async () => {
      const callback = vi.fn();
      apiClient.setSessionExpiredCallback(callback);

      mockFetch.mockResolvedValue({
        status: 401,
        ok: false,
        clone: () => ({
          json: () => Promise.resolve({ code: 'SESSION_EXPIRED', error: 'Session expired' })
        })
      });

      await apiClient.apiRequest('/test');

      expect(callback).toHaveBeenCalledWith({ code: 'SESSION_EXPIRED', error: 'Session expired' });
    });
  });

  describe('apiRequest', () => {
    it('makes request with correct defaults', async () => {
      mockFetch.mockResolvedValue({ status: 200, ok: true });

      await apiClient.apiRequest('/users');

      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:5000/api/v2/users',
        expect.objectContaining({
          credentials: 'include',
          headers: expect.objectContaining({
            'Content-Type': 'application/json'
          })
        })
      );
    });

    it('uses full URL when endpoint starts with http', async () => {
      mockFetch.mockResolvedValue({ status: 200, ok: true });

      await apiClient.apiRequest('https://external.api/test');

      expect(mockFetch).toHaveBeenCalledWith(
        'https://external.api/test',
        expect.anything()
      );
    });

    it('triggers callback for 401 with SESSION_EXPIRED code', async () => {
      const callback = vi.fn();
      apiClient.setSessionExpiredCallback(callback);

      mockFetch.mockResolvedValue({
        status: 401,
        ok: false,
        clone: () => ({
          json: () => Promise.resolve({ code: 'SESSION_EXPIRED' })
        })
      });

      await apiClient.apiRequest('/test');

      expect(callback).toHaveBeenCalledWith({ code: 'SESSION_EXPIRED' });
    });

    it('triggers callback for 401 with NOT_AUTHENTICATED code', async () => {
      const callback = vi.fn();
      apiClient.setSessionExpiredCallback(callback);

      mockFetch.mockResolvedValue({
        status: 401,
        ok: false,
        clone: () => ({
          json: () => Promise.resolve({ code: 'NOT_AUTHENTICATED' })
        })
      });

      await apiClient.apiRequest('/test');

      expect(callback).toHaveBeenCalledWith({ code: 'NOT_AUTHENTICATED' });
    });

    it('does NOT trigger callback when skipSessionCheck is true', async () => {
      const callback = vi.fn();
      apiClient.setSessionExpiredCallback(callback);

      mockFetch.mockResolvedValue({
        status: 401,
        ok: false,
        clone: () => ({
          json: () => Promise.resolve({ code: 'SESSION_EXPIRED' })
        })
      });

      await apiClient.apiRequest('/test', {}, { skipSessionCheck: true });

      expect(callback).not.toHaveBeenCalled();
    });

    it('does NOT trigger callback for 401 without proper error codes', async () => {
      const callback = vi.fn();
      apiClient.setSessionExpiredCallback(callback);

      mockFetch.mockResolvedValue({
        status: 401,
        ok: false,
        clone: () => ({
          json: () => Promise.resolve({ code: 'INVALID_CREDENTIALS' })
        })
      });

      await apiClient.apiRequest('/test');

      expect(callback).not.toHaveBeenCalled();
    });

    it('handles non-JSON 401 response without throwing', async () => {
      const callback = vi.fn();
      apiClient.setSessionExpiredCallback(callback);

      mockFetch.mockResolvedValue({
        status: 401,
        ok: false,
        clone: () => ({
          json: () => Promise.reject(new Error('Not JSON'))
        })
      });

      const response = await apiClient.apiRequest('/test');

      expect(response.status).toBe(401);
      expect(callback).not.toHaveBeenCalled();
    });

    it('does NOT trigger callback if no callback is registered', async () => {
      // No callback registered - should not throw
      mockFetch.mockResolvedValue({
        status: 401,
        ok: false,
        clone: () => ({
          json: () => Promise.resolve({ code: 'SESSION_EXPIRED' })
        })
      });

      const response = await apiClient.apiRequest('/test');

      expect(response.status).toBe(401);
    });
  });

  describe('get', () => {
    it('makes GET request', async () => {
      mockFetch.mockResolvedValue({ status: 200, ok: true });

      await apiClient.get('/users');

      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:5000/api/v2/users',
        expect.objectContaining({ method: 'GET' })
      );
    });

    it('passes config to apiRequest', async () => {
      const callback = vi.fn();
      apiClient.setSessionExpiredCallback(callback);

      mockFetch.mockResolvedValue({
        status: 401,
        ok: false,
        clone: () => ({
          json: () => Promise.resolve({ code: 'SESSION_EXPIRED' })
        })
      });

      await apiClient.get('/test', { skipSessionCheck: true });

      expect(callback).not.toHaveBeenCalled();
    });
  });

  describe('post', () => {
    it('makes POST request with JSON body', async () => {
      mockFetch.mockResolvedValue({ status: 200, ok: true });

      await apiClient.post('/users', { username: 'test' });

      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:5000/api/v2/users',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({ username: 'test' })
        })
      );
    });

    it('handles undefined body', async () => {
      mockFetch.mockResolvedValue({ status: 200, ok: true });

      await apiClient.post('/users', undefined);

      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:5000/api/v2/users',
        expect.objectContaining({
          method: 'POST',
          body: undefined
        })
      );
    });
  });

  describe('del', () => {
    it('makes DELETE request', async () => {
      mockFetch.mockResolvedValue({ status: 200, ok: true });

      await apiClient.del('/users/1');

      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:5000/api/v2/users/1',
        expect.objectContaining({ method: 'DELETE' })
      );
    });
  });

  describe('parseResponse', () => {
    it('returns parsed JSON for successful response', async () => {
      const mockResponse = {
        ok: true,
        json: () => Promise.resolve({ data: 'test' })
      };

      const result = await apiClient.parseResponse(mockResponse);

      expect(result).toEqual({ data: 'test' });
    });

    it('throws error for non-ok response with error message', async () => {
      const mockResponse = {
        ok: false,
        status: 400,
        json: () => Promise.resolve({ error: 'Bad request', code: 'BAD_REQUEST' })
      };

      await expect(apiClient.parseResponse(mockResponse))
        .rejects.toMatchObject({
          message: 'Bad request',
          status: 400,
          code: 'BAD_REQUEST'
        });
    });

    it('uses default error message when no error in response', async () => {
      const mockResponse = {
        ok: false,
        status: 500,
        json: () => Promise.resolve({})
      };

      await expect(apiClient.parseResponse(mockResponse, 'Server error'))
        .rejects.toMatchObject({
          message: 'Server error',
          status: 500
        });
    });

    it('handles non-JSON error response', async () => {
      const mockResponse = {
        ok: false,
        status: 500,
        json: () => Promise.reject(new Error('Not JSON'))
      };

      const error = await apiClient.parseResponse(mockResponse, 'Failed').catch(e => e);

      expect(error.message).toBe('Failed');
      expect(error.status).toBe(500);
      expect(error.parseError).toBe(true);
    });
  });
});

