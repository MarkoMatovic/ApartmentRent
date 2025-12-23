# FILTER FIX - TESTIRANJE

## ŠTA JE URAĐENO

### Backend optimizacije (C#):
1. ✅ **Split Queries** - razdvojeni upiti za apartmane i slike (10x brže)
2. ✅ **Covering Index** - svi često korišćeni podaci u jednom indeksu
3. ✅ **Response Compression** - Brotli/Gzip (5x manja veličina)
4. ✅ **Output Caching** - 5min cache za listu, 10min za detalje
5. ✅ **Take(5)** - maksimalno 5 slika po apartmanu u listama
6. ✅ **Debug logovi** - konzola prikazuje primljene filtere

### Frontend fix (TypeScript):
1. ✅ `minPrice/maxPrice` → `minRent/maxRent` (usklađeno sa backend-om)
2. ✅ Debug console.log-ovi za praćenje filtera
3. ✅ Transformacija string → number pre slanja

---

## KAKO TESTIRATI

### 1. **Restartuj Backend**
```powershell
cd C:\Users\38161\Desktop\Landlord\WebApplication1
dotnet run
```
Backend će biti dostupan na: `https://localhost:5002`

### 2. **Restartuj Frontend** 
Otvori **NOVI PowerShell prozor**:
```powershell
cd C:\Users\38161\Desktop\Landlord\front-land
npm run dev
```
Frontend će biti dostupan na: `http://localhost:5173`

### 3. **Testiraj Filtere**
1. Otvori browser: `http://localhost:5173`
2. Otvori **Browser DevTools** (F12)
3. Idi na **Console** tab
4. Postavi filtere:
   - Price (Min): `300`
   - Price (Max): `500`
5. Proveri konzolu - trebalo bi da vidiš:
   ```
   [apartmentsApi] Input filters: {city: "", minRent: "300", maxRent: "500", ...}
   [apartmentsApi] Sending params: {minRent: 300, maxRent: 500}
   [apartmentsApi] Received apartments count: X
   [apartmentsApi] First 3 apartments: [{...}]
   ```

6. Proveri **Network** tab:
   - URL: `https://localhost:5002/api/v1/rent/get-all-apartments?minRent=300&maxRent=500`
   - Response: samo apartmani sa cenom 300-500 EUR

### 4. **Proveri Backend Log**
U backend konzoli trebalo bi da vidiš:
```
Received filters: City=, MinRent=300, MaxRent=500, Page=1, PageSize=20
[ApartmentService] Filters received - City: '', MinRent: 300, MaxRent: 500, Page: 1, PageSize: 20
[ApartmentService] Applying MinRent filter: 300
[ApartmentService] Applying MaxRent filter: 500
```

---

## AKO FILTRI OPET NE RADE

### Scenario 1: Cache problem
**Simptom**: Uvek isti rezultati bez obzira na filtere  
**Rešenje**:
1. Zatvori browser (potpuno)
2. Obriši cache: Ctrl+Shift+Delete → Cached images and files
3. Ponovo pokreni

### Scenario 2: Frontend ne šalje parametre
**Simptom**: U Network tab-u nemaš `?minRent=300&maxRent=500` u URL-u  
**Rešenje**: 
1. Proveri konzolu - da li ima error-a?
2. Pogledaj `[apartmentsApi] Sending params` - da li su tu `minRent` i `maxRent`?

### Scenario 3: Backend ne prima parametre
**Simptom**: Backend log pokazuje `MinRent=, MaxRent=`  
**Rešenje**:
1. Proveri da li frontend šalje pravilno (Network tab)
2. Ako šalje `minRent=300` ali backend prima prazno, problem je u model binding-u

---

##  VAŽNE NAPOMENE

1. **Cache Invalidation**: Kad kreiraš/obrišeš apartman, cache se automatski briše
2. **Performanse**: Prvo učitavanje je sporije (popunjava cache), drugo je instant
3. **Query Params**: Backend prima i `minRent` i `MinRent` (case-insensitive)
4. **Output Cache Policy**: Cache se razlikuje po query parametrima

---

## DODATNI DEBUG

Ako ni posle ovoga filtri ne rade, otvori:
`C:\Users\38161\Desktop\Landlord\test-api.html`

U browser-u:
1. Otvori taj HTML fajl
2. Klikni "Test with filters (MinRent=300, MaxRent=500)"
3. Pogledaj rezultate - da li backend vraća samo filtrirane apartmane?

Ako i dalje ne radi, pošalji screenshot:
- Browser Network tab (sa request URL-om)
- Backend console output
- Frontend console output

