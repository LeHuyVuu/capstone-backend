using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Interfaces;

/// <summary>
/// User repository interface for user_account specific operations
/// </summary>
public interface IUserRepository : IRepository<user_account>
{
    /// <summary>
    /// Get user by email address
    /// </summary>
    Task<user_account?> GetByEmailAsync(string email, bool includeSoftDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if email already exists
    /// </summary>
    Task<bool> EmailExistsAsync(string email, int? excludeUserId = null, CancellationToken cancellationToken = default);
}
