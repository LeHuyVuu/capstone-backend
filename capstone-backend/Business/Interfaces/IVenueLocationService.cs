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
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of reviews with member information and like count</returns>
    Task<PagedResult<VenueReviewResponse>> GetReviewsByVenueIdAsync(int venueId, int page = 1, int pageSize = 10);
}
