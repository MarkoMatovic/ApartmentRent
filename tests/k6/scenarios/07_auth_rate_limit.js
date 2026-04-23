/**
 * Scenario 07 — Auth Rate Limiter Probe
 *
 * Verifies that the "auth" rate-limiting policy (5 req / 30 s per IP) kicks
 * in on POST /api/v1/auth/login and returns 429 instead of crashing.
 *
 * Rate-limit config (RateLimitingServiceExtensions.cs):
 *   policy "auth": limit=5, window=30s, queue=0 — immediate rejection
 *
 * How it works:
 *   15 VUs simultaneously hit /login with clearly-invalid credentials.
 *   The first ≤5 requests that reach the server return 401 (wrong password).
 *   The remaining ≥10 must be rejected with 429 (Too Many Requests).
 *   The rate limiter runs BEFORE authentication so credentials are irrelevant.
 *
 * Run:
 *   k6 run scenarios/07_auth_rate_limit.js
 *   k6 run -e BASE_URL=http://localhost:5197 scenarios/07_auth_rate_limit.js
 */

import http from 'k6/http';
import { check } from 'k6';
import { Counter, Rate } from 'k6/metrics';
import { BASE_URL, HEADERS_JSON } from '../config.js';

const rateLimited  = new Counter('rate_limited_429');
const passedToAuth = new Counter('passed_to_auth');   // 200 or 401
const serverErrors = new Counter('server_error_5xx');
const errorRate    = new Rate('error_rate');

const TOTAL_REQUESTS = 15;
const RATE_LIMIT_CAP = 5;  // configured limit

export const options = {
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)'],
  scenarios: {
    auth_rate_limit: {
      executor: 'shared-iterations',
      vus:        TOTAL_REQUESTS,
      iterations: TOTAL_REQUESTS,
      maxDuration: '30s',
    },
  },
  thresholds: {
    // At least (TOTAL - CAP) requests must be rate-limited
    rate_limited_429: [`count>=${TOTAL_REQUESTS - RATE_LIMIT_CAP}`],
    server_error_5xx: ['count==0'],
    error_rate:       ['rate==0'],
  },
};

export default function () {
  // Clearly-invalid credentials — we only care about the rate limiter, not auth
  const body = JSON.stringify({
    email:    'k6-rate-limit-probe@nonexistent.invalid',
    password: 'Wr0ngPassword!',
  });

  const res = http.post(
    `${BASE_URL}/api/v1/auth/login`,
    body,
    { headers: HEADERS_JSON, tags: { name: 'auth_login_probe' } }
  );

  const is429 = res.status === 429;
  const is5xx = res.status >= 500;
  const passedAuth = res.status === 200 || res.status === 401;

  check(res, {
    'not a server error': (r) => r.status < 500,
    'expected status':    (r) => r.status === 200 || r.status === 401 || r.status === 429,
  });

  if (is429)      rateLimited.add(1);
  if (passedAuth) passedToAuth.add(1);

  if (is5xx) {
    serverErrors.add(1);
    errorRate.add(1);
    console.error(`[RateLimit] VU${__VU} unexpected 5xx: ${res.status} — ${res.body?.slice(0, 200)}`);
  } else {
    errorRate.add(0);
  }
}

export function handleSummary(data) {
  const limited   = data.metrics['rate_limited_429']?.values ?? {};
  const passed    = data.metrics['passed_to_auth']?.values   ?? {};
  const srv5xx    = data.metrics['server_error_5xx']?.values ?? {};

  const nLimited  = limited.count ?? 0;
  const nPassed   = passed.count  ?? 0;
  const nErrors   = srv5xx.count  ?? 0;

  const expectedBlocked = TOTAL_REQUESTS - RATE_LIMIT_CAP;
  const pass = nLimited >= expectedBlocked && nErrors === 0;
  const verdict = pass
    ? `PASS — ${nLimited}/${TOTAL_REQUESTS} rate-limited (≥${expectedBlocked} expected)`
    : nErrors > 0
      ? `FAIL — ${nErrors} server error(s) from rate limiter`
      : `FAIL — only ${nLimited} rate-limited, expected ≥${expectedBlocked}`;
  const icon = pass ? '✅' : '❌';

  console.log(`
╔══════════════════════════════════════════════════════╗
║      Scenario 07 — Auth Rate Limiter Results         ║
╠══════════════════════════════════════════════════════╣
║  Total requests fired  : ${String(TOTAL_REQUESTS).padEnd(28)}║
║  Rate-limit cap        : ${String(RATE_LIMIT_CAP).padEnd(28)}req / 30 s║
╠══════════════════════════════════════════════════════╣
║  Passed to auth (200/401) : ${String(nPassed).padEnd(25)}║
║  Rate-limited (429)       : ${String(nLimited).padEnd(25)}║
║  Server errors (5xx)      : ${String(nErrors).padEnd(25)}║
╠══════════════════════════════════════════════════════╣
║  ${icon}  ${verdict.padEnd(49)}║
╚══════════════════════════════════════════════════════╝

  How to read this:
  ─────────────────
  • Rate limit policy: 5 req / 30 s per IP (queue=0, reject immediately)
  • ≥10 rate-limited + 0 server errors → Limiter works correctly.       ✅
  • 0 rate-limited                     → Limiter not active or wrong policy.  ❌
  • Server errors > 0                  → Limiter rejected with 5xx (bug). ❌

  Note: "passed to auth" returns 401 for invalid credentials — expected.
  The rate limiter fires BEFORE auth, so 401 means the request got through.
`);

  return {};
}
