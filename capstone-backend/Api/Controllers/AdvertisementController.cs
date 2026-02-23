using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.Advertisement;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AdvertisementController : BaseController
{
    private readonly IAdvertisementService _advertisementService;
    private readonly ILogger<AdvertisementController> _logger;

    public AdvertisementController(
        IAdvertisementService advertisementService,
        ILogger<AdvertisementController> logger)
    {
        _advertisementService = advertisementService;
        _logger = logger;
    }

    /// <summary>
    /// Get rotating advertisements and special events for members to view.
    /// Returns a mix of advertisements (rotated by priority) and active special events.
    /// Special events are displayed first with highest priority (999).
    /// Advertisements are grouped by priority score and rotated within each group.
    /// Each API call rotates to the next advertisement in the same priority group for fair distribution.
    /// Optionally filter by placement type (e.g., "BANNER", "POPUP", "SIDEBAR").
    /// </summary>
    /// <param name="placementType">Optional: Filter by placement type (BANNER, POPUP, SIDEBAR, etc.)</param>
    /// <returns>List of rotating advertisements and special events with venue information</returns>
    [HttpGet]
    [Authorize(Roles = "MEMBER")]
    [ProducesResponseType(typeof(ApiResponse<List<AdvertisementResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> GetRotatingAdvertisements([FromQuery] string? placementType = null)
    {
        _logger.LogInformation("Member requesting rotating advertisements (PlacementType: {PlacementType})", 
            placementType ?? "all");

        try
        {
            var advertisements = await _advertisementService.GetRotatingAdvertisementsAsync(placementType);

            var adCount = advertisements.Count(a => a.Type == "ADVERTISEMENT");
            var eventCount = advertisements.Count(a => a.Type == "SPECIAL_EVENT");

            return OkResponse(advertisements,
                $"Retrieved {advertisements.Count} item(s): {adCount} advertisement(s) + {eventCount} special event(s)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rotating advertisements");
            return BadRequestResponse("Failed to retrieve advertisements");
        }
    }
}
