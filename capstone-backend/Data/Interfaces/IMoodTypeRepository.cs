using capstone_backend.Data.Entities;
using capstone_backend.Business.Interfaces;

namespace capstone_backend.Data.Interfaces;

public interface IMoodTypeRepository : IGenericRepository<MoodType>
{
    /// <summary>
    /// Get all active mood types
    /// </summary>
    Task<List<MoodType>> GetAllActiveAsync(
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get mood type by ID if active
    /// </summary>
    Task<MoodType?> GetByIdActiveAsync(
        int id,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get mood type by name
    /// </summary>
    Task<MoodType?> GetByNameAsync(
        string name,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default);
}
