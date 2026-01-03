using capstone_backend.Business.Entities;

namespace capstone_backend.Business.Interfaces;

/// <summary>
/// User-specific repository interface
/// </summary>
/// <remarks>
/// Extends generic repository with User-specific operations.
/// Add custom queries here (e.g., GetByEmail, GetActiveUsers, etc.)
/// </remarks>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Get user by email address
    /// </summary>
    /// <param name="email">User's email</param>
    /// <param name="includeSoftDeleted">Include soft deleted users</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User if found, null otherwise</returns>
    Task<User?> GetByEmailAsync(string email, bool includeSoftDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if email already exists
    /// </summary>
    /// <param name="email">Email to check</param>
    /// <param name="excludeUserId">Exclude this user ID from check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email exists, false otherwise</returns>
    Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
}
