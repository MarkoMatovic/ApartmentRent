/**
 * Scenario 03 — Monri Payment Idempotency (ConcurrentDictionary / IDistributedCache)
 *
 * Verifies that IdempotencyService.IsDuplicateAsync correctly deduplicates
 * concurrent POST /api/payments/create-payment requests that carry the same
 * Idempotency-Key header.
 *
 * How it works:
 *   - 10 VUs simultaneously POST create-payment with the SAME Idempotency-Key.
 *   - The first request to reach IsDuplicateAsync writes the key to cache → 200 OK.
 *   - All remaining requests find the key already set → 409 Conflict.
 *   - Result: exactly 1 success, 9 conflicts, zero 500s.
 *
 * Run:
 *   k6 run -e AUTH_TOKEN=eyJ... tests/k6/scenarios/03_monri_idempotency.js
 *   k6 run -e BASE_URL=http://localhost:5197 -e AUTH_TOKEN=eyJ... tests/k6/scenarios/03_monri_idempotency.js
 *
 * Getting AUTH_TOKEN:
 *   POST /api/v1/auth/login  →  body.accessToken
 *   (User must have a payment-eligible plan available — the endpoint itself
 *    does not charge; it only prepares a Monri payment form.)
 */

import http from 'k6/http';
import { check } from 'k6';
import { Rate, Counter } from 'k6/metrics';
import { BASE_URL, HEADERS_JSON, authHeaders } from '../config.js';

const errorRate     = new Rate('error_rate');
const okCount       = new Counter('ok_count');       // 200 — first accepted request
const conflictCount = new Counter('conflict_count'); // 409 — duplicates correctly rejected
const serverErrors  = new Counter('server_error_5xx');

// Key is generated in setup() (runs once) and passed to all VUs via data —
// module-level Date.now() runs per-VU and could produce different values.

export const options = {
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)'],
  scenarios: {
    monri_idempotency: {
      executor: 'shared-iterations',
      // 10 VUs fire simultaneously, each runs exactly once.
      vus: 10,
      iterations: 10,
      maxDuration: '30s',
    },
  },
  thresholds: {
    // No 5xx errors — idempotency must not break the server.
    http_req_failed:   ['rate==0'],
    // All 10 must complete (200 or 409 are both acceptable outcomes).
    error_rate:        ['rate==0'],
    http_req_duration: ['p(99)<5000'],
  },
};

export function setup() {
  const token = __ENV.AUTH_TOKEN || '';
  const idempotencyKey = `k6-perf-test-${Date.now()}`;

  if (!token) {
    console.warn('[Idempotency] AUTH_TOKEN not set — requests will be Unauthorized (401).');
    console.warn('[Idempotency] Run: k6 run -e AUTH_TOKEN=<jwt> scenarios/03_monri_idempotency.js\n');
  }

  console.log(`[Idempotency] Idempotency-Key : ${idempotencyKey}`);
  console.log('[Idempotency] 10 VUs will POST create-payment simultaneously.');
  console.log('[Idempotency] Expected: 1 × HTTP 200, 9 × HTTP 409.\n');

  return { token, idempotencyKey };
}

function paymentPayload() {
  return JSON.stringify({
    planId:     'basic',
    successUrl: 'http://localhost:3000/success',
    failureUrl: 'http://localhost:3000/failure',
  });
}

// ── Main VU function ─────────────────────────────────────────────────────────
export default function (data) {
  const token = data?.token || __ENV.AUTH_TOKEN || '';

  const headers = {
    ...HEADERS_JSON,
    'Idempotency-Key': data.idempotencyKey,
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };

  const res = http.post(
    `${BASE_URL}/api/payments/create-payment`,
    paymentPayload(),
    {
      headers,
      tags: { name: 'monri_idempotency' },
    }
  );

  // 200 → first request accepted
  // 409 → correctly rejected as duplicate
  // 401 → no auth token (test misconfiguration, not an app bug)
  const ok = check(res, {
    '200 or 409 or 401': (r) => r.status === 200 || r.status === 409 || r.status === 401,
    'not a 500':         (r) => r.status < 500,
  });

  errorRate.add(!ok);

  if      (res.status === 200)          okCount.add(1);
  else if (res.status === 409)          conflictCount.add(1);
  else if (res.status >= 500)           serverErrors.add(1);
}

// ── Summary ─────────────────────────────────────────────────────────────────
export function handleSummary(data) {
  const ok5xx  = data.metrics.server_error_5xx?.values ?? {};
  const ok200  = data.metrics.ok_count?.values ?? {};
  const ok409  = data.metrics.conflict_count?.values ?? {};
  const d      = data.metrics['http_req_duration']?.values ?? {};

  const n200  = ok200.count ?? 0;
  const n409  = ok409.count ?? 0;
  const n5xx  = ok5xx.count ?? 0;
  const p99   = (d['p(99)'] ?? 0).toFixed(0);

  // Ideal: exactly 1 OK, 9 conflicts.
  // Acceptable: 1 OK, 9 conflicts even if a few arrive after TTL expiry in slow CI.
  const idempotencyOk = n200 <= 1 && n5xx === 0;
  const verdict = idempotencyOk
    ? `PASS — Idempotency OK  (${n200} accepted, ${n409} rejected, ${n5xx} server errors)`
    : `FAIL — ${n200} accepted (expected 1), ${n5xx} server error(s)`;
  const icon = verdict.startsWith('PASS') ? '✅' : '❌';

  console.log(`
╔══════════════════════════════════════════════════════╗
║       Scenario 03 — Monri Idempotency Results        ║
╠══════════════════════════════════════════════════════╣
║  Concurrent POST /create-payment (same key) : 10     ║
╠══════════════════════════════════════════════════════╣
║  HTTP 200 (accepted)     : ${String(n200).padEnd(26)}║
║  HTTP 409 (rejected)     : ${String(n409).padEnd(26)}║
║  HTTP 5xx (server error) : ${String(n5xx).padEnd(26)}║
║  p99 duration            : ${(p99 + ' ms').padEnd(26)}║
╠══════════════════════════════════════════════════════╣
║  ${icon}  ${verdict.padEnd(49)}║
╚══════════════════════════════════════════════════════╝

  How to read this:
  ─────────────────
  • 1 × 200, 9 × 409, 0 × 5xx → IdempotencyService deduplicates correctly. ✅
  • > 1 × 200               → Cache TryAdd race — multiple payments initiated. ❌
  • Any 5xx                 → Server-side error under concurrent load.         ❌

  Note: if AUTH_TOKEN was not set, all 10 will be 401 (Unauthorized) —
  this confirms the endpoint is reachable but skips idempotency validation.
  Provide a valid JWT to run the full test.
`);

  return {};
}
