using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// Member mood log repository implementation for member_mood_log entity
/// </summary>
public class MemberMoodLogRepository : GenericRepository<MemberMoodLog>, IMemberMoodLogRepository
{
    public MemberMoodLogRepository(MyDbContext context) : base(context)
    {
    }

    public async Task<List<MemberMoodLog>> GetByMemberIdAsync(
        int memberId,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!includeSoftDeleted)
            query = query.Where(m => m.IsDeleted != true);

        return await query
            .Where(m => m.MemberId == memberId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
