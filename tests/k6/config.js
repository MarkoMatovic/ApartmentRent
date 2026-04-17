// Shared config — override BASE_URL via: k6 run -e BASE_URL=http://... script.js
export const BASE_URL = __ENV.BASE_URL || 'http://localhost:5197';

export const HEADERS_JSON = {
  'Content-Type': 'application/json',
  'Accept': 'application/json',
};

// Bearer token for endpoints that require auth.
// Set via: k6 run -e AUTH_TOKEN=eyJ... script.js
export function authHeaders() {
  const token = __ENV.AUTH_TOKEN || '';
  return {
    ...HEADERS_JSON,
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };
}

// Shared thresholds used across all scenarios
export const THRESHOLDS_BASE = {
  http_req_failed:   ['rate<0.01'],        // < 1% errors
  http_req_duration: ['p(95)<2000', 'p(99)<4000'],
};
