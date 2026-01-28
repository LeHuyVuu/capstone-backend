using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// Couple profile repository implementation for couple_profile entity
/// </summary>
public class CoupleProfileRepository : GenericRepository<CoupleProfile>, ICoupleProfileRepository
{
    public CoupleProfileRepository(MyDbContext context) : base(context)
    {
    }

    public async Task<CoupleProfile?> GetByMemberIdAsync(
        int memberId,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!includeSoftDeleted)
            query = query.Where(c => c.IsDeleted != true);

        return await query
            .FirstOrDefaultAsync(c => 
                c.MemberId1 == memberId || c.MemberId2 == memberId, 
                cancellationToken);
    }

    public async Task<CoupleProfile?> GetByBothMemberIdsAsync(
        int memberId1,
        int memberId2,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!includeSoftDeleted)
            query = query.Where(c => c.IsDeleted != true);

        return await query
            .FirstOrDefaultAsync(c => 
                (c.MemberId1 == memberId1 && c.MemberId2 == memberId2) ||
                (c.MemberId1 == memberId2 && c.MemberId2 == memberId1),
                cancellationToken);
    }

    public async Task<List<CoupleProfile>> GetAllActiveAsync(
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!includeSoftDeleted)
            query = query.Where(c => c.IsDeleted != true);

        return await query
            .Where(c => c.Status == "ACTIVE")
            .ToListAsync(cancellationToken);
    }
}
