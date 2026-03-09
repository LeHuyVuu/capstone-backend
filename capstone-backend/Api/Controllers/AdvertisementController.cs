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


    /// <param name="placementType">Optional: Filter by placement type (HOME_BANNER, POPUP, AND leave blank is random to display in carousel etc.)</param>
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
    /// Grouped by placement type (HOME_BANNER, POPUP, etc.)
    /// </summary>
    [HttpGet("packages")]
    [ProducesResponseType(typeof(ApiResponse<GroupedAdvertisementPackagesResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> GetAdvertisementPackages()
    {
        _logger.LogInformation("Requesting all active advertisement packages");

        try
        {
            var result = await _advertisementService.GetAdvertisementPackagesAsync();
            var totalCount = result.Data.Values.Sum(list => list.Count);
            var placementCount = result.Data.Count;
            
            return OkResponse(result, 
                $"Retrieved {placementCount} placement group(s) with {totalCount} active package(s)");
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

    [HttpPost("approve")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<AdvertisementApprovalResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> ApproveAdvertisement([FromBody] ApproveAdvertisementRequest request)
    {
        _logger.LogInformation("Admin approving advertisement {AdId}", request.AdvertisementId);

        if (!ModelState.IsValid)
        {
            return BadRequestResponse("Invalid request data");
        }

        var result = await _advertisementService.ApproveAdvertisementAsync(request);

        if (!result.IsSuccess)
        {
            if (result.Message == "Advertisement not found") 
                return NotFoundResponse(result.Message);
            return BadRequestResponse(result.Message);
        }

        return OkResponse(result, result.Message);
    }

    [HttpPost("reject")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<AdvertisementApprovalResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> RejectAdvertisement([FromBody] RejectAdvertisementRequest request)
    {
        _logger.LogInformation("Admin rejecting advertisement {AdId}", request.AdvertisementId);

        if (!ModelState.IsValid)
        {
            return BadRequestResponse("Invalid request data");
        }

        var result = await _advertisementService.RejectAdvertisementAsync(request);

        if (!result.IsSuccess)
        {
            if (result.Message == "Advertisement not found") 
                return NotFoundResponse(result.Message);
            return BadRequestResponse(result.Message);
        }

        return OkResponse(result, result.Message);
    }

    #region Public Detail Endpoints


    /// <param name="id">Advertisement ID or Special Event ID</param>
    /// <param name="type">Type: ADVERTISEMENT or SPECIAL_EVENT</param>
    [HttpGet("detail/{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetDetail(int id, [FromQuery] string type)
    {
        _logger.LogInformation("Public request for {Type} detail ID {Id}", type, id);

        if (string.IsNullOrWhiteSpace(type))
        {
            return BadRequestResponse("Type parameter is required (ADVERTISEMENT or SPECIAL_EVENT)");
        }

        var typeUpper = type.Trim().ToUpper();

        try
        {
            if (typeUpper == "ADVERTISEMENT")
            {
                var detail = await _advertisementService.GetPublicAdvertisementDetailAsync(id);
                return OkResponse(detail, "Advertisement detail retrieved successfully");
            }
            else if (typeUpper == "SPECIAL_EVENT")
            {
                var detail = await _advertisementService.GetSpecialEventDetailAsync(id);
                return OkResponse(detail, "Special event detail retrieved successfully");
            }
            else
            {
                return BadRequestResponse($"Invalid type '{type}'. Must be ADVERTISEMENT or SPECIAL_EVENT");
            }
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("{Type} {Id} not found: {Message}", typeUpper, id, ex.Message);
            return NotFoundResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving {Type} detail for ID {Id}", typeUpper, id);
            return BadRequestResponse($"Failed to retrieve {typeUpper.ToLower()} detail");
        }
    }

    #endregion
}
