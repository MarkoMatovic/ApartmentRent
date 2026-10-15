import axios from 'axios';
import { getAccessToken, setAccessToken } from './tokenStore';

// VITE_API_URL is optional. When the SPA is served by the API itself (Azure App Service, single
// origin) it is left empty and every request uses a relative URL; set it only if the API lives
// on a different origin.
const API_BASE_URL = import.meta.env.VITE_API_URL;
// In dev (no VITE_API_URL) use a RELATIVE base URL so every request flows through the Vite
// proxy (see vite.config.ts: /api, /uploads, /notificationHub, /chatHub → https://localhost:7092).
// This keeps the browser same-origin with the API (http://localhost:5173), so the SameSite=Strict
// httpOnly refresh cookie is actually sent to /token/refresh. Talking to https://localhost:7092
// directly is cross-scheme (http→https), which browsers treat as cross-site and drop the cookie —
// that made a page reload fail the silent refresh and log the user out.
const _baseUrl = API_BASE_URL ?? '';

export const apiBaseUrl = _baseUrl;

export const apiClient = axios.create({
  baseURL: _baseUrl,
  headers: {
    'Content-Type': 'application/json',
  },
  // Required so the browser sends httpOnly cookies (refresh token) automatically
  withCredentials: true,
});

/** Returns true if the JWT token expires within the next `thresholdSeconds` seconds. */
function isTokenExpiringSoon(token: string, thresholdSeconds = 30): boolean {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    const exp: number = payload.exp;
    if (!exp) return false;
    return exp - Date.now() / 1000 < thresholdSeconds;
  } catch {
    return false;
  }
}

let refreshInFlight: Promise<string> | null = null;

/**
 * Gets a new access token with the refresh cookie. One refresh at a time per tab: every caller that
 * asks while one is running shares it. The server exchanges the cookie on every refresh, so a
 * second parallel refresh would present a cookie that was just replaced.
 */
export const refreshAccessToken = (): Promise<string> => {
  if (!refreshInFlight) {
    refreshInFlight = apiClient
      .post<{ accessToken: string }>('/api/v1/auth/token/refresh')
      .then(({ data }) => {
        setAccessToken(data.accessToken);
        return data.accessToken;
      })
      .finally(() => {
        refreshInFlight = null;
      });
  }
  return refreshInFlight;
};

// Request interceptor — attaches token and proactively refreshes if expiring soon
apiClient.interceptors.request.use(
  async (config) => {
    const token = getAccessToken();
    if (!token) return config;

    // Proactively refresh if token expires within 30s (skip for refresh endpoint itself)
    if (isTokenExpiringSoon(token) && !config.url?.includes('token/refresh')) {
      try {
        const fresh = await refreshAccessToken();
        config.headers.Authorization = `Bearer ${fresh}`;
        return config;
      } catch {
        // Proactive refresh failed — let the request proceed with the old token;
        // the 401 response interceptor will handle the fallback.
      }
    }

    config.headers.Authorization = `Bearer ${token}`;
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor — automatski refresh access tokena na 401
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401) {
      const errorData = error.response?.data;
      const errorMessage = typeof errorData === 'string' ? errorData : errorData?.message || '';

      // Ako je poruka od kontrolera (invalid credentials), ne refreshuj — samo proslijedi grešku
      if (errorMessage) {
        return Promise.reject(error);
      }

      // Spriječi beskonačnu petlju na samom refresh endpointu
      if (originalRequest._retry || originalRequest.url?.includes('token/refresh')) {
        setAccessToken(null);
        sessionStorage.removeItem('user');
        window.location.href = '/login';
        return Promise.reject(error);
      }

      originalRequest._retry = true;

      try {
        // Requests that fail together wait for the same refresh (the cookie goes automatically).
        const fresh = await refreshAccessToken();
        originalRequest.headers.Authorization = `Bearer ${fresh}`;
        return apiClient(originalRequest);
      } catch (refreshError) {
        setAccessToken(null);
        sessionStorage.removeItem('user');
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);

export default apiClient;
