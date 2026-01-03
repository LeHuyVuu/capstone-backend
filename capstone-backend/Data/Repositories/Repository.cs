using System.Linq.Expressions;
using capstone_backend.Business.Entities.Base;
using capstone_backend.Business.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

// Repository cơ bản cho tất cả các entity
public class Repository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    // Lấy theo ID
    public virtual async Task<TEntity?> GetByIdAsync(
        Guid id, bool includeSoftDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();
        if (!includeSoftDeleted) query = query.Where(e => !e.IsDeleted);
        return await query.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    // Lấy tất cả
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? filter = null, bool includeSoftDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();
        if (!includeSoftDeleted) query = query.Where(e => !e.IsDeleted);
        if (filter != null) query = query.Where(filter);
        return await query.ToListAsync(cancellationToken);
    }

    // Lấy có phân trang
    public virtual async Task<(IEnumerable<TEntity> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool includeSoftDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();
        if (!includeSoftDeleted) query = query.Where(e => !e.IsDeleted);
        if (filter != null) query = query.Where(filter);

        var totalCount = await query.CountAsync(cancellationToken);
        if (orderBy != null) query = orderBy(query);

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    // Lấy item đầu tiên
    public virtual async Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> filter, bool includeSoftDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();
        if (!includeSoftDeleted) query = query.Where(e => !e.IsDeleted);
        return await query.FirstOrDefaultAsync(filter, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> filter,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!includeSoftDeleted)
            query = query.Where(e => !e.IsDeleted);

        return await query.AnyAsync(filter, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    /// <inheritdoc/>
    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual void Update(TEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
    }

    /// <inheritdoc/>
    public virtual void UpdateRange(IEnumerable<TEntity> entities)
    {
        foreach (var entity in entities)
        {
            entity.UpdatedAt = DateTime.UtcNow;
        }
        _dbSet.UpdateRange(entities);
    }

    /// <inheritdoc/>
    public virtual void SoftDelete(TEntity entity, Guid? deletedBy = null)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = deletedBy;
        _dbSet.Update(entity);
    }

    /// <inheritdoc/>
    public virtual void Delete(TEntity entity)
    {
        _dbSet.Remove(entity);
    }

    /// <inheritdoc/>
    public virtual void DeleteRange(IEnumerable<TEntity> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    /// <inheritdoc/>
    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!includeSoftDeleted)
            query = query.Where(e => !e.IsDeleted);

        if (filter != null)
            query = query.Where(filter);

        return await query.CountAsync(cancellationToken);
    }
}
