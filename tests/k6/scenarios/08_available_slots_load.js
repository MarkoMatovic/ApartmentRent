/**
 * Scenario 08 — Available Slots Endpoint Load Test
 *
 * GET /api/appointments/available-slots/{apartmentId}?date=YYYY-MM-DD
 *
 * This endpoint computes available time slots on-the-fly:
 *   1. Loads landlord availability windows from DB
 *   2. Loads existing appointments for that date from DB
 *   3. Iterates windows, subtracts booked slots, returns free slots
 *
 * There is NO OutputCache or HybridCache on this endpoint.
 * Under 100 concurrent VUs every request hits the DB → potential bottleneck.
 *
 * What we're looking for:
 *   - p95 < 2 s  → acceptable without caching
 *   - p95 2–5 s  → add [OutputCache] or cache in service layer
 *   - p95 > 5 s  → critical — DB connection pool or query problem
 *
 * Run:
 *   k6 run -e AUTH_TOKEN=eyJ... -e APARTMENT_ID=1 scenarios/08_available_slots_load.js
 *   k6 run -e BASE_URL=http://localhost:5197 -e AUTH_TOKEN=eyJ... -e APARTMENT_ID=1 ...
 */

import http   from 'k6/http';
import { check } from 'k6';
import { Trend, Rate } from 'k6/metrics';
import { BASE_URL, HEADERS_JSON } from '../config.js';

const slotsDuration = new Trend('slots_req_ms', true);
const errorRate     = new Rate('error_rate');

export const options = {
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)'],
  scenarios: {
    slots_load: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '15s', target: 50  },   // warm-up
        { duration: '30s', target: 100 },   // peak load
        { duration: '15s', target: 0   },   // cooldown
      ],
    },
  },
  thresholds: {
    slots_req_ms: ['p(95)<2000', 'p(99)<5000'],
    error_rate:   ['rate<0.05'],
  },
};

export function setup() {
  const token       = __ENV.AUTH_TOKEN  || '';
  const apartmentId = __ENV.APARTMENT_ID || '1';

  if (!token) {
    console.warn('[Slots] AUTH_TOKEN not set — requests will fail with 401.');
    console.warn('[Slots] Run: k6 run -e AUTH_TOKEN=<jwt> -e APARTMENT_ID=<id> scenarios/08_available_slots_load.js\n');
  }

  // Pick a date ~2 months out — very likely to have zero existing appointments
  // which means the slot calculation exercises the full availability window iteration
  const d = new Date();
  d.setMonth(d.getMonth() + 2);
  const dateStr = d.toISOString().split('T')[0];

  console.log(`[Slots] GET /api/appointments/available-slots/${apartmentId}?date=${dateStr}`);
  console.log('[Slots] Ramping to 100 VUs. No cache on this endpoint.\n');

  return { token, apartmentId, dateStr };
}

export default function (data) {
  const headers = {
    ...HEADERS_JSON,
    ...(data.token ? { Authorization: `Bearer ${data.token}` } : {}),
  };

  const url = `${BASE_URL}/api/appointments/available-slots/${data.apartmentId}?date=${data.dateStr}`;
  const res = http.get(url, { headers, tags: { name: 'available_slots' } });

  const ok = check(res, {
    'HTTP 200': (r) => r.status === 200,
    'body is JSON': (r) => r.headers['Content-Type']?.includes('application/json'),
  });

  slotsDuration.add(res.timings.duration);
  errorRate.add(!ok);

  if (res.status >= 500) {
    console.error(`[Slots] VU${__VU} 5xx: ${res.status} — ${res.body?.slice(0, 200)}`);
  }
}

export function handleSummary(data) {
  const dur    = data.metrics['slots_req_ms']?.values ?? {};
  const err    = data.metrics['error_rate']?.values   ?? {};

  const p50    = (dur.med      ?? 0).toFixed(0);
  const p95    = (dur['p(95)'] ?? 0).toFixed(0);
  const p99    = (dur['p(99)'] ?? 0).toFixed(0);
  const maxVal = (dur.max      ?? 0).toFixed(0);
  const errPct = ((err.rate    ?? 0) * 100).toFixed(1);

  const pass = (dur['p(95)'] ?? 9999) < 2000 && (err.rate ?? 1) < 0.05;
  const icon = pass ? '✅' : (dur['p(95)'] ?? 9999) < 5000 ? '⚠️ ' : '❌';
  const verdict = pass
    ? `PASS — p95=${p95} ms, errors=${errPct}%`
    : `WARN — p95=${p95} ms or error rate ${errPct}% above threshold`;

  console.log(`
╔══════════════════════════════════════════════════════╗
║     Scenario 08 — Available Slots Load Results       ║
╠══════════════════════════════════════════════════════╣
║  Peak VUs     : 100  (no caching on this endpoint)   ║
╠══════════════════════════════════════════════════════╣
║  Response times (slot calculation, DB only)          ║
║    p50  : ${(p50   + ' ms').padEnd(43)}║
║    p95  : ${(p95   + ' ms').padEnd(43)}║
║    p99  : ${(p99   + ' ms').padEnd(43)}║
║    max  : ${(maxVal + ' ms').padEnd(43)}║
╠══════════════════════════════════════════════════════╣
║  Error rate   : ${(errPct + '%').padEnd(37)}║
╠══════════════════════════════════════════════════════╣
║  ${icon}  ${verdict.padEnd(49)}║
╚══════════════════════════════════════════════════════╝

  How to read this:
  ─────────────────
  • p95 < 2 s                  → Acceptable without caching.                  ✅
  • p95 2–5 s                  → Add [OutputCache(Duration=30)] to endpoint.  ⚠️
  • p95 > 5 s                  → DB pool exhaustion — cache is mandatory.     ❌
  • errors > 5%                → Connection refused or query timeout.         ❌

  Root cause of slowness: the service iterates landlord availability windows
  and filters out booked appointments — 2–3 DB queries per request, no cache.

  Fix: add OutputCache with a short TTL (30–60 s), keyed by
  (apartmentId, date). Slots change only when appointments are booked.
`);

  return {};
}
