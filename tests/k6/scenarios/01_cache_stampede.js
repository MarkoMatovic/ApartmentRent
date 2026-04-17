/**
 * Scenario 01 — Cache Stampede Protection
 *
 * Verifies that HybridCache prevents 500 concurrent identical requests
 * from each triggering a separate DB query (stampede protection).
 *
 * How it works:
 *   - 500 VUs all fire the exact same GET URL simultaneously (cold cache).
 *   - HybridCache queues all waiting callers behind ONE DB query.
 *   - Result: p99 ≈ p50 (all waited for the same DB result, not 500 separate ones).
 *
 * Server-side confirmation:
 *   Look for "Apartment search: Page=1" in the app logs — it should appear ONCE
 *   (or a handful of times if cache expires mid-test), NOT 500 times.
 *
 * Run:
 *   k6 run tests/k6/scenarios/01_cache_stampede.js
 *   k6 run -e BASE_URL=http://localhost:5197 tests/k6/scenarios/01_cache_stampede.js
 */

import http from 'k6/http';
import { check } from 'k6';
import { Rate } from 'k6/metrics';
import { BASE_URL, HEADERS_JSON } from '../config.js';

// ── Custom metrics ─────────────────────────────────────────────────────────
const errorRate = new Rate('error_rate');

// ── Target URL — cache key is fully determined by these query params ────────
// All 500 VUs share the EXACT same key → HybridCache should protect.
const STAMPEDE_URL =
  `${BASE_URL}/api/v1/rent/get-all-apartments` +
  '?page=1&pageSize=20&sortBy=Rent&sortOrder=asc';

// ── k6 options ──────────────────────────────────────────────────────────────
export const options = {
  // Explicitly request p99 — k6 v1.x defaults to p(90) and p(95) only.
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)'],
  scenarios: {
    cache_stampede: {
      executor: 'shared-iterations',
      // 500 VUs start simultaneously, each runs exactly one iteration.
      // This maximises the probability of a cache stampede occurring
      // if protection is absent.
      vus: 500,
      iterations: 500,
      maxDuration: '45s',
    },
  },
  thresholds: {
    // Zero failures — all 500 must get a valid response.
    http_req_failed: ['rate==0'],
    error_rate:      ['rate==0'],
    // With HybridCache, all 500 wait for 1 DB query then get the cached result.
    // Even on a cold start p99 should not blow out — 5 s is conservative.
    http_req_duration: ['p(95)<3000', 'p(99)<5000'],
  },
};

// ── Setup — intentionally NO warmup (cold cache is the point) ──────────────
export function setup() {
  console.log(`\n[Stampede] Target : ${STAMPEDE_URL}`);
  console.log('[Stampede] Cold cache — 500 VUs will fire simultaneously.');
  console.log('[Stampede] Watch server logs for "Apartment search:" — should appear ~once.\n');
  return { url: STAMPEDE_URL };
}

// ── Main VU function ────────────────────────────────────────────────────────
export default function (data) {
  const res = http.get(data.url, {
    headers: HEADERS_JSON,
    tags: { name: 'stampede' },
  });

  const ok = check(res, {
    'HTTP 200':            (r) => r.status === 200,
    'body is JSON':        (r) => r.headers['Content-Type']?.includes('application/json'),
    'items array present': (r) => {
      try { return Array.isArray(JSON.parse(r.body).items); }
      catch { return false; }
    },
  });

  errorRate.add(!ok);
}

// ── Summary — pretty-printed verdict ───────────────────────────────────────
export function handleSummary(data) {
  // Tagged sub-metrics don't expose percentiles in handleSummary —
  // use the top-level metric which covers all requests in this scenario.
  const d    = data.metrics['http_req_duration']?.values ?? {};
  const fail = data.metrics.http_req_failed?.values ?? {};
  const iter = data.metrics.iterations?.values ?? {};

  // k6 computes p(90), p(95), p(99) by default; median is exposed as 'med'.
  const med      = d.med ?? 0;
  const p95val   = d['p(95)'] ?? 0;
  const p99val   = d['p(99)'] ?? 0;
  const p50      = med.toFixed(0);
  const p95      = p95val.toFixed(0);
  const p99      = p99val.toFixed(0);
  const max      = (d.max ?? 0).toFixed(0);
  const errPct   = ((fail.rate ?? 0) * 100).toFixed(2);
  const total    = iter.count ?? 0;

  const ratio    = med > 0 ? p99val / med : null;
  const verdict  = ratio !== null && ratio < 15
    ? `PASS — HybridCache OK  (p99/med = ${ratio.toFixed(1)}x)`
    : `WARN — possible stampede (p99/med = ${ratio?.toFixed(1) ?? '?'}x)`;
  const icon     = verdict.startsWith('PASS') ? '✅' : '⚠️ ';

  console.log(`
╔══════════════════════════════════════════════════════╗
║        Scenario 01 — Cache Stampede Results          ║
╠══════════════════════════════════════════════════════╣
║  VUs / Iterations : 500 / ${String(total).padEnd(25)}║
╠══════════════════════════════════════════════════════╣
║  Response times                                      ║
║    med  : ${(p50  + ' ms').padEnd(43)}║
║    p95  : ${(p95  + ' ms').padEnd(43)}║
║    p99  : ${(p99  + ' ms').padEnd(43)}║
║    max  : ${(max  + ' ms').padEnd(43)}║
╠══════════════════════════════════════════════════════╣
║  Error rate       : ${(errPct + '%').padEnd(33)}║
╠══════════════════════════════════════════════════════╣
║  ${icon}  ${verdict.padEnd(49)}║
╚══════════════════════════════════════════════════════╝

  How to read this:
  ─────────────────
  • p99/med ratio < 15   → HybridCache queued waiters, 1 DB query fired.       ✅
  • p99/med ratio ≥ 15   → Multiple DB queries likely fired (check logs).       ⚠️
  • Error rate  = 0 %    → All 500 VUs received valid responses.                ✅

  Server-side check (open app logs):
    grep "Apartment search: Page=1" → should appear ~1 time, NOT 500 times.
`);

  // Return empty to suppress default k6 summary duplication
  return {};
}
