/**
 * k6 spike test — SignalR /chatHub negotiate endpoint
 *
 * SignalR negotiate is the first HTTP call every client makes; it is the
 * bottleneck under a sudden spike (e.g., app redeploy, all clients reconnect).
 *
 * Usage:
 *   k6 run signalr-chat-spike.js
 *   k6 run --env BASE_URL=https://my-staging-server.com --env TOKEN=<jwt> signalr-chat-spike.js
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const TOKEN    = __ENV.TOKEN    || '';   // bearer JWT; empty → expect 401

const errorRate = new Rate('errors');

export const options = {
  stages: [
    { duration: '5s',  target: 0  },
    { duration: '5s',  target: 100 },   // sudden spike
    { duration: '30s', target: 100 },
    { duration: '10s', target: 0  },
  ],
  thresholds: {
    http_req_duration: ['p(95)<1000'],  // negotiate should be fast even under load
    errors:            ['rate<0.05'],
  },
};

export default function () {
  const headers = TOKEN
    ? { Authorization: `Bearer ${TOKEN}` }
    : {};

  const res = http.post(
    `${BASE_URL}/chatHub/negotiate?negotiateVersion=1`,
    null,
    { headers, tags: { name: 'SignalR-Negotiate' } }
  );

  const ok = check(res, {
    'negotiate: no 500':         r => r.status !== 500,
    'negotiate: 200 or 401':     r => r.status === 200 || r.status === 401,
  });

  errorRate.add(!ok);
  sleep(0.2);
}
