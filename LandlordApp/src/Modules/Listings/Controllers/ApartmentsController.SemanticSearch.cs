using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.MachineLearning.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Lander.src.Modules.Listings.Controllers;

public partial class ApartmentsController
{
    // Fields and Constructor moved to ApartmentsController.cs

    
    [HttpGet("semantic-search")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ApartmentDto>>> SemanticSearch(
        [FromQuery] string query,
        [FromQuery] int topN = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { error = "Query cannot be empty" });
        
        var queryEmbedding = _embeddingService.GenerateEmbedding(query);
        
        var apartments = await _apartmentService.GetAllApartmentsForSemanticSearchAsync();
        
        var results = apartments
            .Where(a => !string.IsNullOrEmpty(a.DescriptionEmbedding))
            .Select(a => new
            {
                Apartment = a,
                Score = _embeddingService.CosineSimilarity(
                    queryEmbedding,
                    DeserializeEmbedding(a.DescriptionEmbedding!)
                )
            })
            .OrderByDescending(x => x.Score)
            .Take(topN)
            .Select(x => x.Apartment)
            .ToList();
        
        return Ok(results);
    }
    
    [HttpPost("generate-embeddings")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GenerateEmbeddings()
    {
        var count = await _apartmentService.GenerateEmbeddingsForAllApartmentsAsync(_embeddingService);
        return Ok(new { success = true, count, message = $"Generated embeddings for {count} apartments" });
    }
    
    private float[] DeserializeEmbedding(string json)
    {
        return JsonSerializer.Deserialize<float[]>(json) ?? Array.Empty<float>();
    }
}
