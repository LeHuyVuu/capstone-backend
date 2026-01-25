using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.VenueLocation;

namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Service interface for venue location operations
/// </summary>
public interface IVenueLocationService
{
    /// <summary>
    /// Get venue location detail by ID including location tag and venue owner profile
    /// </summary>
    /// <param name="venueId">The venue location ID</param>
    /// <returns>Venue location detail or null if not found</returns>
    Task<VenueLocationDetailResponse?> GetVenueLocationDetailByIdAsync(int venueId);

    /// <summary>
    /// Get reviews for a venue location with pagination
    /// </summary>
    /// <param name="venueId">Venue location ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>Paged list of reviews with member information and like count</returns>
    Task<PagedResult<VenueReviewResponse>> GetReviewsByVenueIdAsync(int venueId, int page = 1, int pageSize = 10);

    /// <summary>
    /// Create a new venue location with location tags
    /// </summary>
    /// <param name="request">Create venue location request</param>
    /// <param name="userId">User ID - will resolve to venue owner profile</param>
    /// <returns>Created venue location response</returns>
    Task<VenueLocationDetailResponse> CreateVenueLocationAsync(CreateVenueLocationRequest request, int userId);

    /// <summary>
    /// Update venue location information
    /// </summary>
    /// <param name="id">Venue location ID</param>
    /// <param name="request">Update venue location request</param>
    /// <returns>Updated venue location response</returns>
    Task<VenueLocationDetailResponse?> UpdateVenueLocationAsync(int id, UpdateVenueLocationRequest request);

    /// <summary>
    /// Get all location tags with couple mood type and couple personality type
    /// </summary>
    /// <returns>List of location tags</returns>
    Task<List<LocationTagResponse>> GetAllLocationTagsAsync();

    /// <summary>
    /// Get all couple mood types
    /// </summary>
    /// <returns>List of couple mood types</returns>
    Task<List<CoupleMoodTypeInfo>> GetAllCoupleMoodTypesAsync();

    /// <summary>
    /// Get all couple personality types
    /// </summary>
    /// <returns>List of couple personality types</returns>
    Task<List<CouplePersonalityTypeInfo>> GetAllCouplePersonalityTypesAsync();
}
