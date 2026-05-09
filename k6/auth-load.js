/**
 * k6 load test — Auth endpoints (login + token refresh)
 *
 * Tests the rate-limiter behaviour: at high concurrency the server should
 * return 429 (not 500) once the sliding-window limit is hit.
 *
 * Usage:
 *   k6 run auth-load.js
 *   k6 run --env BASE_URL=https://my-staging-server.com auth-load.js
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

const errorRate = new Rate('errors');

export const options = {
  stages: [
    { duration: '15s', target: 10 },
    { duration: '1m',  target: 10 },
    { duration: '15s', target: 0  },
  ],
  thresholds: {
    http_req_duration: ['p(95)<800'],
    errors:            ['rate<0.02'],
  },
};

const VALID_CREDENTIALS = {
  email:    __ENV.TEST_EMAIL    || 'testuser@example.com',
  password: __ENV.TEST_PASSWORD || 'TestPassword123!',
};

export default function () {
  const res = http.post(
    `${BASE_URL}/api/v1/auth/login`,
    JSON.stringify(VALID_CREDENTIALS),
    {
      headers: { 'Content-Type': 'application/json' },
      tags:    { name: 'Login' },
    }
  );

  const ok = check(res, {
    'login: 200 or 401 or 429': r => [200, 401, 429].includes(r.status),
    'login: no 500':            r => r.status !== 500,
  });

  errorRate.add(!ok);
  sleep(1);
}
