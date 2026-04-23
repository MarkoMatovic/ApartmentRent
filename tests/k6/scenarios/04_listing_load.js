/**
 * Scenario 04 — Listing Load (Ramp-up do 1000 req/s)
 *
 * Mjeri ponašanje GET /api/v1/rent/get-all-apartments pod rastućim opterećenjem.
 * Koristi raznovrsne filter parametre kako bi se izmješali cache hitovi i DB pozivi.
 *
 * Faze:
 *   0-30s  : ramp od 0 → 200 VU-ova   (zagrijavanje)
 *   30-90s : sustain na 200 VU-ova     (stabilno opterećenje)
 *   90-120s: ramp od 200 → 500 VU-ova  (pritisak)
 *   120-150s: sustain na 500 VU-ova    (peak load)
 *   150-180s: ramp dolje na 0          (cooldown)
 *
 * Run:
 *   k6 run tests/k6/scenarios/04_listing_load.js
 *   k6 run -e BASE_URL=http://localhost:5197 tests/k6/scenarios/04_listing_load.js
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';
import { BASE_URL, HEADERS_JSON, THRESHOLDS_BASE } from '../config.js';

const errorRate   = new Rate('error_rate');
const cacheHitP50 = new Trend('cache_hit_duration',  true);
const cacheMissP50= new Trend('cache_miss_duration', true);

// ── Filter combinations — mix cache hits (identical keys) and misses ─────────
const FILTER_SETS = [
  // High-traffic identical key → should be HybridCache hits after first req
  '?page=1&pageSize=20&sortBy=Rent&sortOrder=asc',
  '?page=1&pageSize=20&sortBy=Rent&sortOrder=asc',
  '?page=1&pageSize=20&sortBy=Rent&sortOrder=asc',
  // Different pages — cache miss per key
  '?page=2&pageSize=20&sortBy=Rent&sortOrder=asc',
  '?page=3&pageSize=20&sortBy=Rent&sortOrder=asc',
  // With city filter — distinct cache keys
  '?page=1&pageSize=20&city=Sarajevo&sortBy=Rent&sortOrder=asc',
  '?page=1&pageSize=20&city=Mostar&sortBy=Rent&sortOrder=asc',
  '?page=1&pageSize=20&city=Banja+Luka&sortBy=Rent&sortOrder=asc',
  // Price range filters
  '?page=1&pageSize=20&minRent=300&maxRent=800&sortBy=Rent&sortOrder=asc',
  '?page=1&pageSize=10&sortBy=Rent&sortOrder=desc',
];

export const options = {
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)'],
  scenarios: {
    listing_load: {
      executor: 'ramping-vus',
      stages: [
        { duration: '30s',  target: 200 }, // ramp up
        { duration: '60s',  target: 200 }, // sustain
        { duration: '30s',  target: 500 }, // peak ramp
        { duration: '30s',  target: 500 }, // peak sustain
        { duration: '30s',  target: 0   }, // cooldown
      ],
    },
  },
  thresholds: {
    ...THRESHOLDS_BASE,
    error_rate: ['rate<0.01'],
    // Cache hits should be fast; overall p99 must stay under 4 s
    'http_req_duration{name:cache_hit}':  ['p(95)<500',  'p(99)<1000'],
    'http_req_duration{name:cache_miss}': ['p(95)<2000', 'p(99)<4000'],
  },
};

export function setup() {
  console.log(`\n[Load] Target: ${BASE_URL}/api/v1/rent/get-all-apartments`);
  console.log('[Load] Ramp: 0 → 200 VUs → 500 VUs → 0  over 3 minutes.');
  console.log(`[Load] ${FILTER_SETS.length} filter combinations (mix of cache hits + misses).\n`);
}

// ── Main VU function ─────────────────────────────────────────────────────────
export default function () {
  // Pick a filter set — weighted so the hot key (index 0-2) is hit more often
  const idx     = Math.floor(Math.random() * FILTER_SETS.length);
  const query   = FILTER_SETS[idx];
  const isCacheCandidate = idx < 3; // first 3 share the same cache key

  const url = `${BASE_URL}/api/v1/rent/get-all-apartments${query}`;
  const tag = isCacheCandidate ? 'cache_hit' : 'cache_miss';

  const res = http.get(url, {
    headers: HEADERS_JSON,
    tags: { name: tag },
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

  if (isCacheCandidate) cacheHitP50.add(res.timings.duration);
  else                  cacheMissP50.add(res.timings.duration);

  // Think time: 100-500 ms (simulates realistic browser pacing)
  sleep(0.1 + Math.random() * 0.4);
}

// ── Summary ─────────────────────────────────────────────────────────────────
export function handleSummary(data) {
  const d    = data.metrics['http_req_duration']?.values ?? {};
  const fail = data.metrics.http_req_failed?.values ?? {};
  const iter = data.metrics.iterations?.values ?? {};
  const hit  = data.metrics['cache_hit_duration']?.values ?? {};
  const miss = data.metrics['cache_miss_duration']?.values ?? {};

  const p50      = (d.med     ?? 0).toFixed(0);
  const p95      = (d['p(95)']?? 0).toFixed(0);
  const p99      = (d['p(99)']?? 0).toFixed(0);
  const maxVal   = (d.max     ?? 0).toFixed(0);
  const errPct   = ((fail.rate ?? 0) * 100).toFixed(2);
  const total    = iter.count ?? 0;
  const rps      = (iter.rate  ?? 0).toFixed(1);

  const hitP95   = (hit['p(95)']  ?? 0).toFixed(0);
  const missP95  = (miss['p(95)'] ?? 0).toFixed(0);

  const errOk    = (fail.rate ?? 0) < 0.01;
  const p95Ok    = (d['p(95)'] ?? 9999) < 2000;
  const verdict  = errOk && p95Ok
    ? 'PASS — latency and error thresholds met'
    : `WARN — ${!p95Ok ? `p95=${p95}ms > 2000ms` : ''} ${!errOk ? `errors=${errPct}%` : ''}`.trim();
  const icon     = verdict.startsWith('PASS') ? '✅' : '⚠️ ';

  console.log(`
╔══════════════════════════════════════════════════════╗
║          Scenario 04 — Listing Load Results          ║
╠══════════════════════════════════════════════════════╣
║  Total iterations : ${String(total).padEnd(33)}║
║  Throughput       : ${(rps + ' req/s').padEnd(33)}║
╠══════════════════════════════════════════════════════╣
║  Overall response times                              ║
║    p50  : ${(p50  + ' ms').padEnd(43)}║
║    p95  : ${(p95  + ' ms').padEnd(43)}║
║    p99  : ${(p99  + ' ms').padEnd(43)}║
║    max  : ${(maxVal + ' ms').padEnd(43)}║
╠══════════════════════════════════════════════════════╣
║  Cache breakdown (p95)                               ║
║    Hot key (cache hit)  : ${(hitP95  + ' ms').padEnd(27)}║
║    Varied key (DB/miss) : ${(missP95 + ' ms').padEnd(27)}║
╠══════════════════════════════════════════════════════╣
║  Error rate       : ${(errPct + '%').padEnd(33)}║
╠══════════════════════════════════════════════════════╣
║  ${icon}  ${verdict.padEnd(49)}║
╚══════════════════════════════════════════════════════╝

  Thresholds:
  ───────────
  • Overall  p95 < 2000 ms  and  p99 < 4000 ms    ← API SLA
  • Cache-hit p95 < 500 ms  (HybridCache serving)
  • Error rate < 1 %

  If cache-hit p95 ≈ cache-miss p95 → cache may not be working under load.
  If errors spike during 500-VU phase → check connection pool / DB limits.
`);

  return {};
}
