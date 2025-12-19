using Lander.src.Common;
using Microsoft.EntityFrameworkCore;

namespace Lander.Examples;

// PRIMER 1: Osnovni pristup - Manuelno kreiranje PagedResult
public class Example1Service
{
    public async Task<PagedResult<MyDto>> GetItemsManual(int page, int pageSize)
    {
        var query = dbContext.Items.Where(i => i.IsActive);
        
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new MyDto { ... })
            .ToListAsync();

        return PagedResult<MyDto>.Create(items, totalCount, page, pageSize);
    }
}

// PRIMER 2: Korišćenje static metode CreateAsync
public class Example2Service
{
    public async Task<PagedResult<MyDto>> GetItemsWithHelper(int page, int pageSize)
    {
        var query = dbContext.Items
            .Where(i => i.IsActive)
            .Select(i => new MyDto { ... });

        return await PagedResult<MyDto>.CreateAsync(query, page, pageSize);
    }
}

// PRIMER 3: Korišćenje extension metode ToPagedResultAsync (NAJLAKŠE!)
public class Example3Service
{
    public async Task<PagedResult<MyDto>> GetItemsWithExtension(int page, int pageSize)
    {
        return await dbContext.Items
            .Where(i => i.IsActive)
            .Select(i => new MyDto { ... })
            .ToPagedResultAsync(page, pageSize);
    }
}

// PRIMER 4: Korišćenje PaginationParams klase
public class Example4Service
{
    public async Task<PagedResult<MyDto>> GetItemsWithParams(PaginationParams pagination)
    {
        return await dbContext.Items
            .Where(i => i.IsActive)
            .Select(i => new MyDto { ... })
            .ToPagedResultAsync(pagination.Page, pagination.PageSize);
    }
}

// PRIMER 5: U Controlleru sa PaginationParams
public class ExampleController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<MyDto>>> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetItemsWithParams(pagination);
        return Ok(result);
    }
    
    // Poziv: GET /api/items?page=2&pageSize=15
}

// PRIMER 6: Kombinovanje sa filterima
public class Example6Service
{
    public async Task<PagedResult<ApartmentDto>> GetFilteredApartments(
        string? city, 
        decimal? minRent, 
        decimal? maxRent,
        PaginationParams pagination)
    {
        var query = dbContext.Apartments
            .Where(a => a.IsActive && !a.IsDeleted)
            .AsNoTracking();

        // Primeni filtere
        if (!string.IsNullOrEmpty(city))
            query = query.Where(a => a.City == city);
        
        if (minRent.HasValue)
            query = query.Where(a => a.Rent >= minRent.Value);
        
        if (maxRent.HasValue)
            query = query.Where(a => a.Rent <= maxRent.Value);

        // Automatski primeni paginaciju
        return await query
            .OrderBy(a => a.Rent)
            .Select(a => new ApartmentDto { ... })
            .ToPagedResultAsync(pagination.Page, pagination.PageSize);
    }
}

// DTO primer (opciono)
public class MyDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}
