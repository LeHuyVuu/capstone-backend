using capstone_backend.Business.DTOs.VenueLocation;
using capstone_backend.Business.Interfaces;
using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Data.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VenueLocationController : BaseController
{
    private readonly IVenueLocationService _venueLocationService;
    private readonly ISubscriptionPackageService _subscriptionPackageService;

    private readonly ILogger<VenueLocationController> _logger;

    public VenueLocationController(
        IVenueLocationService venueLocationService,
        ISubscriptionPackageService subscriptionPackageService,
        ILogger<VenueLocationController> logger)
    {
        _subscriptionPackageService = subscriptionPackageService;
        _venueLocationService = venueLocationService;
        _logger = logger;
    }

    /// <summary>
    /// Get venue location detail by ID.
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
    /// </summary>
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

    [HttpGet("my-venues/by-status")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<VenueOwnerVenueLocationResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> GetMyVenueLocationsByStatus([FromQuery] VenueLocationStatus? status, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1)
        {
            return BadRequestResponse("Page number must be greater than 0");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequestResponse("Page size must be between 1 and 100");
        }

        _logger.LogInformation("Requesting venue locations with status {Status} and search {Search}", status, search);

        var venues = await _venueLocationService.GetVenueLocationsByVenueOwnerAndStatusAsync(status, search, page, pageSize);

        var message = status.HasValue
            ? $"Retrieved {venues.Items.Count()} venue locations with status {status}"
            : $"Retrieved {venues.Items.Count()} venue locations";

        return OkResponse(venues, message);
    }

    /// <summary>
    /// Get venue location detail by ID for the authenticated venue owner.
    /// Returns same structure as my-venues endpoint.
    /// </summary>
    [HttpGet("my-venues/{id}")]
    [Authorize(Roles = "VENUEOWNER")]
    [ProducesResponseType(typeof(ApiResponse<VenueOwnerVenueLocationResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetMyVenueLocationById(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("User not authenticated");
        }

        _logger.LogInformation("User {UserId} requesting venue location detail for ID: {VenueId}", currentUserId.Value, id);

        var venue = await _venueLocationService.GetVenueLocationByIdForOwnerAsync(id, currentUserId.Value);

        if (venue == null)
        {
            return NotFoundResponse($"Venue location with ID {id} not found or you don't have permission to access it");
        }

        return OkResponse(venue, "Venue location retrieved successfully");
    }

    /// <summary>
    /// Get reviews for a specific venue location with summary statistics.
    /// </summary>
    [HttpGet("{id}/reviews")]
    public async Task<IActionResult> GetReviewsByVenueId(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation("Requesting reviews for venue ID: {VenueId}, Page: {Page}, PageSize: {PageSize}", id, page, pageSize);

        var response = await _venueLocationService.GetReviewsByVenueIdAsync(id, page, pageSize);

        return OkResponse(response, $"Retrieved {response.Reviews.Items.Count()} reviews with summary for venue location");
    }

    /// <summary>
    /// Get reviews for a specific venue location with optional date/month/year filter and review likes.
    /// </summary>
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
    /// </summary>
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
    /// </summary>
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
    /// </summary>
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
    [HttpGet("mood-types/all")]
    public async Task<IActionResult> GetAllCoupleMoodTypes()
    {
        _logger.LogInformation("Requesting all couple mood types");

        var moodTypes = await _venueLocationService.GetAllCoupleMoodTypesAsync();

        return OkResponse(moodTypes, $"Retrieved {moodTypes.Count} couple mood types");
    }

    /// <summary>
    /// Get all couple personality types.
    /// </summary>
    [HttpGet("personality-types/all")]
    public async Task<IActionResult> GetAllCouplePersonalityTypes()
    {
        _logger.LogInformation("Requesting all couple personality types");

        var personalityTypes = await _venueLocationService.GetAllCouplePersonalityTypesAsync();

        return OkResponse(personalityTypes, $"Retrieved {personalityTypes.Count} couple personality types");
    }

    [HttpPost("opening-hours/update-all")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UpdateVenueOpeningHours([FromBody] UpdateVenueOpeningHoursRequest request)
    {
        _logger.LogInformation("User updating all opening hours for venue {VenueId}", request.VenueLocationId);

        if (!ModelState.IsValid)
        {
            return BadRequestResponse("Invalid request data");
        }

        var result = await _venueLocationService.UpdateVenueOpeningHoursAsync(request);

        if (!result)
        {
            return NotFoundResponse("Venue not found");
        }

        return OkResponse<object>(null, "Cập nhật giờ mở cửa thành công");
    }

    /// <summary>Deprecated !!!! </summary>
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

    [HttpPost("subscription-only/submit-with-payment")]
    [Authorize(Roles = "VENUEOWNER")]
    [ProducesResponseType(typeof(ApiResponse<SubmitVenueWithPaymentResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 403)]
    public async Task<IActionResult> SubmitSubscriptionOnlyWithPayment([FromBody] SubmitSubscriptionOnlyWithPaymentRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return UnauthorizedResponse("User not authenticated");
        }

        _logger.LogInformation("User {UserId} creating user-level subscription with payment", currentUserId);

        var result = await _venueLocationService.SubmitSubscriptionOnlyWithPaymentAsync(currentUserId.Value, request);

        if (!result.IsSuccess)
        {
            return OkResponse(result, result.Message);
        }

        return OkResponse(result, "Payment QR code generated. Please scan to complete payment.");
    }
    
    /// <summary>ADMIN get list venue pending</summary>
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

        /// <summary>ADMIN</summary>

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

    [HttpGet("search/stats")]
    [Tags("Meilisearch")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<IActionResult> GetSearchStats()
    {
        _logger.LogInformation("Getting Meilisearch index statistics");

        try
        {
            // Get all venues from database grouped by status
            var allVenues = await _venueLocationService.GetAllVenuesForStatsAsync();
            
            var stats = new
            {
                database = new
                {
                    totalVenues = allVenues.Total,
                    activeVenues = allVenues.Active,
                    pendingVenues = allVenues.Pending,
                    draftedVenues = allVenues.Drafted,
                    deletedVenues = allVenues.Deleted,
                    statusBreakdown = allVenues.StatusBreakdown
                },
                recommendation = new
                {
                    message = "To make venues searchable, they must have status = 'ACTIVE'",
                    steps = new[]
                    {
                        "1. Create venue (Status = DRAFTED)",
                        "2. Submit venue via POST /api/VenueLocation/{id}/submit (Status = PENDING)",
                        "3. Admin approves via POST /api/VenueLocation/approve (Status = ACTIVE)",
                        "4. Sync to Meilisearch via POST /api/VenueLocation/search/sync"
                    }
                }
            };

            return OkResponse(stats, "Retrieved search statistics");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search statistics");
            return BadRequestResponse("Error retrieving statistics");
        }
    }


    /// <param name="id">Venue location ID</param>
    /// <returns>Venue location with KYC documents and owner profile</returns>
    [HttpGet("{id}/docs")]
    [ProducesResponseType(typeof(ApiResponse<VenueLocationWithKycResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetVenueLocationWithKyc(int id)
    {
        _logger.LogInformation("Requesting venue location with KYC for ID: {VenueId}", id);

        var venue = await _venueLocationService.GetVenueLocationWithKycAsync(id);
        
        if (venue == null)
        {
            return NotFoundResponse($"Venue location with ID {id} not found");
        }

        return OkResponse(venue, "Venue location with KYC retrieved successfully");
    }

     [HttpGet("my-subscriptions")]
    [Authorize(Roles = "VENUEOWNER")]
    public async Task<IActionResult> GetMyVenueSubscriptions()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return UnauthorizedResponse("User not authenticated");
            }

            var packages = await _subscriptionPackageService.GetVenueSubscriptionPackagesByOwnerUserIdAsync(userId.Value);
            
            return OkResponse(packages, $"Successfully retrieved {packages.Count} venue subscription packages");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Venue owner not found for user ID: {UserId}", GetCurrentUserId());
            return NotFoundResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting venue subscription packages for current user");
            return InternalServerErrorResponse("An error occurred while retrieving venue subscription packages");
        }
    }
}
