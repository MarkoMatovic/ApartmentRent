# Pagination Helper Classes - Uputstvo za Upotrebu

Lokacija: `WebApplication1/src/Common/`

## 📁 Dostupne Klase

### 1. `PagedResult<T>`
Helper klasa koja sadrži rezultate paginacije.

**Propertiji:**
```csharp
public class PagedResult<T>
{
    public List<T> Items { get; set; }           // Lista rezultata za trenutnu stranicu
    public int TotalCount { get; set; }          // Ukupan broj rezultata
    public int Page { get; set; }                // Trenutna stranica
    public int PageSize { get; set; }            // Broj rezultata po stranici
    public int TotalPages { get; }               // Ukupan broj stranica (computed)
    public bool HasPreviousPage { get; }         // Da li postoji prethodna stranica
    public bool HasNextPage { get; }             // Da li postoji sledeća stranica
}
```

### 2. `PaginationParams`
Klasa za primanje pagination parametara iz API poziva.

**Propertiji:**
```csharp
public class PaginationParams
{
    public int Page { get; set; } = 1;           // Default: stranica 1
    public int PageSize { get; set; } = 20;      // Default: 20 po stranici
    public int Skip { get; }                     // Automatski računa skip vrednost
    public int Take { get; }                     // Automatski vraća pageSize
}
```

**Sigurnost:**
- Maksimalna veličina stranice: **100**
- Ako korisnik pošalje `pageSize=1000`, automatski se ograničava na 100

---

## 🚀 Primeri Korišćenja

### Metod 1: Extension Metoda (PREPORUČENO - NAJLAKŠE)

```csharp
using Lander.src.Common;

public async Task<PagedResult<ApartmentDto>> GetApartments(int page, int pageSize)
{
    return await _context.Apartments
        .Where(a => a.IsActive)
        .AsNoTracking()
        .Select(a => new ApartmentDto { ... })
        .ToPagedResultAsync(page, pageSize);  // ⬅️ Extension metoda!
}
```

### Metod 2: Static CreateAsync Metoda

```csharp
public async Task<PagedResult<ApartmentDto>> GetApartments(int page, int pageSize)
{
    var query = _context.Apartments
        .Where(a => a.IsActive)
        .Select(a => new ApartmentDto { ... });

    return await PagedResult<ApartmentDto>.CreateAsync(query, page, pageSize);
}
```

### Metod 3: Manuelno Kreiranje (za kompleksne scenarije)

```csharp
public async Task<PagedResult<ApartmentDto>> GetApartments(int page, int pageSize)
{
    var query = _context.Apartments.Where(a => a.IsActive);
    
    var totalCount = await query.CountAsync();
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(a => new ApartmentDto { ... })
        .ToListAsync();

    return PagedResult<ApartmentDto>.Create(items, totalCount, page, pageSize);
}
```

---

## 🎯 Korišćenje u Controllerima

### Primer 1: Sa PaginationParams (NAJBOLJE!)

```csharp
[HttpGet]
public async Task<ActionResult<PagedResult<ApartmentDto>>> GetAll(
    [FromQuery] PaginationParams pagination)
{
    var result = await _service.GetApartments(pagination.Page, pagination.PageSize);
    return Ok(result);
}

// API Poziv:
// GET /api/apartments?page=2&pageSize=15
```

### Primer 2: Sa Individual Parametrima

```csharp
[HttpGet]
public async Task<ActionResult<PagedResult<ApartmentDto>>> GetAll(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
{
    var result = await _service.GetApartments(page, pageSize);
    return Ok(result);
}
```

### Primer 3: Kombinovanje sa Filterima

```csharp
[HttpGet]
public async Task<ActionResult<PagedResult<ApartmentDto>>> GetAll(
    [FromQuery] string? city,
    [FromQuery] decimal? minRent,
    [FromQuery] decimal? maxRent,
    [FromQuery] PaginationParams pagination)
{
    var result = await _service.GetFilteredApartments(
        city, 
        minRent, 
        maxRent,
        pagination.Page, 
        pagination.PageSize
    );
    return Ok(result);
}

// API Poziv:
// GET /api/apartments?city=Beograd&minRent=300&maxRent=600&page=1&pageSize=20
```

---

## 📋 Kompletan Primer - Service Layer

```csharp
using Lander.src.Common;
using Microsoft.EntityFrameworkCore;

public class ApartmentService : IApartmentService
{
    private readonly ListingsContext _context;

    public ApartmentService(ListingsContext context)
    {
        _context = context;
    }

    // Primer 1: Osnovna paginacija
    public async Task<PagedResult<ApartmentDto>> GetAllApartments(int page, int pageSize)
    {
        return await _context.Apartments
            .Where(a => !a.IsDeleted && a.IsActive)
            .AsNoTracking()
            .OrderBy(a => a.Rent)
            .Select(a => new ApartmentDto
            {
                ApartmentId = a.ApartmentId,
                Title = a.Title,
                Rent = a.Rent,
                City = a.City
            })
            .ToPagedResultAsync(page, pageSize);
    }

    // Primer 2: Sa filterima
    public async Task<PagedResult<ApartmentDto>> GetFilteredApartments(
        string? city,
        decimal? minRent,
        decimal? maxRent,
        bool? isFurnished,
        int page,
        int pageSize)
    {
        var query = _context.Apartments
            .Where(a => !a.IsDeleted && a.IsActive)
            .AsNoTracking();

        // Dinamički filtri
        if (!string.IsNullOrEmpty(city))
            query = query.Where(a => a.City == city);

        if (minRent.HasValue)
            query = query.Where(a => a.Rent >= minRent.Value);

        if (maxRent.HasValue)
            query = query.Where(a => a.Rent <= maxRent.Value);

        if (isFurnished.HasValue)
            query = query.Where(a => a.IsFurnished == isFurnished.Value);

        // Paginacija
        return await query
            .OrderBy(a => a.Rent)
            .Select(a => new ApartmentDto
            {
                ApartmentId = a.ApartmentId,
                Title = a.Title,
                Rent = a.Rent,
                City = a.City
            })
            .ToPagedResultAsync(page, pageSize);
    }

    // Primer 3: Sa JOIN operacijama
    public async Task<PagedResult<RoommateDto>> GetRoommates(int page, int pageSize)
    {
        var query = from r in _context.Roommates
                    join u in _usersContext.Users on r.UserId equals u.UserId
                    where r.IsActive
                    select new RoommateDto
                    {
                        RoommateId = r.RoommateId,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Bio = r.Bio
                    };

        return await query
            .AsNoTracking()
            .OrderByDescending(r => r.RoommateId)
            .ToPagedResultAsync(page, pageSize);
    }
}
```

---

## 🌐 API Response Format

Kada pozoveš API endpoint sa paginacijom, dobijaš ovaj format:

```json
{
  "items": [
    {
      "apartmentId": 1,
      "title": "Lux stan u centru",
      "rent": 450,
      "city": "Beograd"
    },
    {
      "apartmentId": 2,
      "title": "Dvosoban stan",
      "rent": 380,
      "city": "Beograd"
    }
    // ... još 18 stavki (ako je pageSize=20)
  ],
  "totalCount": 150,        // Ukupno 150 stanova u bazi
  "page": 1,                // Trenutna stranica
  "pageSize": 20,           // Broj rezultata po stranici
  "totalPages": 8,          // Ukupno 8 stranica (150 / 20 = 7.5 → 8)
  "hasPreviousPage": false, // Nema prethodne stranice (jer smo na 1.)
  "hasNextPage": true       // Postoji sledeća stranica
}
```

---

## ✅ Najbolje Prakse

### 1. Uvek koristi AsNoTracking() za paginaciju
```csharp
return await _context.Apartments
    .AsNoTracking()  // ⬅️ Obavezno za read-only!
    .ToPagedResultAsync(page, pageSize);
```

### 2. Sortiraj pre paginacije
```csharp
return await _context.Apartments
    .OrderBy(a => a.Rent)  // ⬅️ Sorting pre paginacije!
    .ToPagedResultAsync(page, pageSize);
```

### 3. Koristi PaginationParams u controllerima
```csharp
public async Task<ActionResult> GetAll([FromQuery] PaginationParams pagination)
{
    // Automatska validacija (max 100 pageSize)
}
```

### 4. Stavi filtere PRE projekcije (Select)
```csharp
// ✅ DOBRO
return await _context.Apartments
    .Where(a => a.City == "Beograd")     // Filter prvo
    .Select(a => new ApartmentDto { ... }) // Projekcija posle
    .ToPagedResultAsync(page, pageSize);

// ❌ LOŠE
return await _context.Apartments
    .Select(a => new ApartmentDto { ... }) // Projekcija prvo
    .Where(a => a.City == "Beograd")       // Filter posle (neefikasno!)
    .ToPagedResultAsync(page, pageSize);
```

---

## 🔧 Testiranje

### Primer Unit Testa

```csharp
[Fact]
public async Task GetApartments_ReturnsPagedResult()
{
    // Arrange
    var service = new ApartmentService(_context);

    // Act
    var result = await service.GetAllApartments(page: 1, pageSize: 10);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(10, result.PageSize);
    Assert.Equal(1, result.Page);
    Assert.True(result.Items.Count <= 10);
}
```

---

## 📊 Performance Benefiti

| Scenario | Bez Paginacije | Sa Paginacijom | Poboljšanje |
|----------|---------------|----------------|-------------|
| **1000 rezultata** | Vrati 1000 | Vrati 20 | **50x** |
| **Transfer size** | 500KB | 10KB | **50x** |
| **Load time** | 2000ms | 40ms | **50x** |
| **Memory** | 50MB | 1MB | **50x** |

---

## 🎓 Zaključak

**Najlakši način:**
```csharp
.ToPagedResultAsync(page, pageSize)
```

**Koristite ove helper klase kroz CELU aplikaciju** za:
- ✅ Stanove (Apartments)
- ✅ Cimere (Roommates)
- ✅ Search Requests
- ✅ Korisnike (Users)
- ✅ Reviews
- ✅ Bilo koju drugu listu!

Sve je sada centralizovano u `src/Common/` folderu! 🎉
