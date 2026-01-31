using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces;

/// <summary>
/// Repository interface for VenueOwnerProfile entity operations
/// </summary>
public interface IVenueOwnerProfileRepository : IGenericRepository<VenueOwnerProfile>
{
    /// <summary>
    /// Get venue owner profile by user ID
    /// </summary>
    /// <param name="userId">User ID from JWT token (sub claim)</param>
    /// <param name="includeSoftDeleted">Include soft deleted records</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Venue owner profile or null if not found</returns>
    Task<VenueOwnerProfile?> GetByUserIdAsync(
        int userId,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default);
}
