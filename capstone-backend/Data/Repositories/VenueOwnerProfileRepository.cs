using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// Repository implementation for VenueOwnerProfile entity
/// </summary>
public class VenueOwnerProfileRepository : GenericRepository<VenueOwnerProfile>, IVenueOwnerProfileRepository
{
    public VenueOwnerProfileRepository(MyDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<VenueOwnerProfile>> GetByIdsAsync(List<int> venueOwnerIds)
    {
        return await _dbSet
            .Where(vop => venueOwnerIds.Contains(vop.Id) && vop.IsDeleted == false)
            .ToListAsync();
    }

    /// <summary>
    /// Get venue owner profile by user ID
    /// </summary>
    public async Task<VenueOwnerProfile?> GetByUserIdAsync(
        int userId,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();

        if (!includeSoftDeleted)
            query = query.Where(vop => vop.IsDeleted != true);

        return await query.FirstOrDefaultAsync(vop => vop.UserId == userId, cancellationToken);
    }

    public async Task<VenueOwnerProfile?> GetIncludeByUserIdAsync(int userId)
    {
        return await _dbSet
            .Include(vop => vop.VenueLocations)
            .FirstOrDefaultAsync(vop => vop.UserId == userId);
    }
}
