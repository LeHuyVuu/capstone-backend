using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces;

/// <summary>
/// Repository interface for LocationTag entity operations
/// </summary>
public interface ILocationTagRepository : IGenericRepository<LocationTag>
{
    /// <summary>
    /// Get location tag by couple mood type and couple personality type IDs
    /// </summary>
    /// <param name="coupleMoodTypeId">Couple mood type ID</param>
    /// <param name="couplePersonalityTypeId">Couple personality type ID</param>
    /// <returns>Location tag or null if not found</returns>
    Task<LocationTag?> GetByMoodAndPersonalityTypeIdsAsync(int? coupleMoodTypeId, int? couplePersonalityTypeId);
}
