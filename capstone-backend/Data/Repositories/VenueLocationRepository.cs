using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// Repository implementation for VenueLocation entity
/// </summary>
public class VenueLocationRepository : GenericRepository<VenueLocation>, IVenueLocationRepository
{
    public VenueLocationRepository(MyDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Get venue location by ID with all related entities (LocationTag, CoupleMoodType, CouplePersonalityType, VenueOwner)
    /// </summary>
    public async Task<VenueLocation?> GetByIdWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(v => v.LocationTag)
                .ThenInclude(lt => lt!.CoupleMoodType)
            .Include(v => v.LocationTag)
                .ThenInclude(lt => lt!.CouplePersonalityType)
            .Include(v => v.VenueOwner)
            .FirstOrDefaultAsync(v => v.Id == id && v.IsDeleted != true);
    }

    /// <summary>
    /// Get venue locations by venue owner ID
    /// </summary>
    public async Task<List<VenueLocation>> GetByVenueOwnerIdAsync(int venueOwnerId)
    {
        return await _dbSet
            .Where(v => v.VenueOwnerId == venueOwnerId && v.IsDeleted != true)
            .ToListAsync();
    }
}
