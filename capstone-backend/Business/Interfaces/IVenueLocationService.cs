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
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Venue location detail or null if not found</returns>
    Task<VenueLocationDetailResponse?> GetVenueLocationDetailByIdAsync(int venueId);
}
