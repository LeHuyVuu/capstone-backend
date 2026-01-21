using capstone_backend.Data.Context;

namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Unit of Work pattern for managing database transactions
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Database context for direct access when needed
    /// </summary>
    MyDbContext Context { get; }

    /// <summary>
    /// User repository for user_account entity operations
    /// </summary>
    IUserRepository Users { get; }

    IMemberProfileRepository MembersProfile { get; }

    /// <summary>
    /// Save all changes to database
    /// </summary>
    Task<int> SaveChangesAsync();

    /// <summary>
    /// Begin a new database transaction
    /// </summary>
    Task BeginTransactionAsync();

    /// <summary>
    /// Commit the current transaction
    /// </summary>
    Task CommitTransactionAsync();

    /// <summary>
    /// Rollback the current transaction
    /// </summary>
    Task RollbackTransactionAsync();
}
