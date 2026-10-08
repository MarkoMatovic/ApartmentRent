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

// Request interceptor — attaches token and proactively refreshes if expiring soon
apiClient.interceptors.request.use(
  async (config) => {
    const token = getAccessToken();
    if (!token) return config;

    // Proactively refresh if token expires within 30s (skip for refresh endpoint itself)
    if (isTokenExpiringSoon(token) && !config.url?.includes('token/refresh')) {
      try {
        const { data } = await apiClient.post<{ accessToken: string }>('/api/v1/auth/token/refresh');
        setAccessToken(data.accessToken);
        config.headers.Authorization = `Bearer ${data.accessToken}`;
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

// Queue za zahteve koji čekaju na refresh tokena
let isRefreshing = false;
let failedQueue: Array<{ resolve: (token: string) => void; reject: (err: unknown) => void }> = [];

const processQueue = (error: unknown, token: string | null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token!);
    }
  });
  failedQueue = [];
};

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

      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        }).then((token) => {
          originalRequest.headers.Authorization = `Bearer ${token}`;
          return apiClient(originalRequest);
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        // Refresh token is sent automatically via httpOnly cookie — no body needed
        const { data } = await apiClient.post<{ accessToken: string }>(
          '/api/v1/auth/token/refresh'
        );

        setAccessToken(data.accessToken);
        apiClient.defaults.headers.common.Authorization = `Bearer ${data.accessToken}`;
        processQueue(null, data.accessToken);

        originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
        return apiClient(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError, null);
        setAccessToken(null);
        sessionStorage.removeItem('user');
        window.location.href = '/login';
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

export default apiClient;
