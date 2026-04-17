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

# Scenario 01 — Cache Stampede (defaultni)
.\run.ps1

# Sa custom URL-om
.\run.ps1 -BaseUrl http://localhost:5197

# Sa auth tokenom (za zaštićene endpoint-e)
.\run.ps1 -AuthToken "eyJhbGciOiJIUzI1NiIs..."

# Direktno k6 komandom
k6 run -e BASE_URL=http://localhost:5197 scenarios/01_cache_stampede.js
```

---

## Scenariji

| # | Fajl | Šta testira | Status |
|---|------|-------------|--------|
| 01 | `01_cache_stampede.js` | HybridCache stampede protection — 500 concurrent GET sa istim cache keyem | ✅ Implementiran |
| 02 | `02_role_upgrade_race.js` | SemaphoreSlim race condition — 2 simultana POST /apartments, isti user | 🔜 |
| 03 | `03_monri_idempotency.js` | ConcurrentDictionary.TryAdd — isti order_number × 10 paralelno | 🔜 |
| 04 | `04_listing_load.js` | Ramp-up do 1000 req/s, p95/p99 latency, error rate | 🔜 |
| 05 | `05_signalr_broadcast.js` | 200 SignalR konekcija, nova objava → broadcast latency | 🔜 |

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

## Metrike koje pratimo

| Metrika | Threshold | Zašto |
|---------|-----------|-------|
| `http_req_failed` | `rate == 0` | Nijedna greška pod opterećenjem |
| `http_req_duration{p95}` | `< 3000ms` | Korisničko iskustvo |
| `http_req_duration{p99}` | `< 5000ms` | Worst-case outlieri |
| `p99/p50 ratio` | `< 15×` | Detekcija stampede efekta |

---

## Preduvjeti za pokretanje

1. API mora biti pokrenut: `dotnet run --project LandlordApp`
2. Baza mora biti dostupna i migrirana
3. Bar nekoliko `Apartment` zapisa u bazi (inače cache test nije smislen)
