using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces;

public interface ILeaderboardRepository : IGenericRepository<Leaderboard>
{
    Task<(IEnumerable<Leaderboard> Items, int TotalCount)> GetLeaderboardWithCoupleAsync(
        string periodType,
        string seasonKey,
        int pageNumber,
        int pageSize);
    
    Task<(IEnumerable<Leaderboard> Items, int TotalCount)> GetLeaderboardByPeriodAsync(
        string periodType,
        DateTime periodStart,
        DateTime periodEnd,
        int pageNumber,
        int pageSize);
}
