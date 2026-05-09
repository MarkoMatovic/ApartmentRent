/**
 * k6 load test — Apartments listing & detail endpoints
 *
 * Usage:
 *   k6 run apartments-load.js
 *   k6 run --env BASE_URL=https://my-staging-server.com apartments-load.js
 *
 * Stages:
 *   0→30 VUs over 30 s  — ramp-up (warm caches, JIT)
 *   30   VUs for 2 min  — sustained load
 *   30→0 VUs over 30 s  — ramp-down
 *
 * SLOs (checked via thresholds):
 *   - 95 % of requests complete in < 500 ms
 *   - Error rate < 1 %
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

// ── Configuration ─────────────────────────────────────────────────────────────
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

// ── Custom metrics ────────────────────────────────────────────────────────────
const errorRate   = new Rate('errors');
const listLatency = new Trend('list_latency', true);
const detailLatency = new Trend('detail_latency', true);

// ── Test options ──────────────────────────────────────────────────────────────
export const options = {
  stages: [
    { duration: '30s', target: 30 },   // ramp-up
    { duration: '2m',  target: 30 },   // sustained
    { duration: '30s', target: 0  },   // ramp-down
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],  // 95th percentile < 500 ms
    errors:            ['rate<0.01'],  // error rate < 1 %
    list_latency:      ['p(95)<400'],  // listing search < 400 ms
    detail_latency:    ['p(95)<300'],  // single apartment detail < 300 ms
  },
};

// ── Virtual User scenario ─────────────────────────────────────────────────────
export default function () {
  // 1. Browse all apartments (paginated search — hits OutputCache + HybridCache)
  const listRes = http.get(
    `${BASE_URL}/api/v1/apartments?page=1&pageSize=20&city=Beograd`,
    { tags: { name: 'ApartmentsList' } }
  );

  const listOk = check(listRes, {
    'list: status 200':          r => r.status === 200,
    'list: has items array':     r => {
      try { return Array.isArray(JSON.parse(r.body).items); }
      catch { return false; }
    },
  });

  errorRate.add(!listOk);
  listLatency.add(listRes.timings.duration);

  sleep(0.5);

  // 2. Fetch a single apartment detail
  const detailRes = http.get(
    `${BASE_URL}/api/v1/apartments/1`,
    { tags: { name: 'ApartmentDetail' } }
  );

  const detailOk = check(detailRes, {
    'detail: status 200 or 404': r => r.status === 200 || r.status === 404,
    'detail: json content-type': r => (r.headers['Content-Type'] || '').includes('application/json'),
  });

  errorRate.add(!detailOk);
  detailLatency.add(detailRes.timings.duration);

  sleep(0.5);
}
