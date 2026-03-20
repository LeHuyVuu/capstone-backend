using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

public class LeaderboardRepository : GenericRepository<Leaderboard>, ILeaderboardRepository
{
    public LeaderboardRepository(MyDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<Leaderboard> Items, int TotalCount)> GetLeaderboardWithCoupleAsync(
        string periodType,
        string seasonKey,
        int pageNumber,
        int pageSize)
    {
        var query = _dbSet
            .Include(l => l.Couple)
            .Where(l => l.PeriodType == periodType 
                     && l.SeasonKey == seasonKey 
                     && l.Status == LeaderboardStatus.ACTIVE.ToString())
            .OrderBy(l => l.RankPosition);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
