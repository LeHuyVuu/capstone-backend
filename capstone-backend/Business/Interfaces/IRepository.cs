using System.Linq.Expressions;
using capstone_backend.Business.Entities.Base;

namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Generic repository interface for CRUD operations on entities
/// </summary>
/// <typeparam name="TEntity">The entity type that inherits from BaseEntity</typeparam>
/// <remarks>
/// Provides common database operations: Create, Read, Update, Delete (CRUD)
/// with support for filtering, pagination, and soft delete
/// </remarks>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    /// <summary>
    /// Get entity by ID
    /// </summary>
    /// <param name="id">Entity unique identifier</param>
    /// <param name="includeSoftDeleted">Include soft deleted entities in search</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Entity if found, null otherwise</returns>
    Task<TEntity?> GetByIdAsync(Guid id, bool includeSoftDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all entities with optional filtering
    /// </summary>
    /// <param name="filter">Optional filter expression</param>
    /// <param name="includeSoftDeleted">Include soft deleted entities</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of entities matching the filter</returns>
    Task<IEnumerable<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paged entities with optional filtering and ordering
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="filter">Optional filter expression</param>
    /// <param name="orderBy">Optional ordering function</param>
    /// <param name="includeSoftDeleted">Include soft deleted entities</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (items, totalCount)</returns>
    Task<(IEnumerable<TEntity> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get first entity matching the filter
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <param name="includeSoftDeleted">Include soft deleted entities</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>First entity matching filter, null if not found</returns>
    Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> filter,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if any entity exists matching the filter
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <param name="includeSoftDeleted">Include soft deleted entities</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> filter,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new entity to database
    /// </summary>
    /// <param name="entity">Entity to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Added entity</returns>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add multiple entities to database
    /// </summary>
    /// <param name="entities">Entities to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing entity
    /// </summary>
    /// <param name="entity">Entity to update</param>
    void Update(TEntity entity);

    /// <summary>
    /// Update multiple entities
    /// </summary>
    /// <param name="entities">Entities to update</param>
    void UpdateRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// Soft delete entity (mark as deleted)
    /// </summary>
    /// <param name="entity">Entity to soft delete</param>
    /// <param name="deletedBy">User ID who deleted the entity</param>
    void SoftDelete(TEntity entity, Guid? deletedBy = null);

    /// <summary>
    /// Hard delete entity (permanently remove from database)
    /// </summary>
    /// <param name="entity">Entity to delete</param>
    void Delete(TEntity entity);

    /// <summary>
    /// Hard delete multiple entities
    /// </summary>
    /// <param name="entities">Entities to delete</param>
    void DeleteRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// Count entities matching the filter
    /// </summary>
    /// <param name="filter">Optional filter expression</param>
    /// <param name="includeSoftDeleted">Include soft deleted entities</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities matching the filter</returns>
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default);
}
