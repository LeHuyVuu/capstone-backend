using capstone_backend.Api.Controllers;
using capstone_backend.Api.Models;
using capstone_backend.Api.VenueRecommendation.Service;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.VenueRecommendation.Api;

/// <summary>
/// Controller for syncing all venues to Meilisearch index
/// </summary>
[Route("api/venue-location")]
[ApiController]
public class VenueLocationSearchSyncController : BaseController
{
    private readonly IMeilisearchService _meilisearchService;
    private readonly ILogger<VenueLocationSearchSyncController> _logger;

    public VenueLocationSearchSyncController(
        IMeilisearchService meilisearchService,
        ILogger<VenueLocationSearchSyncController> logger)
    {
        _meilisearchService = meilisearchService;
        _logger = logger;
    }

    /// <summary>
    /// Sync all venue locations to Meilisearch index.
    /// This will index all active venues to make them searchable.
    /// Requires ADMIN role.
    /// </summary>
    /// <returns>Number of venues indexed</returns>
    [HttpPost("search/sync")]
    [Tags("Meilisearch")]
    [ProducesResponseType(typeof(ApiResponse<int>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> SyncVenuesToMeilisearch()
    {
        _logger.LogInformation("Admin syncing all venues to Meilisearch");

        // Configure index settings first
        await _meilisearchService.ConfigureIndexSettingsAsync();

        var count = await _meilisearchService.IndexAllVenueLocationsAsync();

        return OkResponse(count, $"Đồng bộ thành công {count} địa điểm lên Meilisearch");
    }

    /// <summary>
    /// Sync all venue locations to Meilisearch index (v2).
    /// This will index all active venues to make them searchable.
    /// Requires ADMIN role.
    /// </summary>
    /// <returns>Number of venues indexed</returns>
    [HttpPost("v2/search/sync")]
    [Tags("Meilisearch")]
    [ProducesResponseType(typeof(ApiResponse<int>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> SyncVenuesToMeilisearchV2()
    {
        _logger.LogInformation("Admin syncing all venues to Meilisearch");

        // Configure index settings first
        await _meilisearchService.ConfigureIndexSettingsV2Async();

        var count = await _meilisearchService.IndexAllVenueLocationsV2Async();

        return OkResponse(count, $"Đồng bộ thành công {count} địa điểm lên Meilisearch");
    }

    /// <summary>
    /// Verify Meilisearch index settings
    /// </summary>
    [HttpGet("search/verify-settings")]
    [Tags("Meilisearch")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> VerifyMeilisearchSettings()
    {
        var result = await _meilisearchService.VerifyIndexSettingsAsync();
        return OkResponse(result, "Xác minh cấu hình thành công");
    }

    /// <summary>
    /// Verify Meilisearch v2 index settings
    /// </summary>
    [HttpGet("v2/search/verify-settings")]
    [Tags("Meilisearch")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> VerifyMeilisearchSettingsV2()
    {
        var result = await _meilisearchService.VerifyIndexSettingsV2Async();
        return OkResponse(result, "Xác minh cấu hình v2 thành công");
    }
}
