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
}
