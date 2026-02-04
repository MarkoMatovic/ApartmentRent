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

    
    /// <summary>
    /// .NET 10 Feature: Semantic search using vector embeddings
    /// Search apartments by meaning, not just keywords
    /// </summary>
    [HttpGet("semantic-search")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ApartmentDto>>> SemanticSearch(
        [FromQuery] string query,
        [FromQuery] int topN = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { error = "Query cannot be empty" });
        
        // Generate embedding for search query
        var queryEmbedding = _embeddingService.GenerateEmbedding(query);
        
        // Get all apartments with embeddings
        var apartments = await _apartmentService.GetAllApartmentsForSemanticSearchAsync();
        
        // Calculate similarity scores
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
    
    /// <summary>
    /// Generate embeddings for all apartments (admin only)
    /// </summary>
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
