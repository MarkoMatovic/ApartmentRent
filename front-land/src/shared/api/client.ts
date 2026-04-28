import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL;
if (!API_BASE_URL) {
  if (import.meta.env.PROD) {
    throw new Error('VITE_API_URL is not set. This environment variable is required in production.');
  }
  console.warn('[client] VITE_API_URL is not set — falling back to https://localhost:7092 (dev only)');
}
const _baseUrl = API_BASE_URL ?? 'https://localhost:7092';

/**
 * The resolved API base URL.  Always use this instead of re-reading
 * `import.meta.env.VITE_API_URL` so the dev-fallback and production guard
 * only live in one place.
 */
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
    const token = sessionStorage.getItem('authToken');
    if (!token) return config;

    // Proactively refresh if token expires within 30s (skip for refresh endpoint itself)
    if (isTokenExpiringSoon(token) && !config.url?.includes('token/refresh')) {
      try {
        const { data } = await apiClient.post<{ accessToken: string }>('/api/v1/auth/token/refresh');
        sessionStorage.setItem('authToken', data.accessToken);
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
        sessionStorage.removeItem('authToken');
        sessionStorage.removeItem('refreshToken');
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

        sessionStorage.setItem('authToken', data.accessToken);
        // Note: new refresh token is set by server as httpOnly cookie automatically

        apiClient.defaults.headers.common.Authorization = `Bearer ${data.accessToken}`;
        processQueue(null, data.accessToken);

        originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
        return apiClient(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError, null);
        sessionStorage.removeItem('authToken');
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
