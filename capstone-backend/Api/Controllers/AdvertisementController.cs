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
    /// <param name="placementType">Optional: Filter by placement type (HOME_BANNER etc.)</param>
    /// <returns>List of rotating advertisements and special events with venue information</returns>
    [HttpGet]
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

    /// <summary>
    /// Get all active advertisement packages.
    /// Public endpoint - returns all available packages for venue owners to choose from.
    /// Packages include pricing, duration, priority score, and placement information.
    /// </summary>
    /// <returns>List of active advertisement packages</returns>
    [HttpGet("packages")]
    [ProducesResponseType(typeof(ApiResponse<List<AdvertisementPackageResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetAdvertisementPackages()
    {
        _logger.LogInformation("Requesting all active advertisement packages");

        try
        {
            var packages = await _advertisementService.GetAdvertisementPackagesAsync();
            return OkResponse(packages, $"Retrieved {packages.Count} active package(s)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving advertisement packages");
            return BadRequestResponse("Failed to retrieve advertisement packages");
        }
    }

    /// <summary>
    /// Create a new advertisement (draft status).
    /// Requires VENUEOWNER role. User ID is extracted from JWT token (sub claim).
    /// Advertisement will be created in DRAFT status. Use submit-with-payment endpoint to activate.
    /// </summary>
    /// <param name="request">Advertisement creation request</param>
    /// <returns>Created advertisement detail</returns>
    [HttpPost("create")]
    [Authorize(Roles = "VENUEOWNER")]
    [ProducesResponseType(typeof(ApiResponse<AdvertisementDetailResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> CreateAdvertisement([FromBody] CreateAdvertisementRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("User not authenticated");
        }

        try
        {
            var advertisement = await _advertisementService.CreateAdvertisementAsync(request, currentUserId.Value);
            return CreatedResponse(advertisement, "Advertisement created successfully in DRAFT status");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch (Exception)
        {
            return BadRequestResponse("Failed to create advertisement");
        }
    }

    /// <summary>
    /// Get all advertisements for the authenticated venue owner.
    /// Requires VENUEOWNER role. User ID is extracted from JWT token (sub claim).
    /// Returns advertisements with status, venue location count, and active venue ad info.
    /// </summary>
    /// <returns>List of advertisements owned by the authenticated user</returns>
    [HttpGet("my-advertisements")]
    [Authorize(Roles = "VENUEOWNER")]
    [ProducesResponseType(typeof(ApiResponse<List<MyAdvertisementResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> GetMyAdvertisements()
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("User not authenticated");
        }

        _logger.LogInformation("VenueOwner {UserId} requesting their advertisements", currentUserId.Value);

        try
        {
            var advertisements = await _advertisementService.GetMyAdvertisementsAsync(currentUserId.Value);
            return OkResponse(advertisements, $"Retrieved {advertisements.Count} advertisements");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving advertisements for user {UserId}", currentUserId);
            return BadRequestResponse("Failed to retrieve advertisements");
        }
    }

    /// <summary>
    /// Get advertisement detail by ID.
    /// Requires VENUEOWNER role. User must own the advertisement.
    /// Returns advertisement information with venue location ads and ads orders.
    /// </summary>
    /// <param name="id">Advertisement ID</param>
    /// <returns>Advertisement detail</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = "VENUEOWNER")]
    [ProducesResponseType(typeof(ApiResponse<AdvertisementDetailResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetAdvertisementById(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("User not authenticated");
        }

        _logger.LogInformation("VenueOwner {UserId} requesting advertisement detail for ID: {AdId}", currentUserId, id);

        try
        {
            var advertisement = await _advertisementService.GetAdvertisementByIdAsync(id, currentUserId.Value);

            if (advertisement == null)
            {
                return NotFoundResponse($"Advertisement with ID {id} not found or you don't have permission to view it");
            }

            return OkResponse(advertisement, "Advertisement retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving advertisement {AdId} for user {UserId}", id, currentUserId);
            return BadRequestResponse("Failed to retrieve advertisement");
        }
    }

    /// <summary>
    /// Submit advertisement with payment - validates advertisement, generates QR code for payment.
    /// After payment is confirmed (via webhook), advertisement status will change to PENDING for admin approval,
    /// and VenueLocationAdvertisement record will be created.
    /// Requires VENUEOWNER role. User must own the advertisement.
    /// </summary>
    /// <param name="id">Advertisement ID</param>
    /// <param name="request">Payment request with packageId</param>
    /// <returns>Payment QR code and transaction info</returns>
    [HttpPost("{id}/submit-with-payment")]
    [Authorize(Roles = "VENUEOWNER")]
    [ProducesResponseType(typeof(ApiResponse<SubmitAdvertisementWithPaymentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> SubmitAdvertisementWithPayment(int id, [FromBody] SubmitAdvertisementWithPaymentRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("User not authenticated");
        }

        _logger.LogInformation("VenueOwner {UserId} submitting advertisement {AdId} with payment", currentUserId, id);

        try
        {
            var result = await _advertisementService.SubmitAdvertisementWithPaymentAsync(id, currentUserId.Value, request);

            if (!result.IsSuccess)
            {
                // Return validation errors with 200 but IsSuccess = false
                return OkResponse(result, result.Message);
            }

            return OkResponse(result, "Payment QR code generated. Please scan to complete payment.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation submitting advertisement {AdId} for user {UserId}", id, currentUserId);
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting advertisement {AdId} with payment for user {UserId}", id, currentUserId);
            return BadRequestResponse("Failed to submit advertisement with payment");
        }
    }
}
