# Landlord App

Full-stack platforma za iznajmljivanje stanova i traženje cimera. Backend u .NET 8, frontend u React + TypeScript.

---

## Tech Stack

| Layer | Tehnologija |
|---|---|
| Backend | .NET 8, Entity Framework Core, SignalR, ML.NET |
| Frontend | React 18, TypeScript, Vite, Material-UI, React Query |
| Baza | SQL Server (višestruki DbContext-i po modulu) |
| Auth | JWT + Refresh Token (httpOnly cookie) |
| Real-time | SignalR (chat, notifikacije) |
| Plaćanje | Monri, Patten |
| i18n | Srpski, Engleski, Njemački, Ruski |

---

## Pokretanje — Backend

### Preduslovi
- .NET 8 SDK
- SQL Server (lokalni ili Docker)

### Konfiguracija
Kreiraj `LandlordApp/appsettings.Development.json` (ne commitovati):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LandlordDb;User Id=sa;Password=TVOJA_LOZINKA;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "Secret": "MINIMUM_32_KARAKTERA_TAJNA_LOZINKA_OVDJE",
    "Issuer": "LandlordApp",
    "Audience": "LandlordAppUsers",
    "ExpirationMinutes": 60
  }
}
```

### Migracije i pokretanje

```bash
cd LandlordApp

# Pokrenuti migracije za sve module
dotnet ef database update --context UsersContext
dotnet ef database update --context ListingsContext
dotnet ef database update --context ReviewsContext

# Pokrenuti backend
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
Kreiraj `front-land/.env.local`:

```env
VITE_API_URL=https://localhost:7092
```

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

## Varijable okoline (Production)

Nikad ne commitovati stvarne vrijednosti. U produkciji koristiti environment varijable:

- `ConnectionStrings__DefaultConnection`
- `JwtSettings__Secret`
- `Monri__AuthenticityToken`
- `Brevo__ApiKey`
- `SmtpSettings__Password`
