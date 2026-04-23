/**
 * Scenario 06 — Concurrent Apartment Applications
 *
 * Verifies that the unique DB constraint on (UserId, ApartmentId) +
 * DbUpdateException handler in ApartmentApplicationService prevents
 * duplicate applications under concurrent load.
 *
 * How it works:
 *   - 10 VUs share one JWT token (same userId) and simultaneously POST
 *     /api/applications with the same apartmentId.
 *   - Without the constraint: multiple INSERTs could commit → duplicates in DB.
 *   - With the constraint: at most 1 succeeds; rest get 400 "Already applied".
 *   - ZERO 500s is the hard pass criterion.
 *
 * Run:
 *   k6 run -e AUTH_TOKEN=eyJ... -e APARTMENT_ID=1 scenarios/06_concurrent_applications.js
 *   k6 run -e BASE_URL=http://localhost:5197 -e AUTH_TOKEN=eyJ... -e APARTMENT_ID=1 ...
 *
 * Note: if the user already has an application for APARTMENT_ID from a prior
 * run, all 10 VUs will get 400 — this is also a pass (0 server errors).
 */

import http from 'k6/http';
import { check } from 'k6';
import { Counter, Rate } from 'k6/metrics';
import { BASE_URL, HEADERS_JSON } from '../config.js';

const firstSuccess    = new Counter('application_accepted');   // should be ≤ 1
const dupRejected     = new Counter('duplicate_rejected');     // 400 / 409
const serverErrors    = new Counter('server_error_5xx');
const errorRate       = new Rate('error_rate');

const CONCURRENT_VUS = 10;

export const options = {
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)'],
  scenarios: {
    concurrent_applications: {
      executor: 'shared-iterations',
      vus:        CONCURRENT_VUS,
      iterations: CONCURRENT_VUS,
      maxDuration: '30s',
    },
  },
  thresholds: {
    application_accepted: ['count<=1'],  // unique constraint must allow at most one
    server_error_5xx:     ['count==0'],  // no unhandled DB exception
    error_rate:           ['rate==0'],
  },
};

export function setup() {
  const email = __ENV.TEST_EMAIL    || 'marko.matovic.6992@gmail.com';
  const pass  = __ENV.TEST_PASSWORD || 'Marko92@';

  const loginRes = http.post(
    `${BASE_URL}/api/v1/auth/login`,
    JSON.stringify({ email, password: pass }),
    { headers: HEADERS_JSON }
  );

  if (loginRes.status !== 200) {
    console.error(`[Apps] Login failed: ${loginRes.status} — ${loginRes.body}`);
    return null;
  }

  let token;
  try {
    const body = JSON.parse(loginRes.body);
    token = body.accessToken ?? body.token ?? body.access_token ?? null;
  } catch {
    console.error('[Apps] Could not parse login response.');
    return null;
  }

  const apartmentId = parseInt(__ENV.APARTMENT_ID || '1');
  console.log(`[Apps] Logged in. Target apartmentId: ${apartmentId}`);
  console.log(`[Apps] ${CONCURRENT_VUS} VUs will apply simultaneously.`);
  console.log('[Apps] Unique DB constraint must allow at most 1 insert.\n');

  return { token, apartmentId };
}

export default function (data) {
  if (!data?.token) {
    console.error('[Apps] No auth token — skipping VU.');
    errorRate.add(1);
    return;
  }

  const headers = {
    ...HEADERS_JSON,
    Authorization: `Bearer ${data.token}`,
  };

  const res = http.post(
    `${BASE_URL}/api/applications`,
    JSON.stringify({ apartmentId: data.apartmentId, isPriority: false }),
    { headers, tags: { name: 'apply_for_apartment' } }
  );

  const ok      = res.status === 200 || res.status === 201;
  const isDupe  = res.status === 400 || res.status === 409;
  const is5xx   = res.status >= 500;

  check(res, {
    'not a server error':  (r) => r.status < 500,
    'expected status code': (r) => r.status === 200 || r.status === 201
                                || r.status === 400 || r.status === 409,
  });

  if (ok)    firstSuccess.add(1);
  if (isDupe) dupRejected.add(1);
  if (is5xx) {
    serverErrors.add(1);
    errorRate.add(1);
    console.error(`[Apps] VU${__VU} 5xx: ${res.status} — ${res.body?.slice(0, 200)}`);
  } else {
    errorRate.add(0);
  }
}

export function handleSummary(data) {
  const success  = data.metrics['application_accepted']?.values ?? {};
  const dupes    = data.metrics['duplicate_rejected']?.values ?? {};
  const srv5xx   = data.metrics['server_error_5xx']?.values ?? {};

  const nSuccess = success.count ?? 0;
  const nDupes   = dupes.count   ?? 0;
  const nErrors  = srv5xx.count  ?? 0;

  const pass    = nErrors === 0 && nSuccess <= 1;
  const verdict = pass
    ? `PASS — ${nSuccess} accepted, ${nDupes} duplicates blocked, 0 server errors`
    : `FAIL — ${nErrors} server error(s) or ${nSuccess} duplicate inserts`;
  const icon = pass ? '✅' : '❌';

  console.log(`
╔══════════════════════════════════════════════════════╗
║    Scenario 06 — Concurrent Applications Results     ║
╠══════════════════════════════════════════════════════╣
║  Concurrent VUs            : ${String(CONCURRENT_VUS).padEnd(24)}║
╠══════════════════════════════════════════════════════╣
║  Applications accepted (200/201) : ${String(nSuccess).padEnd(17)}║
║  Duplicate rejections (400/409)  : ${String(nDupes).padEnd(17)}║
║  Server errors (5xx)             : ${String(nErrors).padEnd(17)}║
╠══════════════════════════════════════════════════════╣
║  ${icon}  ${verdict.padEnd(49)}║
╚══════════════════════════════════════════════════════╝

  How to read this:
  ─────────────────
  • accepted ≤ 1 + 0 server errors → Unique constraint works.          ✅
  • accepted = 0 (all 400)         → Already applied from prior run.   ✅ (also ok)
  • server errors > 0              → Unhandled DbUpdateException.      ❌
  • accepted > 1                   → Duplicate records inserted!       ❌

  DB verification (after test):
    SELECT COUNT(*) FROM Applications.ApartmentApplications
    WHERE UserId = <your_user_id> AND ApartmentId = ${__ENV.APARTMENT_ID || 1}
    → must return exactly 1.
`);

  return {};
}
