using capstone_backend.Business.DTOs.VenueLocation;
using capstone_backend.Business.Interfaces;
using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VenueLocationController : BaseController
{
    private readonly IVenueLocationService _venueLocationService;
    private readonly ILogger<VenueLocationController> _logger;

    public VenueLocationController(
        IVenueLocationService venueLocationService,
        ILogger<VenueLocationController> logger)
    {
        _venueLocationService = venueLocationService;
        _logger = logger;
    }

    /// <summary>
    /// Get venue location detail by ID.
    /// Returns venue information with location tag (couple mood type and couple personality type) and venue owner profile.
    /// </summary>
    /// <param name="id">Venue location ID</param>
    /// <returns>Venue location detail</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetVenueLocationById(int id)
    {
        _logger.LogInformation("Requesting venue location detail for ID: {VenueId}", id);

        var venue = await _venueLocationService.GetVenueLocationDetailByIdAsync(id);
        
        if (venue == null)
        {
            return NotFoundResponse($"Venue location with ID {id} not found");
        }

        return OkResponse(venue, "Venue location retrieved successfully");
    }

    /// <summary>
    /// Get all venue locations for the authenticated venue owner.
    /// Requires VENUEOWNER role. User ID is extracted from JWT token (sub claim).
    /// Returns venue locations with location tag details (couple mood type and couple personality type).
    /// </summary>
    /// <returns>List of venue locations owned by the authenticated user</returns>
    [HttpGet("my-venues")]
    [Authorize(Roles = "VENUEOWNER")]
    [ProducesResponseType(typeof(ApiResponse<List<VenueOwnerVenueLocationResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> GetMyVenueLocations()
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("User not authenticated");
        }

        _logger.LogInformation("User {UserId} requesting their venue locations", currentUserId.Value);

        var venues = await _venueLocationService.GetVenueLocationsByVenueOwnerAsync(currentUserId.Value);

        return OkResponse(venues, $"Retrieved {venues.Count} venue locations");
    }

    /// <summary>
    /// Get reviews for a specific venue location with summary statistics.
    /// Returns:
    /// - Summary: Average rating, total reviews, rating distribution (5-1 stars with count and percentage)
    /// - Reviews: Paginated list with member info (name, avatar), attached images, and matched tag in Vietnamese
    /// </summary>
    /// <param name="id">Venue location ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>Reviews with summary and paginated list</returns>
    [HttpGet("{id}/reviews")]
    [Authorize]
    public async Task<IActionResult> GetReviewsByVenueId(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation("Requesting reviews for venue ID: {VenueId}, Page: {Page}, PageSize: {PageSize}", id, page, pageSize);

        var response = await _venueLocationService.GetReviewsByVenueIdAsync(id, page, pageSize);

        return OkResponse(response, $"Retrieved {response.Reviews.Items.Count()} reviews with summary for venue location");
    }

    /// <summary>
    /// Get reviews for a specific venue location with optional date/month/year filter and review likes.
    /// If no date filter provided, returns all reviews.
    /// Priority: Date > Month+Year > Year only
    /// Returns:
    /// - Summary: Average rating, total reviews, rating distribution, mood match percentage (from all reviews)
    /// - Reviews: Paginated list (optionally filtered) with member info, attached images, matched tag, and review likes
    /// </summary>
    /// <param name="id">Venue location ID</param>
    /// <param name="date">Optional: Specific date to filter (format: yyyy-MM-dd). If provided, month and year are ignored</param>
    /// <param name="month">Optional: Month to filter (1-12). Requires year parameter</param>
    /// <param name="year">Optional: Year to filter. Can be used alone or with month</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="sortDescending">Sort by created time descending - newest first (default: true)</param>
    /// <returns>Reviews with summary, paginated list, and review likes</returns>
    [HttpGet("{id}/reviews/with-likes")]
    [ProducesResponseType(typeof(ApiResponse<VenueReviewsWithSummaryResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetReviewsWithLikes(
        int id,
        [FromQuery] DateTime? date = null,
        [FromQuery] int? month = null,
        [FromQuery] int? year = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool sortDescending = true)
    {
        // Validate pagination parameters
        if (page < 1)
        {
            return BadRequestResponse("Page number must be greater than 0");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequestResponse("Page size must be between 1 and 100");
        }

        // Validate date filter parameters (nếu có)
        if (month.HasValue && !year.HasValue)
        {
            return BadRequestResponse("Year is required when month is provided");
        }

        if (month.HasValue && (month.Value < 1 || month.Value > 12))
        {
            return BadRequestResponse("Month must be between 1 and 12");
        }

        if (year.HasValue && (year.Value < 2000 || year.Value > 2100))
        {
            return BadRequestResponse("Year must be between 2000 and 2100");
        }

        var filterDescription = date.HasValue
            ? $"date: {date.Value:yyyy-MM-dd}"
            : month.HasValue && year.HasValue
                ? $"month: {year}/{month:D2}"
                : year.HasValue
                    ? $"year: {year}"
                    : "all reviews";

        _logger.LogInformation(
            "Requesting reviews ({Filter}) for venue ID: {VenueId}, Page: {Page}, PageSize: {PageSize}, SortDescending: {SortDesc}",
            filterDescription, id, page, pageSize, sortDescending);

        try
        {
            var response = await _venueLocationService.GetReviewsWithLikesByVenueIdAsync(
                id, page, pageSize, date, month, year, sortDescending);

            var sortOrder = sortDescending ? "newest first" : "oldest first";
            return OkResponse(response,
                $"Retrieved {response.Reviews.Items.Count()} reviews ({filterDescription}) with likes for venue location (sorted by time, {sortOrder})");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Venue {VenueId} not found", id);
            return NotFoundResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reviews for venue {VenueId}", id);
            return BadRequestResponse("Failed to retrieve reviews");
        }
    }

    /// <summary>
    /// Register a new venue location with associated location tags.
    /// Requires authentication - user must be a venue owner.
    /// Location tag is determined based on couple mood type ID and couple personality type ID.
    /// </summary>
    /// <param name="request">Venue location registration request</param>
    /// <returns>Created venue location detail</returns>
    [HttpPost("register")]
    public async Task<IActionResult> RegisterVenueLocation([FromBody] CreateVenueLocationRequest request)
    {
        // Get current user ID
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("User not authenticated");
        }

        _logger.LogInformation("User {UserId} attempting to register venue location: {VenueName}", currentUserId, request.Name);

        try
        {
            var venue = await _venueLocationService.CreateVenueLocationAsync(request, currentUserId.Value);
            return CreatedResponse(venue, "Venue location registered successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation registering venue location for user {UserId}", currentUserId);
            return BadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering venue location for user {UserId}", currentUserId);
            return BadRequestResponse("Failed to register venue location");
        }
    }

    /// <summary>
    /// Delete (soft delete) a location tag from venue.
    /// Venue must have at least 2 tags to allow deletion.
    /// </summary>
    /// <param name="venueId">Venue location ID</param>
    /// <param name="locationTagId">Location tag ID to delete</param>
    /// <returns>Success status</returns>
    [HttpDelete("{venueId}/tags/{locationTagId}")]
    [Authorize]
    public async Task<IActionResult> DeleteVenueLocationTag(int venueId, int locationTagId)
    {
        _logger.LogInformation("Deleting tag {TagId} from venue {VenueId}", locationTagId, venueId);

        var result = await _venueLocationService.DeleteVenueLocationTagAsync(venueId, locationTagId);

        if (!result)
        {
            return NotFoundResponse("Tag not found for this venue or cannot delete last tag");
        }

        return OkResponse(true, "Tag deleted successfully");
    }

    /// <summary>
    /// Update venue location information.
    /// Requires authentication - user must be the venue owner.
    /// </summary>
    /// <param name="id">Venue location ID</param>
    /// <param name="request">Venue location update request</param>
    /// <returns>Updated venue location detail</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVenueLocation(int id, [FromBody] UpdateVenueLocationRequest request)
    {
        _logger.LogInformation("Updating venue location with ID: {VenueId}", id);

        var venue = await _venueLocationService.UpdateVenueLocationAsync(id, request);
        
        if (venue == null)
        {
            return NotFoundResponse($"Venue location with ID {id} not found");
        }

        return OkResponse(venue, "Venue location updated successfully");
    }

    /// <summary>
    /// Get all couple mood types.
    /// Returns a list of all active couple mood types that can be used for venue registration.
    /// </summary>
    /// <returns>List of couple mood types</returns>
    [HttpGet("mood-types/all")]
    public async Task<IActionResult> GetAllCoupleMoodTypes()
    {
        _logger.LogInformation("Requesting all couple mood types");

        var moodTypes = await _venueLocationService.GetAllCoupleMoodTypesAsync();

        return OkResponse(moodTypes, $"Retrieved {moodTypes.Count} couple mood types");
    }

    /// <summary>
    /// Get all couple personality types.
    /// Returns a list of all active couple personality types that can be used for venue registration.
    /// </summary>
    /// <returns>List of couple personality types</returns>
    [HttpGet("personality-types/all")]
    public async Task<IActionResult> GetAllCouplePersonalityTypes()
    {
        _logger.LogInformation("Requesting all couple personality types");

        var personalityTypes = await _venueLocationService.GetAllCouplePersonalityTypesAsync();

        return OkResponse(personalityTypes, $"Retrieved {personalityTypes.Count} couple personality types");
    }

    /// <summary>
    /// Update venue opening hours for a specific day.
    /// Automatically updates is_closed based on current time.
    /// Requires authentication - user must be the venue owner.
    /// </summary>
    /// <param name="request">Update venue opening hour request with venue ID, day (2-8), open time, and close time</param>
    /// <returns>Updated venue opening hour information</returns>
    [HttpPost("opening-hours/update")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<VenueOpeningHourResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UpdateVenueOpeningHour([FromBody] UpdateVenueOpeningHourRequest request)
    {
        _logger.LogInformation("User updating venue opening hours for venue {VenueId}, day {Day}", request.VenueLocationId, request.Day);

        if (!ModelState.IsValid)
        {
            return BadRequestResponse("Invalid request data");
        }

        var result = await _venueLocationService.UpdateVenueOpeningHourAsync(request);

        if (result == null)
        {
            return BadRequestResponse("Failed to update venue opening hours");
        }

        return OkResponse(result, "Venue opening hours updated successfully");
    }
    /// <summary>
    /// Submit venue location to admin for approval.
    /// Validates required fields before changing status to PENDING.
    /// Requires authentication - user must be the venue owner.
    /// </summary>
    /// <param name="id">Venue location ID</param>
    /// <returns>Submission result</returns>
    [HttpPost("{id}/submit")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<VenueSubmissionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> SubmitVenue(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("User not authenticated");
        }
        
        _logger.LogInformation("User {UserId} submitting venue {VenueId} for approval", currentUserId, id);
        
        var result = await _venueLocationService.SubmitVenueToAdminAsync(id, currentUserId.Value);
        
        if (!result.IsSuccess)
        {
            if (result.Message == "Venue not found") return NotFoundResponse(result.Message);
            if (result.Message == "Unauthorized access") return ForbiddenResponse(result.Message);
            
            // For validation errors, we return the result object so the client can see MissingFields
            // Using OkResponse (200) but IsSuccess is false in the DTO
            return OkResponse(result, result.Message);
        }
        
        return OkResponse(result, "Venue submitted successfully");
    }

    /// <summary>
    /// Submit venue with payment - validates venue, generates QR code for payment.
    /// After payment is confirmed (via webhook), venue status will change to PENDING for admin approval.
    /// Requires authentication - user must be the venue owner.
    /// </summary>
    /// <param name="id">Venue location ID</param>
    /// <param name="request">Payment request with packageId and quantity</param>
    /// <returns>Payment QR code and transaction info</returns>
    [HttpPost("{id}/submit-with-payment")]
    [Authorize(Roles = "VENUEOWNER")]
    [ProducesResponseType(typeof(ApiResponse<SubmitVenueWithPaymentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> SubmitVenueWithPayment(int id, [FromBody] SubmitVenueWithPaymentRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("User not authenticated");
        }
        
        _logger.LogInformation("User {UserId} submitting venue {VenueId} with payment", currentUserId, id);
        
        var result = await _venueLocationService.SubmitVenueWithPaymentAsync(id, currentUserId.Value, request);
        
        if (!result.IsSuccess)
        {
            // Return validation errors with 200 but IsSuccess = false
            return OkResponse(result, result.Message);
        }
        
        return OkResponse(result, "Payment QR code generated. Please scan to complete payment.");
    }
    
    /// <summary>
    /// Get pending venue locations for admin.
    /// Requires ADMIN role.
    /// Returns a paginated list of venues waiting for approval.
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>Paginated list of pending venues</returns>
    [HttpGet("pending")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<VenueOwnerVenueLocationResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> GetPendingVenues([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation("Admin requesting pending venues list (Page {Page}, Size {PageSize})", page, pageSize);

        var result = await _venueLocationService.GetPendingVenuesAsync(page, pageSize);

        return OkResponse(result, $"Retrieved {result.Items.Count()} pending venues");
    }

    /// <summary>
    /// Approve or reject a venue location.
    /// Requires ADMIN role.
    /// Accepted statuses: "ACTIVE" (Approve) or "DRAFTED" (Reject).
    /// </summary>
    /// <param name="request">Approval request</param>
    /// <returns>Result of operation</returns>
    [HttpPost("approve")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<VenueSubmissionResult>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> ApproveVenue([FromBody] VenueApprovalRequest request)
    {
        _logger.LogInformation("Admin processing approval for venue {VenueId}, Status: {Status}", request.VenueId, request.Status);

        if (!ModelState.IsValid)
        {
            return BadRequestResponse("Invalid request data");
        }

        var result = await _venueLocationService.ApproveVenueAsync(request);

        if (!result.IsSuccess)
        {
            if (result.Message == "Venue not found") return NotFoundResponse(result.Message);
            return BadRequestResponse(result.Message);
        }

        return OkResponse(result, result.Message);
    }
}
