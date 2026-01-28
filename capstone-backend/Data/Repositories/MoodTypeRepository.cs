using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// Mood type repository implementation for mood_type entity
/// </summary>
public class MoodTypeRepository : GenericRepository<MoodType>, IMoodTypeRepository
{
    public MoodTypeRepository(MyDbContext context) : base(context)
    {
    }

    public async Task<List<MoodType>> GetAllActiveAsync(
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!includeSoftDeleted)
            query = query.Where(m => m.IsDeleted != true);

        return await query
            .Where(m => m.IsActive == true)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<MoodType?> GetByIdActiveAsync(
        int id,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!includeSoftDeleted)
            query = query.Where(m => m.IsDeleted != true);

        return await query
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive == true, cancellationToken);
    }

    public async Task<MoodType?> GetByNameAsync(
        string name,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!includeSoftDeleted)
            query = query.Where(m => m.IsDeleted != true);

        return await query
            .FirstOrDefaultAsync(m => m.Name.ToLower() == name.ToLower(), cancellationToken);
    }
}
