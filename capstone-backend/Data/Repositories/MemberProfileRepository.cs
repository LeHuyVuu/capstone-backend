using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// Member profile repository implementation for member_profile entity
/// </summary>
public class MemberProfileRepository : GenericRepository<MemberProfile>, IMemberProfileRepository
{
    public MemberProfileRepository(MyDbContext context) : base(context)
    {
    }

    public async Task<MemberProfile?> GetByInviteCodeAsync(
        string inviteCode,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!includeSoftDeleted)
            query = query.Where(m => m.IsDeleted != true);

        return await query.FirstOrDefaultAsync(m => m.InviteCode == inviteCode, cancellationToken);
    }

    public async Task<MemberProfile?> GetByUserIdAsync(
        int userId,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!includeSoftDeleted)
            query = query.Where(m => m.IsDeleted != true);

        return await query.FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken);
    }
}
