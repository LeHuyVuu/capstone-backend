using System.Linq.Expressions;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Context;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// Generic repository implementation for all entities
/// </summary>
public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly MyDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(MyDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
    {
        if (filter == null)
            return await _dbSet.CountAsync();
        return await _dbSet.CountAsync(filter);
    }

    public void Delete(T entity)
    {
        if (_context.Entry(entity).State == EntityState.Detached)
        {
            _dbSet.Attach(entity);
        }
        _dbSet.Remove(entity);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null)
    {
        IQueryable<T> query = _dbSet.AsNoTracking();

        // 1. Filter
        if (filter != null)
        {
            query = query.Where(filter);
        }

        // 2. Count
        var totalCount = await query.CountAsync();

        // 3. OrderBy
        if (orderBy != null)
        {
            query = orderBy(query);
        }

        // 4. Paging
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public void Update(T entity)
    {
        _dbSet.Attach(entity);
        _context.Entry(entity).State = EntityState.Modified;
    }
}
