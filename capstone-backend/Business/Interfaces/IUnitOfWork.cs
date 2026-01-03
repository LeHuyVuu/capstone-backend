namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Unit of Work pattern interface for managing database transactions
/// </summary>
/// <remarks>
/// Coordinates multiple repository operations in a single transaction.
/// Ensures data consistency by committing all changes together or rolling back on error.
/// </remarks>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// User repository for User entity operations
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Save all changes to database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities affected</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begin a new database transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commit the current transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback the current transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
