/**
 * Scenario 05 — SignalR Broadcast Latency
 *
 * Mjeri kašnjenje od trenutka kreiranja nove objave do trenutka kada svi
 * povezani SignalR klijenti prime ReceiveNotification event.
 *
 * Kako funkcionira:
 *   1. 200 VU-ova se poveže na /notificationHub via WebSocket (SignalR JSON protokol).
 *   2. Svaki klijent se pridruži grupi korisnika pozivom JoinNotificationGroup.
 *   3. Jedan "trigger" VU (VU 1) POST-a novu objavu na /api/v1/rent/create-apartment.
 *      → ApartmentService.CreateApartmentAsync poziva NotifyNewListingAsync
 *      → SignalR broadcastuje ReceiveNotification svim klijentima.
 *   4. Svaki klijent mjeri koliko dugo je čekao na poruku (broadcast latency).
 *
 * SignalR JSON protokol handshake:
 *   → WS connect  /notificationHub?id=<connectionId>
 *   → send        {"protocol":"json","version":1}\x1e
 *   ← recv        {}\x1e                              (handshake OK)
 *   → send        invocation JoinNotificationGroup
 *   ← recv        ReceiveNotification  (čekamo ovo)
 *
 * Run:
 *   k6 run -e AUTH_TOKEN=eyJ... tests/k6/scenarios/05_signalr_broadcast.js
 *   k6 run -e BASE_URL=http://localhost:5197 -e AUTH_TOKEN=eyJ... tests/k6/scenarios/05_signalr_broadcast.js
 *
 * Requires: k6 v0.43+ (k6/experimental/websockets)
 */

import http       from 'k6/http';
import ws         from 'k6/experimental/websockets';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';
import { BASE_URL, HEADERS_JSON } from '../config.js';

// Record separator used by SignalR JSON protocol
const RS = String.fromCharCode(30);

// Derive WebSocket base URL from BASE_URL
const WS_BASE = BASE_URL.replace(/^http/, 'ws');

const broadcastLatency = new Trend('signalr_broadcast_latency_ms', true);
const connectErrors    = new Rate('signalr_connect_error');
const broadcastMissed  = new Counter('signalr_broadcast_missed'); // VUs that never got the event
const errorRate        = new Rate('error_rate');

// ── Total listener VUs (VU 1 is the trigger, rest are listeners) ─────────────
const TOTAL_LISTENERS = 50; // keep manageable for local dev; scale up on CI

export const options = {
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(95)', 'p(99)'],
  scenarios: {
    signalr_broadcast: {
      executor: 'shared-iterations',
      vus:        TOTAL_LISTENERS + 1, // +1 for the trigger VU
      iterations: TOTAL_LISTENERS + 1,
      maxDuration: '60s',
    },
  },
  thresholds: {
    // Broadcast should reach all listeners within 2 s (local), 5 s (network hop)
    signalr_broadcast_latency_ms: ['p(95)<2000', 'p(99)<5000'],
    signalr_connect_error:        ['rate<0.05'],   // allow up to 5% connect failures
    signalr_broadcast_missed:     ['count==0'],    // every listener must receive the event
    error_rate:                   ['rate<0.05'],
  },
};

// ── Setup: authenticate and return token + a landlord userId ─────────────────
export function setup() {
  const token = __ENV.AUTH_TOKEN || '';
  if (!token) {
    console.warn('[SignalR] AUTH_TOKEN not set — trigger will fail with 401.');
    console.warn('[SignalR] Run: k6 run -e AUTH_TOKEN=<jwt> scenarios/05_signalr_broadcast.js\n');
  }

  console.log(`[SignalR] Hub    : ${WS_BASE}/notificationHub`);
  console.log(`[SignalR] Trigger: POST ${BASE_URL}/api/v1/rent/create-apartment`);
  console.log(`[SignalR] ${TOTAL_LISTENERS} listener VUs + 1 trigger VU.\n`);

  return { token };
}

// ── Negotiate a SignalR connectionId ─────────────────────────────────────────
function negotiate() {
  const res = http.post(
    `${BASE_URL}/notificationHub/negotiate?negotiateVersion=1`,
    null,
    { headers: HEADERS_JSON }
  );
  if (res.status !== 200) return null;
  try {
    return JSON.parse(res.body).connectionId ?? null;
  } catch {
    return null;
  }
}

// ── Serialise a SignalR hub invocation ────────────────────────────────────────
function hubInvocation(target, args) {
  return JSON.stringify({ type: 1, invocationId: '0', target, arguments: args }) + RS;
}

// ── Main VU function ─────────────────────────────────────────────────────────
export default function (data) {
  const token = data?.token || __ENV.AUTH_TOKEN || '';

  // VU 1 is the trigger; all others are listeners
  if (__VU === 1) {
    triggerBroadcast(token);
  } else {
    listenForBroadcast(token);
  }
}

function triggerBroadcast(token) {
  // Wait briefly so listeners have time to connect first
  sleep(3);

  const headers = {
    ...HEADERS_JSON,
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };

  const payload = JSON.stringify({
    title:         'SignalR Broadcast Test Apartment',
    description:   'k6 performance test — scenario 05',
    rent:          400,
    address:       'Broadcast ulica 5',
    city:          'Sarajevo',
    postalCode:    '71000',
    numberOfRooms: 1,
    sizeSquareMeters: 35,
    listingType:   0,
    apartmentType: 0,
  });

  const res = http.post(
    `${BASE_URL}/api/v1/rent/create-apartment`,
    payload,
    { headers, tags: { name: 'signalr_trigger' } }
  );

  const ok = check(res, {
    'trigger HTTP 200/201': (r) => r.status === 200 || r.status === 201,
  });

  errorRate.add(!ok);

  if (!ok) {
    console.error(`[SignalR] Trigger failed: ${res.status} — ${res.body?.slice(0, 200)}`);
  } else {
    console.log(`[SignalR] Trigger fired at ${new Date().toISOString()}`);
  }
}

function listenForBroadcast(token) {
  const connId = negotiate();
  if (!connId) {
    console.error(`[SignalR] VU${__VU}: negotiate failed`);
    connectErrors.add(1);
    errorRate.add(1);
    broadcastMissed.add(1);
    return;
  }

  const wsUrl = `${WS_BASE}/notificationHub?id=${connId}`;
  let receivedAt    = null;
  let connectedAt   = null;
  let handshakeDone = false;

  const socket = ws.connect(wsUrl, {}, (sock) => {
    sock.on('open', () => {
      connectedAt = Date.now();
      // Step 1 — SignalR handshake
      sock.send(JSON.stringify({ protocol: 'json', version: 1 }) + RS);
    });

    sock.on('message', (raw) => {
      // Messages are delimited by RS; a single frame may contain multiple
      const frames = raw.split(RS).filter(f => f.trim() !== '');

      for (const frame of frames) {
        let msg;
        try { msg = JSON.parse(frame); } catch { continue; }

        // Handshake response: {} (type undefined or type 0 with no error)
        if (!handshakeDone && (msg.type === undefined || msg.type === 0)) {
          handshakeDone = true;
          // Step 2 — join notification group (use VU id as userId placeholder)
          sock.send(hubInvocation('JoinNotificationGroup', [__VU]));
          continue;
        }

        // Hub invocation (type 1) with target ReceiveNotification
        if (msg.type === 1 && msg.target === 'ReceiveNotification') {
          receivedAt = Date.now();
          sock.close();
          return;
        }

        // Ping (type 6) — respond with pong to keep connection alive
        if (msg.type === 6) {
          sock.send(JSON.stringify({ type: 6 }) + RS);
        }
      }
    });

    sock.on('error', (e) => {
      console.error(`[SignalR] VU${__VU} WS error: ${e}`);
      connectErrors.add(1);
    });

    // Timeout: wait up to 30 s for the broadcast
    sock.setTimeout(() => {
      if (!receivedAt) {
        broadcastMissed.add(1);
        console.warn(`[SignalR] VU${__VU}: broadcast NOT received within timeout`);
      }
      sock.close();
    }, 30_000);
  });

  check(socket, {
    'WS connected': (s) => s && s.readyState !== undefined,
  });

  if (receivedAt && connectedAt) {
    const latency = receivedAt - connectedAt;
    broadcastLatency.add(latency);
  } else if (!receivedAt) {
    errorRate.add(1);
  }
}

// ── Summary ─────────────────────────────────────────────────────────────────
export function handleSummary(data) {
  const lat    = data.metrics['signalr_broadcast_latency_ms']?.values ?? {};
  const cerr   = data.metrics['signalr_connect_error']?.values ?? {};
  const missed = data.metrics['signalr_broadcast_missed']?.values ?? {};

  const p50    = (lat.med      ?? 0).toFixed(0);
  const p95    = (lat['p(95)'] ?? 0).toFixed(0);
  const p99    = (lat['p(99)'] ?? 0).toFixed(0);
  const maxVal = (lat.max      ?? 0).toFixed(0);
  const cErrPct= ((cerr.rate ?? 0) * 100).toFixed(1);
  const nMissed= missed.count ?? 0;

  const pass = nMissed === 0 && (lat['p(95)'] ?? 9999) < 2000;
  const verdict = pass
    ? `PASS — broadcast reached all ${TOTAL_LISTENERS} listeners  (p95=${p95} ms)`
    : `WARN — ${nMissed} listener(s) missed broadcast, p95=${p95} ms`;
  const icon = pass ? '✅' : '⚠️ ';

  console.log(`
╔══════════════════════════════════════════════════════╗
║       Scenario 05 — SignalR Broadcast Results        ║
╠══════════════════════════════════════════════════════╣
║  Listener VUs     : ${String(TOTAL_LISTENERS).padEnd(33)}║
╠══════════════════════════════════════════════════════╣
║  Broadcast latency (connect → receive)               ║
║    p50  : ${(p50   + ' ms').padEnd(43)}║
║    p95  : ${(p95   + ' ms').padEnd(43)}║
║    p99  : ${(p99   + ' ms').padEnd(43)}║
║    max  : ${(maxVal + ' ms').padEnd(43)}║
╠══════════════════════════════════════════════════════╣
║  Connect errors   : ${(cErrPct + '%').padEnd(33)}║
║  Missed broadcast : ${String(nMissed).padEnd(33)}║
╠══════════════════════════════════════════════════════╣
║  ${icon}  ${verdict.padEnd(49)}║
╚══════════════════════════════════════════════════════╝

  How to read this:
  ─────────────────
  • p95 < 2000 ms + 0 missed  → SignalR broadcast is healthy.        ✅
  • High p99 (> 5 s)          → Check SignalR backpressure / pool.   ⚠️
  • Missed > 0                → Some clients disconnected too early. ⚠️
  • Connect errors > 5 %      → Hub not reachable or overloaded.     ❌

  Note: latency includes WS connect + handshake + group join time.
  For pure broadcast delta, subtract avg connect overhead (~50-100 ms local).
`);

  return {};
}
