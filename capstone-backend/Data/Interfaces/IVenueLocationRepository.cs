using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using System.Linq.Expressions;

namespace capstone_backend.Data.Interfaces;

/// <summary>
/// Repository interface for VenueLocation entity operations
/// </summary>
public interface IVenueLocationRepository : IGenericRepository<VenueLocation>
{
    /// <summary>
    /// Get venue location by ID with all related entities
    /// </summary>
    /// <param name="id">Venue location ID</param>
    /// <returns>Venue location with related data or null</returns>
    Task<VenueLocation?> GetByIdWithDetailsAsync(int id);

    /// <summary>
    /// Get venue locations by venue owner ID
    /// </summary>
    /// <param name="venueOwnerId">Venue owner ID</param>
    /// <returns>List of venue locations</returns>
    Task<List<VenueLocation>> GetByVenueOwnerIdAsync(int venueOwnerId);

    /// <summary>
    /// Get reviews for a venue with member and user information
    /// </summary>
    /// <param name="venueId">Venue location ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of reviews list and total count</returns>
    Task<(List<Review> Reviews, int TotalCount)> GetReviewsByVenueIdAsync(int venueId, int page, int pageSize);
}
