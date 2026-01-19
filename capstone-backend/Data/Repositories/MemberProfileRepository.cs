using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// Member profile repository implementation for member_profile entity
/// </summary>
public class MemberProfileRepository : Repository<member_profile>, IMemberProfileRepository
{
    public MemberProfileRepository(MyDbContext context) : base(context)
    {
    }

    public async Task<member_profile?> GetByInviteCodeAsync(
        string inviteCode,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!includeSoftDeleted)
            query = query.Where(m => m.is_deleted != true);

        return await query.FirstOrDefaultAsync(m => m.invite_code == inviteCode, cancellationToken);
    }

    public async Task<member_profile?> GetByUserIdAsync(
        int userId,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!includeSoftDeleted)
            query = query.Where(m => m.is_deleted != true);

        return await query.FirstOrDefaultAsync(m => m.user_id == userId, cancellationToken);
    }
}
