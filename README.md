# Landlord App

Full-stack platforma za iznajmljivanje stanova i traženje cimera. Backend u .NET 10, frontend u React + TypeScript.

---

## Tech Stack

| Layer | Tehnologija |
|---|---|
| Backend | .NET 10, Entity Framework Core, SignalR, Hangfire, ML.NET |
| Frontend | React 18, TypeScript, Vite, Material-UI, React Query |
| Baza | SQL Server (višestruki DbContext-i po modulu) |
| Auth | JWT + Refresh Token (httpOnly cookie) |
| Real-time | SignalR (chat, notifikacije) |
| Plaćanje | Paddle Billing (Merchant of Record) |
| E-mail | Brevo |
| i18n | Srpski, Engleski, Njemački, Ruski |

---

## Pokretanje — Backend

### Preduslovi
- .NET 10 SDK
- SQL Server (lokalni ili Docker)

### Konfiguracija
Podrazumijevane vrijednosti su u `LandlordApp/appsettings.json`. Lokalne tajne stavi u
`LandlordApp/appsettings.Development.json` (nije u git-u) ili u `dotnet user-secrets`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Landlander;Integrated Security=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Secret": "MINIMUM_32_KARAKTERA_TAJNA_LOZINKA_OVDJE",
    "Issuer": "landlander",
    "Audience": "account"
  }
}
```

U Development okruženju Brevo i Azure Blob nisu obavezni (e-mailovi se ne šalju, fajlovi idu na lokalni disk).

### Pokretanje

Migracije za sve DbContext-e se pokreću **automatski pri startu** (`DatabaseMigrationService`),
pa nije potrebno ručno pokretati `dotnet ef database update`.

```bash
cd LandlordApp
dotnet run
```

Backend je dostupan na `https://localhost:7092`  
Swagger UI: `https://localhost:7092/swagger`

---

## Pokretanje — Frontend

### Preduslovi
- Node.js 18+
- npm ili yarn

### Konfiguracija
U **developmentu ne postavljaj `VITE_API_URL`**. Frontend tada šalje zahtjeve na isti origin
(`http://localhost:5173`), a Vite proxy ih prosljeđuje na `https://localhost:7092` (`/api`, `/uploads`,
`/notificationHub`, `/chatHub`). Ovo je bitno: refresh kolačić je `SameSite=Strict`, pa ga browser ne šalje
na drugi origin (`http` → `https`), i korisnik bi bio odjavljen pri svakom osvježavanju stranice.

Za **produkcijski build** `VITE_API_URL` je obavezan (bez njega aplikacija baca grešku pri učitavanju).

### Instalacija i pokretanje

```bash
cd front-land
npm install
npm run dev
```

Frontend je dostupan na `http://localhost:5173`

---

## Struktura projekta

```
Landlord/
├── LandlordApp/                  # .NET Backend
│   └── src/
│       └── Modules/
│           ├── Listings/         # Oglasi za stanove
│           ├── Users/            # Korisnici, auth, uloge
│           ├── Communications/   # Chat, poruke
│           ├── Analytics/        # Analitika i praćenje
│           ├── Payments/         # Integracija plaćanja
│           ├── Reviews/          # Ocjene i recenzije
│           └── MachineLearning/  # ML.NET (preporuke, predviđanje)
└── front-land/                   # React Frontend
    └── src/
        ├── pages/                # Stranice (30+)
        ├── components/           # Zajednički komponenti
        ├── shared/
        │   ├── api/              # API klijenti
        │   ├── context/          # Auth, Theme, Notifications
        │   ├── i18n/             # Konfiguracija prijevoda
        │   └── types/            # TypeScript tipovi
        └── locales/              # Prijevodi (sr/en/de/ru)
```

---

## Ključne funkcionalnosti

- Oglasi stanova (pretraga, filteri, mape, omiljeni)
- Profili cimera s ML.NET match score
- Real-time chat putem SignalR
- Sistem zakazivanja posjeta
- Prijave na oglase s praćenjem statusa
- Premium pretplata s naprednom analitikom
- Prediktor cijena (ML.NET)
- Višejezična podrška (SR/EN/DE/RU)

---

## Plaćanje (Paddle)

Kupovina ide kroz Paddle inline checkout. Backend kreira transakciju (`POST /api/payments/create-payment`) i
upisuje `userId`/`planId` u `custom_data` na serveru; korisnik se dodjeljuje tek kada Paddle pošalje potpisan
webhook (`POST /api/payments/paddle/webhook`, event `transaction.paid` ili `transaction.completed`).
Webhook potpis se provjerava nad sirovim tijelom zahtjeva. Obrada je idempotentna po ID-u transakcije:
porudžbina se prvo "zauzme" (unique ključ), pa tek onda dodijeli, tako da dva istovremena webhook-a za istu
transakciju ne mogu dodijeliti dvaput.

**Refundacije i chargeback-ovi:** događaji `adjustment.created` / `adjustment.updated` sa statusom `approved`
poništavaju kupovinu (tokeni i krediti se oduzimaju, ne ispod nule; trajanje analitike/isticanja/boosta/priority
inbox-a se skraćuje). Obrađuje se samo **potpuni** povraćaj; **djelimični** se samo upisuje u log
(`PARTIAL ... review manually`) i rješava ručno. Poništavanje je idempotentno (`ReversedAt` na porudžbini).

Lokalni test (Paddle **sandbox**):
1. U Paddle sandbox nalogu napravi API ključ, client-side token i notification destination.
2. Katalog (12 jednokratnih cijena) kreira `tools/seed-paddle-catalog.ps1` (traži `PADDLE_API_KEY`).
   Dobijene `pri_...` ID-jeve upiši u `Paddle:PriceIds`.
3. Webhook mora biti javno dostupan; lokalno koristi tunel (npr. `cloudflared tunnel --url https://localhost:7092 --no-tls-verify`)
   i njegov URL + `/api/payments/paddle/webhook` upiši kao destination. Pretplati ga na **`transaction.paid`**,
   **`transaction.completed`**, **`adjustment.created`** i **`adjustment.updated`** (bez zadnja dva refundacije
   neće oduzimati kupljeno).
4. Test kartica: `4242 4242 4242 4242`.

Produkcija zahtijeva odobren **live** Paddle nalog, live ključeve i live `pri_...` ID-jeve (razlikuju se od sandbox-a).

---

## Baza i migracije

Migracije svih 12 DbContext-a se primjenjuju automatski pri startu (`DatabaseMigrationService`). Prazna baza se
u cijelosti može izgraditi iz migracija (provjereno i protiv postojeće baze). Napomene:

- Ne oslanjaj se na konkretan `RoleId`: ID-jevi uloga se razlikuju između baza (kod baze izgrađene iz migracija je
  `Tenant` = 1). Admin se prepoznaje po **imenu** uloge (`Admin`), i na backendu i u frontendu.
- Tabele `payments.Subscriptions` i `payments.Transactions` su zaostale iz Monri perioda i više nisu u modelu;
  namjerno se **ne brišu** automatski jer `Transactions` može sadržavati historijske podatke.
- Šema termina (`appointments`) se sada kreira EF migracijom; stari `001_CreateAppointmentsTables.sql` više nije potreban.

## Skaliranje na više instanci

- Za više instanci obavezno podesi `Redis__Configuration`: koristi se za SignalR backplane, distribuirani cache i
  **OutputCache** (da `EvictByTagAsync` poništi keš na svim instancama). Bez Redisa svaka instanca ima svoj keš.
- **Rate limiting je po instanci** (`System.Threading.RateLimiting` nema distribuiranu varijantu): sa N instanci
  efektivni limiti su do N puta veći. Zaključavanje naloga nakon pogrešnih lozinki je u bazi i važi globalno.
- `/health` uključuje `background-workers`: ako pozadinski servis (npr. outbox) padne 3+ puta zaredom, status je
  `Degraded`, a u logu se pojavljuje `Critical` zapis — postavi alarm na to.

## Testovi

```bash
dotnet test LandlordApp.Tests              # backend (E2E testovi traže Docker, podižu pravi SQL Server)
cd front-land && npm test                  # frontend (Vitest)
cd front-land && npm run lint:ci           # ESLint, greške obaraju build
```

`EmailServiceTests` pozivaju stvarni Brevo API (~14 s po testu) pa su spori; za brzi prolaz ih isključi sa
`--filter "FullyQualifiedName!~EmailServiceTests"`.

---

## Varijable okoline (Production)

Nikad ne commitovati stvarne vrijednosti. U produkciji koristiti environment varijable:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Secret` (jak, nasumičan; aplikacija ne starta sa placeholder vrijednošću)
- `Jwt__Issuer`, `Jwt__Audience`
- `Brevo__ApiKey`, `Brevo__SenderEmail`, `Brevo__SenderName`
- `Paddle__Environment` (`production`), `Paddle__ApiKey`, `Paddle__ClientToken`, `Paddle__WebhookSecret`
- `Paddle__PriceIds__<planId>` (npr. `Paddle__PriceIds__tokens-50`)
- `Metrics__ScrapeToken` (za `/metrics` izvan loopback-a)
- `ForwardedHeaders__KnownProxies` / `ForwardedHeaders__KnownNetworks` (IP load balancera, inače rate limit po IP-u ne radi ispravno)
- `AzureBlobStorage__ConnectionString`, `Redis__Configuration` (za više instanci)
