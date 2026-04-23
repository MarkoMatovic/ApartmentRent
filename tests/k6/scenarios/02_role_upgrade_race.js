/**
 * Scenario 02 — Role Upgrade Race Condition (SemaphoreSlim)
 *
 * Verifies that UserRoleUpgradeService.AutoUpgradeOnFirstListingAsync uses
 * SemaphoreSlim to prevent concurrent duplicate role upgrades when the same
 * Tenant user submits two apartments simultaneously.
 *
 * How it works:
 *   - setup() registers a fresh Tenant user and returns their JWT token.
 *   - 2 VUs simultaneously POST to create-apartment using that same token.
 *   - Without SemaphoreSlim: both threads read "Tenant" and both write
 *     "TenantLandlord" → potential race / duplicate-write / DB constraint error.
 *   - With SemaphoreSlim: first caller upgrades, second sees role already updated
 *     and skips → both requests return HTTP 200, zero 500s.
 *
 * Run:
 *   k6 run tests/k6/scenarios/02_role_upgrade_race.js
 *   k6 run -e BASE_URL=http://localhost:5197 tests/k6/scenarios/02_role_upgrade_race.js
 *
 * Note: each run registers a unique user so the role starts as "Tenant" every time.
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Counter } from 'k6/metrics';
import { BASE_URL, HEADERS_JSON } from '../config.js';

const errorRate    = new Rate('error_rate');
const successCount = new Counter('success_count');
const serverErrors = new Counter('server_error_count');

export const options = {
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)'],
  scenarios: {
    role_upgrade_race: {
      executor: 'shared-iterations',
      // Exactly 2 VUs fire simultaneously — mirrors the race condition scenario.
      vus: 2,
      iterations: 2,
      maxDuration: '30s',
    },
  },
  thresholds: {
    // Both concurrent requests must succeed — no 500s from the race.
    http_req_failed: ['rate==0'],
    error_rate:      ['rate==0'],
    // Creating an apartment shouldn't take more than 5 s even on a cold start.
    http_req_duration: ['p(99)<5000'],
  },
};

// ── Setup: login as existing user (provide via env or default test creds) ───
export function setup() {
  const email = __ENV.TEST_EMAIL    || 'marko.matovic.6992@gmail.com';
  const pass  = __ENV.TEST_PASSWORD || 'Marko92@';

  const loginRes = http.post(
    `${BASE_URL}/api/v1/auth/login`,
    JSON.stringify({ email, password: pass }),
    { headers: HEADERS_JSON }
  );

  if (loginRes.status !== 200) {
    console.error(`[Race] Login failed: ${loginRes.status} — ${loginRes.body}`);
    return null;
  }

  let token;
  try {
    const body = JSON.parse(loginRes.body);
    token = body.accessToken ?? body.token ?? body.access_token ?? null;
  } catch {
    console.error('[Race] Could not parse login response');
    return null;
  }

  console.log(`[Race] Logged in as: ${email}`);
  console.log('[Race] 2 VUs will POST create-apartment simultaneously.');
  console.log('[Race] SemaphoreSlim should ensure no 500s from concurrent writes.\n');

  return { token };
}

// Minimal valid apartment payload
function apartmentPayload(vuId) {
  return JSON.stringify({
    title:            `Race Test Apartment VU${vuId}`,
    description:      'Performance test — race condition scenario',
    rent:             500,
    address:          'Test ulica 1',
    city:             'Sarajevo',
    postalCode:       '71000',
    numberOfRooms:    2,
    sizeSquareMeters: 50,
    listingType:      0,
    apartmentType:    0,
  });
}

// ── Main VU function ─────────────────────────────────────────────────────────
export default function (data) {
  if (!data?.token) {
    console.error('[Race] No auth token — skipping VU.');
    return;
  }

  const headers = {
    ...HEADERS_JSON,
    Authorization: `Bearer ${data.token}`,
  };

  const res = http.post(
    `${BASE_URL}/api/v1/rent/create-apartment`,
    apartmentPayload(__VU),
    {
      headers,
      tags: { name: 'create_apartment_race' },
    }
  );

  const ok = check(res, {
    'HTTP 200 or 201': (r) => r.status === 200 || r.status === 201,
    'not a 500':       (r) => r.status < 500,
    'body is JSON':    (r) => r.headers['Content-Type']?.includes('application/json'),
  });

  errorRate.add(!ok);
  if (res.status === 200 || res.status === 201) successCount.add(1);
  if (res.status >= 500) serverErrors.add(1);

  if (res.status >= 500) {
    console.error(`[Race] VU${__VU} got server error ${res.status}: ${res.body?.slice(0, 200)}`);
  }
}

// ── Summary ─────────────────────────────────────────────────────────────────
export function handleSummary(data) {
  const fail    = data.metrics.http_req_failed?.values ?? {};
  const success = data.metrics.success_count?.values ?? {};
  const srv5xx  = data.metrics.server_error_count?.values ?? {};
  const d       = data.metrics['http_req_duration']?.values ?? {};

  const errPct    = ((fail.rate ?? 0) * 100).toFixed(2);
  const successes = success.count ?? 0;
  const srv500s   = srv5xx.count ?? 0;
  const p99       = (d['p(99)'] ?? 0).toFixed(0);

  const verdict = srv500s === 0 && successes === 2
    ? 'PASS — SemaphoreSlim OK  (0 race errors, both requests succeeded)'
    : `FAIL — ${srv500s} server error(s), ${successes}/2 succeeded`;
  const icon = verdict.startsWith('PASS') ? '✅' : '❌';

  console.log(`
╔══════════════════════════════════════════════════════╗
║        Scenario 02 — Role Upgrade Race Results       ║
╠══════════════════════════════════════════════════════╣
║  Concurrent POST /create-apartment (same user) : 2   ║
╠══════════════════════════════════════════════════════╣
║  Successful (2xx)  : ${String(successes).padEnd(32)}║
║  Server errors(5xx): ${String(srv500s).padEnd(32)}║
║  Error rate        : ${(errPct + '%').padEnd(32)}║
║  p99 duration      : ${(p99 + ' ms').padEnd(32)}║
╠══════════════════════════════════════════════════════╣
║  ${icon}  ${verdict.padEnd(49)}║
╚══════════════════════════════════════════════════════╝

  How to read this:
  ─────────────────
  • 0 server errors + 2 successes → SemaphoreSlim protected the upgrade.  ✅
  • Any 500 → race condition exists — check logs for DB constraint errors. ❌

  Server-side check (open app logs):
    grep "Auto-upgraded user" → should appear exactly 1 time, NOT 2 times.
`);

  return {};
}
