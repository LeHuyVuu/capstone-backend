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
    /// <returns>Tuple of reviews list and total count</returns>
    Task<(List<Review> Reviews, int TotalCount)> GetReviewsByVenueIdAsync(int venueId, int page, int pageSize);

    /// <summary>
    /// Get venues for recommendations with filtering
    /// Priority: lat/lon radius search > area (province code) filtering
    /// Returns venues with calculated distance when lat/lon provided
    /// </summary>
    /// <param name="coupleMoodType">Couple mood type to filter</param>
    /// <param name="personalityTags">Personality tags to filter</param>
    /// <param name="area">Province code (e.g. "01" = Hà Nội, "79" = TP.HCM, "92" = Cần Thơ) - used when lat/lon not provided</param>
    /// <param name="latitude">Latitude for geo-location search</param>
    /// <param name="longitude">Longitude for geo-location search</param>
    /// <param name="radiusKm">Search radius in kilometers</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>List of matching venues with distance (sorted by distance when lat/lon provided)</returns>
    Task<List<(VenueLocation Venue, decimal? DistanceKm)>> GetForRecommendationsAsync(
        string? coupleMoodType,
        List<string> personalityTags,
        string? area,
        decimal? latitude,
        decimal? longitude,
        decimal? radiusKm,
        int limit);
}
