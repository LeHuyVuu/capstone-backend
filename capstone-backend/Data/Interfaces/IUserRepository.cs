using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Interfaces;

/// <summary>
/// User repository interface for user_account specific operations
/// </summary>
public interface IUserRepository : IGenericRepository<UserAccount>
{
    /// <summary>
    /// Get user by email address
    /// </summary>
    Task<UserAccount?> GetByEmailAsync(string email, bool includeSoftDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if email already exists
    /// </summary>
    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user by ID with member and venue owner profiles included
    /// </summary>
    Task<UserAccount?> GetByIdWithProfilesAsync(int id, bool includeSoftDeleted = false, CancellationToken cancellationToken = default);
}
