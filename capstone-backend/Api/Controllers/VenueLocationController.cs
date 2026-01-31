using capstone_backend.Business.DTOs.VenueLocation;
using capstone_backend.Business.Interfaces;
using capstone_backend.Api.Models;
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
    /// Get reviews for a specific venue location.
    /// Returns reviews with member information (name, avatar) and like count.
    /// </summary>
    /// <param name="id">Venue location ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>Paginated list of reviews</returns>
    [HttpGet("{id}/reviews")]
    public async Task<IActionResult> GetReviewsByVenueId(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation("Requesting reviews for venue ID: {VenueId}, Page: {Page}, PageSize: {PageSize}", id, page, pageSize);

        var reviews = await _venueLocationService.GetReviewsByVenueIdAsync(id, page, pageSize);

        return OkResponse(reviews, $"Retrieved {reviews.Items.Count()} reviews for venue location");
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
}
