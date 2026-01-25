using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// Repository implementation for LocationTag entity
/// </summary>
public class LocationTagRepository : GenericRepository<LocationTag>, ILocationTagRepository
{
    public LocationTagRepository(MyDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get location tag by couple mood type and couple personality type IDs
    /// </summary>
    public async Task<LocationTag?> GetByMoodAndPersonalityTypeIdsAsync(int? coupleMoodTypeId, int? couplePersonalityTypeId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(lt => lt.CoupleMoodType)
            .Include(lt => lt.CouplePersonalityType)
            .FirstOrDefaultAsync(lt =>
                lt.IsDeleted != true &&
                (coupleMoodTypeId.HasValue ? lt.CoupleMoodTypeId == coupleMoodTypeId : true) &&
                (couplePersonalityTypeId.HasValue ? lt.CouplePersonalityTypeId == couplePersonalityTypeId : true));
    }
}
