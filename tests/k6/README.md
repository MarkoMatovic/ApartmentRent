# k6 Load Tests — LandlordApp

## Instalacija

```powershell
winget install k6 --source winget
# ili
choco install k6
```

Provjeri: `k6 version`

---

## Pokretanje

```powershell
# Iz root foldera projekta:
cd tests\k6

# Scenario 01 — Cache Stampede (defaultni, bez auth)
.\run.ps1

# Scenariji koji trebaju token
.\run.ps1 -Scenario 02 -AuthToken "eyJ..."
.\run.ps1 -Scenario 03 -AuthToken "eyJ..."
.\run.ps1 -Scenario 04
.\run.ps1 -Scenario 05 -AuthToken "eyJ..."
.\run.ps1 -Scenario 06 -AuthToken "eyJ..." -ApartmentId 1
.\run.ps1 -Scenario 07                                   # bez auth (testira rate limiter)
.\run.ps1 -Scenario 08 -AuthToken "eyJ..." -ApartmentId 1
.\run.ps1 -Scenario 09 -AuthToken "eyJ..." -SenderId 1 -ReceiverId 2

# Svi scenariji redom
.\run.ps1 -Scenario all -AuthToken "eyJ..." -ApartmentId 1 -SenderId 1 -ReceiverId 2

# Direktno k6 komandom
k6 run -e BASE_URL=http://localhost:5197 scenarios/01_cache_stampede.js
k6 run -e BASE_URL=http://localhost:5197 -e AUTH_TOKEN=eyJ... -e APARTMENT_ID=1 scenarios/06_concurrent_applications.js
k6 run -e BASE_URL=http://localhost:5197 scenarios/07_auth_rate_limit.js
k6 run -e BASE_URL=http://localhost:5197 -e AUTH_TOKEN=eyJ... -e APARTMENT_ID=1 scenarios/08_available_slots_load.js
k6 run -e BASE_URL=http://localhost:5197 -e AUTH_TOKEN=eyJ... -e SENDER_ID=1 -e RECEIVER_ID=2 scenarios/09_message_idempotency.js
```

### Dobivanje AUTH_TOKEN-a

```powershell
# Login i izvuci token (PowerShell)
$res = Invoke-RestMethod -Method POST -Uri "http://localhost:5197/api/v1/auth/login" `
       -ContentType "application/json" `
       -Body '{"email":"vas@email.com","password":"VasaLozinka"}'
$token = $res.accessToken
.\run.ps1 -Scenario 02 -AuthToken $token
```

---

## Scenariji

| # | Fajl | Šta testira | Status |
|---|------|-------------|--------|
| 01 | `01_cache_stampede.js` | HybridCache stampede protection — 500 concurrent GET sa istim cache keyem | ✅ Implementiran |
| 02 | `02_role_upgrade_race.js` | SemaphoreSlim race condition — 2 simultana POST /apartments, isti user | ✅ Implementiran |
| 03 | `03_monri_idempotency.js` | IdempotencyService — isti Idempotency-Key × 10 paralelnih payment zahtjeva | ✅ Implementiran |
| 04 | `04_listing_load.js` | Ramp-up 0→500 VU-ova, p95/p99 latency, cache vs. DB breakdown | ✅ Implementiran |
| 05 | `05_signalr_broadcast.js` | 50 SignalR konekcija, nova objava → broadcast latency (ms) | ✅ Implementiran |
| 06 | `06_concurrent_applications.js` | 10 simultanih aplikacija za isti stan (isti userId) — unique constraint | ✅ Implementiran |
| 07 | `07_auth_rate_limit.js` | Rate limiter na /login — 15 req brže od 5/30s limita | ✅ Implementiran |
| 08 | `08_available_slots_load.js` | 100 VU-ova na GET /available-slots — nema cachea, bottleneck detekcija | ✅ Implementiran |
| 09 | `09_message_idempotency.js` | 10 concurrent SendMessage sa istim Idempotency-Key — SemaphoreSlim fix | ✅ Implementiran |

---

## Scenario 01 — Cache Stampede

### Što mjeri
500 VU-ova istovremeno udara isti `GET /api/v1/rent/get-all-apartments?page=1&pageSize=20&sortBy=Rent&sortOrder=asc` sa hladnim cacheom.

**HybridCache** treba da:
1. Pusti samo **1 DB query** kroz
2. Stavi ostalih 499 u red čekanja
3. Svima vrati isti keširan rezultat

### Interpretacija rezultata

```
p99 / p50 ratio < 15  → ✅ HybridCache radi — 1 DB query, ostali čekali
p99 / p50 ratio ≥ 15  → ⚠️  Moguć stampede — provjeri logove
Error rate = 0%       → ✅ Svih 500 VU-ova dobilo validan odgovor
```

### Server-side verifikacija
Dok k6 radi, otvori app logove i traži:

```
grep "Apartment search: Page=1" logs/
```

Trebalo bi se pojaviti **~1 put** (maksimalno nekoliko puta ako test traje duže od 5-minutnog cache TTL-a), **NE 500 puta**.

### Tipičan dobar output

```
╔══════════════════════════════════════════════════════╗
║        Scenario 01 — Cache Stampede Results          ║
╠══════════════════════════════════════════════════════╣
║  VUs / Iterations : 500 / 500                        ║
╠══════════════════════════════════════════════════════╣
║  Response times                                      ║
║    p50  : 45 ms                                      ║
║    p95  : 120 ms                                     ║
║    p99  : 250 ms                                     ║
║    max  : 480 ms                                     ║
╠══════════════════════════════════════════════════════╣
║  Error rate       : 0.00%                            ║
╠══════════════════════════════════════════════════════╣
║  ✅  PASS — HybridCache OK  (p99/p50 = 5.5x)        ║
╚══════════════════════════════════════════════════════╝
```

---

## Scenario 02 — Role Upgrade Race (SemaphoreSlim)

### Što mjeri
2 VU-a istovremeno POST-aju `/api/v1/rent/create-apartment` koristeći **isti JWT token** (isti Tenant korisnik).

`UserRoleUpgradeService.AutoUpgradeOnFirstListingAsync` koristi `SemaphoreSlim(1,1)` da spriječi da oba threada pročitaju "Tenant" i oba zapišu "TenantLandlord" → potencijalni DB conflict.

### Interpretacija rezultata

```
2 × HTTP 200 + 0 × 5xx → ✅ SemaphoreSlim radio, nema race
Bilo koji 5xx          → ❌ Race condition — provjeri DB constraint logove
```

### Server-side verifikacija

```
grep "Auto-upgraded user" logs/ → treba se pojaviti TAČNO 1 put
```

---

## Scenario 03 — Monri Idempotency

### Što mjeri
10 VU-ova istovremeno šalje POST `/api/payments/create-payment` sa **istim `Idempotency-Key` headerom**.

`IdempotencyService.IsDuplicateAsync` koristi `IDistributedCache` — prvi zahtjev piše ključ, ostali ga nađu i dobiju 409.

### Interpretacija rezultata

```
1 × 200 + 9 × 409 + 0 × 5xx → ✅ Idempotency radi ispravno
> 1 × 200                    → ❌ Višestruki payment zahtjevi prošli
```

**Zahtijeva**: `AUTH_TOKEN` (JWT za autentifikovanog korisnika)

---

## Scenario 04 — Listing Load Ramp

### Što mjeri
Postepeni ramp od 0 do **500 VU-ova** kroz 3 minute, sa raznovrsnim filter parametrima (mix cache hits i DB pozivi).

**Faze**: 0→200 VU (30s) → sustain (60s) → 200→500 VU (30s) → peak (30s) → cooldown (30s)

### Interpretacija rezultata

| Metrika | Threshold |
|---------|-----------|
| `http_req_duration{cache_hit}` p95 | `< 500 ms` |
| `http_req_duration{cache_miss}` p95 | `< 2000 ms` |
| `http_req_duration` p99 | `< 4000 ms` |
| `error_rate` | `< 1%` |

Ako je `cache_hit_p95 ≈ cache_miss_p95` → HybridCache ne radi pod opterećenjem.

---

## Scenario 05 — SignalR Broadcast Latency

### Što mjeri
50 WebSocket klijenata se poveže na `/notificationHub`. Jedan VU kreira novu objavu → server broadcastuje `ReceiveNotification`. Mjeri se **latency od POST do prijema poruke**.

**SignalR JSON protokol**:
1. `GET /notificationHub/negotiate` → connectionId
2. WebSocket connect + handshake (`{"protocol":"json","version":1}`)
3. `JoinNotificationGroup(vuId)`
4. Čekaj `ReceiveNotification` event

### Interpretacija rezultata

```
p95 < 2 s + 0 missed → ✅ SignalR broadcast je zdrav
missed > 0           → ⚠️  Klijenti se diskonektovali prije poruke
p99 > 5 s            → ⚠️  Provjeri SignalR backpressure / thread pool
```

**Zahtijeva**: `AUTH_TOKEN` (za trigger VU koji kreira objavu)

---

## Scenario 06 — Concurrent Apartment Applications

### Što mjeri
10 VU-ova istovremeno aplicira za **isti stan sa istim userId** (dijele isti JWT token).

Unique DB constraint `(UserId, ApartmentId)` + `DbUpdateException` handler mora garantovati da se u bazi nađe **tačno 1** aplikacija, bez ijedne 500 greške.

### Interpretacija rezultata

```
accepted=1 + rejected=9 + 0 server errors → ✅ Unique constraint radi
accepted=0 (all 400)                       → ✅ Već applicirao ranije (ok)
server errors > 0                          → ❌ DbUpdateException nije uhvaćen
accepted > 1                               → ❌ Duplikat u bazi!
```

**Zahtijeva**: `AUTH_TOKEN`, `APARTMENT_ID`

---

## Scenario 07 — Auth Rate Limiter Probe

### Što mjeri
15 VU-ova istovremeno šalje POST `/api/v1/auth/login` sa netačnim kredencijalima.

Rate-limit policy `"auth"`: **5 req / 30 s per IP** (queue=0 — immediate rejection).
Rate limiter se izvršava **prije** autentifikacije, pa kredencijali nisu relevantni.

### Interpretacija rezultata

```
≥10 × 429 + ≤5 × 401 + 0 × 5xx → ✅ Rate limiter radi ispravno
0 × 429                          → ❌ Limiter nije aktivan
server errors > 0                → ❌ Limiter vraca 5xx (bug)
```

**Ne zahtijeva AUTH_TOKEN** — testira javni endpoint.

---

## Scenario 08 — Available Slots Load Test

### Što mjeri
GET `/api/appointments/available-slots/{apartmentId}?date=YYYY-MM-DD` pod rampom **0→100 VU-ova**.

Endpoint računa slobodne termina iz baze (bez cachea):
1. Učita landlord availability windows
2. Učita postojeće appointmente za taj datum
3. Iterira prozore i oduzima zauzete termine

Svaki zahtjev = 2–3 DB query-a. Pod 100 VU-ova to je potencijalni bottleneck.

### Interpretacija rezultata

| p95 | Zaključak |
|-----|-----------|
| < 2 s | ✅ Radi bez cachea |
| 2–5 s | ⚠️ Dodaj `[OutputCache(Duration=30)]` |
| > 5 s | ❌ DB pool iscrpljen — cache je obavezan |

**Zahtijeva**: `AUTH_TOKEN`, `APARTMENT_ID`

---

## Scenario 09 — Message Send Idempotency Race

### Što mjeri
10 VU-ova istovremeno šalje POST `/api/v1/messages/send` sa **istim `Idempotency-Key` headerom**.

Testira i `SemaphoreSlim` double-checked locking fix u `IdempotencyService`
(zamijenio stari racy `GetStringAsync + SetStringAsync` pattern).

### Interpretacija rezultata

```
1 × 200 + 9 × 409 + 0 × 5xx → ✅ Idempotency radi
accepted > 1                  → ❌ Duplikat poruka u bazi!
rejected = 0                  → ❌ Idempotency-Key header se ignorira
```

**Zahtijeva**: `AUTH_TOKEN`, `SENDER_ID` (mora odgovarati userId u tokenu), `RECEIVER_ID`

---

## Metrike koje pratimo

| Metrika | Threshold | Scenarij |
|---------|-----------|----------|
| `http_req_failed` | `rate == 0` | 01, 02 |
| `http_req_duration{p95}` | `< 2000ms` | 04, 08 |
| `http_req_duration{p99}` | `< 4000ms` | 01, 04 |
| `p99/p50 ratio` | `< 15×` | 01 — detekcija stampede efekta |
| `server_error_count` | `count == 0` | 02, 06, 09 |
| `ok_count` | `count == 1` | 03 — idempotency |
| `signalr_broadcast_latency_ms{p95}` | `< 2000ms` | 05 |
| `signalr_broadcast_missed` | `count == 0` | 05 |
| `application_accepted` | `count <= 1` | 06 — unique constraint |
| `rate_limited_429` | `count >= 10` | 07 — rate limiter |
| `slots_req_ms{p95}` | `< 2000ms` | 08 — bottleneck detekcija |
| `message_accepted` | `count == 1` | 09 — message idempotency |

---

## Preduvjeti za pokretanje

1. API mora biti pokrenut: `dotnet run --project LandlordApp`
2. Baza mora biti dostupna i migrirana (`dotnet ef database update`)
3. Bar nekoliko `Apartment` zapisa u bazi (scenariji 01, 04, 06, 08)
4. `AUTH_TOKEN` JWT za scenarije 02, 03, 05, 06, 08, 09 (vidjeti sekciju "Dobivanje AUTH_TOKEN-a")
5. `APARTMENT_ID` za scenarije 06, 08 — ID stana koji postoji u bazi
6. `SENDER_ID` + `RECEIVER_ID` za scenarij 09 — userId-evi koji postoje u bazi
7. k6 v0.43+ za scenarij 05 (`k6/experimental/websockets`)
