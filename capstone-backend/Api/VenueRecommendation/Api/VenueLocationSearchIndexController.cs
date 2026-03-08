using capstone_backend.Api.Controllers;
using capstone_backend.Api.Models;
using capstone_backend.Api.VenueRecommendation.Service;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.VenueRecommendation.Api;

/// <summary>
/// Controller for indexing individual venue locations to Meilisearch
/// </summary>
[Route("api/venue-location")]
[ApiController]
public class VenueLocationSearchIndexController : BaseController
{
    private readonly IMeilisearchService _meilisearchService;
    private readonly ILogger<VenueLocationSearchIndexController> _logger;

    public VenueLocationSearchIndexController(
        IMeilisearchService meilisearchService,
        ILogger<VenueLocationSearchIndexController> logger)
    {
        _meilisearchService = meilisearchService;
        _logger = logger;
    }

    /// <summary>
    /// Index a single venue location to Meilisearch.
    /// Use this after creating or updating a venue to make it searchable.
    /// </summary>
    /// <param name="id">Venue location ID</param>
    /// <returns>Success status</returns>
    [HttpPost("{id}/search/index")]
    [Tags("Meilisearch")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> IndexVenueToMeilisearch(int id)
    {
        _logger.LogInformation("Indexing venue {VenueId} to Meilisearch", id);

        var result = await _meilisearchService.IndexVenueLocationAsync(id);

        if (!result)
        {
            return NotFoundResponse($"Venue location with ID {id} not found or could not be indexed");
        }

        return OkResponse(result, "Venue indexed successfully");
    }
}
