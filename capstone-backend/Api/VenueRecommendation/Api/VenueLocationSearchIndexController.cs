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
    private const string HardcodedIndexHost = "http://134.209.108.208:7700";

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
            return NotFoundResponse($"Không tìm thấy địa điểm có ID {id} hoặc không thể lập chỉ mục");
        }

        return OkResponse(result, "Lập chỉ mục địa điểm thành công");
    }

    /// <summary>
    /// Index a single venue location to a hardcoded Meilisearch host.
    /// </summary>
    /// <param name="id">Venue location ID</param>
    /// <returns>Success status</returns>
    [HttpPost("v2/{id}/search/index")]
    [Tags("Meilisearch")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> IndexVenueToMeilisearchHardcodedHost(int id)
    {
        _logger.LogInformation(
            "Indexing venue {VenueId} to hardcoded Meilisearch host {Host}",
            id,
            HardcodedIndexHost);

        try
        {
            var syncedCount = await MeilisearchSyncDataUtil.SyncVenueByIdLikeOldAsync(
                id,
                indexName: "venue_locations",
                targetHost: HardcodedIndexHost);

            if (syncedCount <= 0)
            {
                return NotFoundResponse($"Không tìm thấy địa điểm có ID {id} hoặc không thể lập chỉ mục");
            }

            return OkResponse(true, $"Lập chỉ mục địa điểm thành công lên host {HardcodedIndexHost}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed indexing venue {VenueId} to hardcoded Meilisearch host {Host}",
                id,
                HardcodedIndexHost);
            return NotFoundResponse($"Không tìm thấy địa điểm có ID {id} hoặc không thể lập chỉ mục");
        }
    }

    [HttpDelete("search/index/clear")]
    [Tags("Meilisearch")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<IActionResult> ClearSearchIndex()
    {
        var result = await _meilisearchService.ClearIndexAsync();
        return OkResponse(result, "Xóa chỉ mục tìm kiếm thành công");
    }
}
