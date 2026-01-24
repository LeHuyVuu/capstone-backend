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
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Venue location with related data or null</returns>
    Task<VenueLocation?> GetByIdWithDetailsAsync(int id);

    /// <summary>
    /// Get venue locations by venue owner ID
    /// </summary>
    /// <param name="venueOwnerId">Venue owner ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of venue locations</returns>
    Task<List<VenueLocation>> GetByVenueOwnerIdAsync(int venueOwnerId);
}
