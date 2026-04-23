/**
 * Scenario 09 — Message Send Idempotency Race
 *
 * Verifies that IdempotencyService prevents duplicate message delivery when
 * the same Idempotency-Key is sent by multiple concurrent requests.
 *
 * How it works:
 *   - A single shared Idempotency-Key is generated before all VUs start.
 *   - 10 VUs simultaneously POST /api/v1/messages/send with that key.
 *   - Without idempotency: all 10 requests could create 10 messages → spam.
 *   - With idempotency (double-checked locking): exactly 1 succeeds (200),
 *     the other 9 get 409 Conflict.
 *
 * Also tests the fixed IdempotencyService (SemaphoreSlim double-checked
 * locking) that replaced the old racy GetStringAsync + SetStringAsync.
 *
 * Run:
 *   k6 run -e AUTH_TOKEN=eyJ... -e SENDER_ID=1 -e RECEIVER_ID=2 scenarios/09_message_idempotency.js
 *   k6 run -e BASE_URL=http://localhost:5197 -e AUTH_TOKEN=eyJ... \
 *          -e SENDER_ID=1 -e RECEIVER_ID=2 scenarios/09_message_idempotency.js
 *
 * Note: SENDER_ID must match the userId claim in AUTH_TOKEN.
 *       RECEIVER_ID can be any other valid user in the system.
 */

import http from 'k6/http';
import { check } from 'k6';
import { Counter, Rate } from 'k6/metrics';
import { BASE_URL, HEADERS_JSON } from '../config.js';

// Key generated in setup() (runs once) — module-level Date.now() would run per-VU

const msgAccepted   = new Counter('message_accepted');    // should be exactly 1
const msgRejected   = new Counter('message_rejected_409'); // should be 9
const serverErrors  = new Counter('server_error_5xx');
const errorRate     = new Rate('error_rate');

const CONCURRENT_VUS = 10;

export const options = {
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)'],
  scenarios: {
    message_idempotency: {
      executor: 'shared-iterations',
      vus:        CONCURRENT_VUS,
      iterations: CONCURRENT_VUS,
      maxDuration: '30s',
    },
  },
  thresholds: {
    message_accepted:     ['count==1'],  // exactly one message must be created
    message_rejected_409: ['count==9'],  // all others must be rejected
    server_error_5xx:     ['count==0'],
    error_rate:           ['rate==0'],
  },
};

export function setup() {
  const token           = __ENV.AUTH_TOKEN  || '';
  const senderId        = parseInt(__ENV.SENDER_ID   || '1');
  const receiverId      = parseInt(__ENV.RECEIVER_ID || '2');
  const idempotencyKey  = `k6-msg-race-${Date.now()}`;

  if (!token) {
    console.warn('[Msg] AUTH_TOKEN not set — requests will fail with 401.');
    console.warn('[Msg] Run: k6 run -e AUTH_TOKEN=<jwt> -e SENDER_ID=<id> -e RECEIVER_ID=<id> ...\n');
  }

  console.log(`[Msg] Shared Idempotency-Key: ${idempotencyKey}`);
  console.log(`[Msg] Sender: ${senderId}  →  Receiver: ${receiverId}`);
  console.log(`[Msg] ${CONCURRENT_VUS} VUs will send the same message simultaneously.`);
  console.log('[Msg] IdempotencyService must ensure exactly 1 is created.\n');

  return { token, senderId, receiverId, idempotencyKey };
}

export default function (data) {
  if (!data?.token) {
    console.error('[Msg] No auth token — skipping VU.');
    errorRate.add(1);
    return;
  }

  const headers = {
    ...HEADERS_JSON,
    Authorization: `Bearer ${data.token}`,
    'Idempotency-Key': data.idempotencyKey,
  };

  const payload = JSON.stringify({
    senderId:    data.senderId,
    receiverId:  data.receiverId,
    messageText: 'k6 idempotency race test — scenario 09',
    isSuperLike: false,
  });

  const res = http.post(
    `${BASE_URL}/api/v1/messages/send`,
    payload,
    { headers, tags: { name: 'message_send_idempotent' } }
  );

  const ok      = res.status === 200 || res.status === 201;
  const is409   = res.status === 409;
  const is5xx   = res.status >= 500;

  check(res, {
    'not a server error':   (r) => r.status < 500,
    'expected status':      (r) => r.status === 200 || r.status === 201 || r.status === 409,
  });

  if (ok)    msgAccepted.add(1);
  if (is409) msgRejected.add(1);
  if (is5xx) {
    serverErrors.add(1);
    errorRate.add(1);
    console.error(`[Msg] VU${__VU} 5xx: ${res.status} — ${res.body?.slice(0, 200)}`);
  } else {
    errorRate.add(0);
  }
}

export function handleSummary(data) {
  const accepted  = data.metrics['message_accepted']?.values      ?? {};
  const rejected  = data.metrics['message_rejected_409']?.values  ?? {};
  const srv5xx    = data.metrics['server_error_5xx']?.values       ?? {};

  const nAccepted = accepted.count ?? 0;
  const nRejected = rejected.count ?? 0;
  const nErrors   = srv5xx.count   ?? 0;

  const pass = nAccepted === 1 && nRejected === CONCURRENT_VUS - 1 && nErrors === 0;
  const verdict = pass
    ? `PASS — 1 accepted, ${nRejected} blocked by idempotency, 0 server errors`
    : nErrors > 0
      ? `FAIL — ${nErrors} server error(s)`
      : `FAIL — ${nAccepted} accepted (expected 1), ${nRejected} rejected (expected ${CONCURRENT_VUS - 1})`;
  const icon = pass ? '✅' : '❌';

  console.log(`
╔══════════════════════════════════════════════════════╗
║    Scenario 09 — Message Idempotency Race Results    ║
╠══════════════════════════════════════════════════════╣
║  Concurrent VUs         : ${String(CONCURRENT_VUS).padEnd(27)}║
║  Shared Idempotency-Key : 1 (all VUs use same key)   ║
╠══════════════════════════════════════════════════════╣
║  Messages accepted (200/201) : ${String(nAccepted).padEnd(22)}║
║  Blocked by idempotency (409): ${String(nRejected).padEnd(22)}║
║  Server errors (5xx)         : ${String(nErrors).padEnd(22)}║
╠══════════════════════════════════════════════════════╣
║  ${icon}  ${verdict.padEnd(49)}║
╚══════════════════════════════════════════════════════╝

  How to read this:
  ─────────────────
  • accepted=1 + rejected=9 + 0 errors → IdempotencyService works.     ✅
  • accepted > 1                        → Duplicate messages in DB!     ❌
  • rejected = 0                        → Idempotency-Key header ignored.❌
  • Server errors                       → SemaphoreSlim threw uncaught. ❌

  This also validates the SemaphoreSlim double-checked locking fix
  introduced in IdempotencyService (replaced the old racy Get+Set).
`);

  return {};
}
